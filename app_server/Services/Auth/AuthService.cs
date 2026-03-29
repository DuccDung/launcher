using app_server.Contracts.Auth;
using app_server.Models;
using Microsoft.EntityFrameworkCore;

namespace app_server.Services.Auth;

public class AuthService(
    LauncherDbContext dbContext,
    IPasswordHasher passwordHasher,
    ITokenService tokenService)
{
    private const string DefaultUserRoleCode = "USER";
    private const string AdminRoleCode = "ADMIN";

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var exists = await dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken);
        if (exists)
        {
            return new AuthResult { Success = false, Error = "Email already exists." };
        }

        var defaultRole = await dbContext.Roles
            .FirstOrDefaultAsync(x => x.RoleCode == DefaultUserRoleCode, cancellationToken);

        if (defaultRole is null)
        {
            return new AuthResult { Success = false, Error = $"Default role '{DefaultUserRoleCode}' was not found." };
        }

        var utcNow = DateTime.UtcNow;
        var user = new User
        {
            Email = email,
            PasswordHash = passwordHasher.HashPassword(request.Password),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            Status = "active",
            EmailVerified = false,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        var profile = new Profile
        {
            User = user,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? email : request.DisplayName.Trim(),
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        var userRole = new UserRole
        {
            User = user,
            Role = defaultRole,
            CreatedAt = utcNow
        };
        user.UserRoles.Add(userRole);

        dbContext.Users.Add(user);
        dbContext.Profiles.Add(profile);
        dbContext.UserRoles.Add(userRole);

        var tokenResult = tokenService.GenerateTokens(user);
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            User = user,
            TokenHash = tokenResult.RefreshTokenHash,
            ExpiresAt = tokenResult.RefreshTokenExpiresAt,
            CreatedAt = utcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResult
        {
            Success = true,
            Response = CreateAuthResponse(user, tokenResult),
            Tokens = tokenResult
        };
    }

    public Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        return LoginInternalAsync(request, requiredRoleCode: null, cancellationToken);
    }

    public Task<AuthResult> AdminLoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        return LoginInternalAsync(request, requiredRoleCode: AdminRoleCode, cancellationToken);
    }

    public Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        return RefreshInternalAsync(refreshToken, requiredRoleCode: null, cancellationToken);
    }

    public Task<AuthResult> AdminRefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        return RefreshInternalAsync(refreshToken, requiredRoleCode: AdminRoleCode, cancellationToken);
    }

    private async Task<AuthResult> LoginInternalAsync(LoginRequest request, string? requiredRoleCode, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

        if (user is null || !passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return new AuthResult { Success = false, Error = "Email or password is invalid." };
        }

        if (string.Equals(user.Status, "banned", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(user.Status, "remove", StringComparison.OrdinalIgnoreCase))
        {
            return new AuthResult { Success = false, Error = $"User status '{user.Status}' is not allowed to sign in." };
        }

        if (!string.IsNullOrWhiteSpace(requiredRoleCode) && !UserHasRole(user, requiredRoleCode))
        {
            return new AuthResult { Success = false, Error = $"User must have role '{requiredRoleCode}'." };
        }

        var utcNow = DateTime.UtcNow;
        user.LastLoginAt = utcNow;
        user.UpdatedAt = utcNow;

        var tokenResult = tokenService.GenerateTokens(user);
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.UserId,
            TokenHash = tokenResult.RefreshTokenHash,
            ExpiresAt = tokenResult.RefreshTokenExpiresAt,
            CreatedAt = utcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResult
        {
            Success = true,
            Response = CreateAuthResponse(user, tokenResult),
            Tokens = tokenResult
        };
    }

    private async Task<AuthResult> RefreshInternalAsync(string refreshToken, string? requiredRoleCode, CancellationToken cancellationToken)
    {
        var refreshTokenHash = tokenService.ComputeRefreshTokenHash(refreshToken);
        var storedToken = await dbContext.RefreshTokens
            .Include(x => x.User)
            .ThenInclude(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.TokenHash == refreshTokenHash, cancellationToken);

        if (storedToken is null || storedToken.RevokedAt.HasValue || storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            return new AuthResult { Success = false, Error = "Refresh token is invalid or expired." };
        }

        if (!string.IsNullOrWhiteSpace(requiredRoleCode) && !UserHasRole(storedToken.User, requiredRoleCode))
        {
            return new AuthResult { Success = false, Error = $"User must have role '{requiredRoleCode}'." };
        }

        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.User.UpdatedAt = DateTime.UtcNow;

        var tokenResult = tokenService.GenerateTokens(storedToken.User);
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = storedToken.UserId,
            TokenHash = tokenResult.RefreshTokenHash,
            ExpiresAt = tokenResult.RefreshTokenExpiresAt,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResult
        {
            Success = true,
            Response = CreateAuthResponse(storedToken.User, tokenResult),
            Tokens = tokenResult
        };
    }

    private static bool UserHasRole(User user, string roleCode)
    {
        return user.UserRoles.Any(x =>
            x.Role is not null &&
            string.Equals(x.Role.RoleCode, roleCode, StringComparison.OrdinalIgnoreCase));
    }

    private static AuthResponse CreateAuthResponse(User user, TokenResult tokenResult)
    {
        return new AuthResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            Status = user.Status,
            Roles = user.UserRoles
                .Where(x => x.Role is not null && !string.IsNullOrWhiteSpace(x.Role.RoleCode))
                .Select(x => x.Role.RoleCode)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            AccessTokenExpiresAt = tokenResult.AccessTokenExpiresAt,
            RefreshTokenExpiresAt = tokenResult.RefreshTokenExpiresAt
        };
    }
}
