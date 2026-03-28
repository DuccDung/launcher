using app_server.Contracts.Auth;
using app_server.Models;
using Microsoft.EntityFrameworkCore;

namespace app_server.Services.Auth;

public class AuthService(
    LauncherDbContext dbContext,
    IPasswordHasher passwordHasher,
    ITokenService tokenService)
{
    public async Task<(bool Success, string? Error, AuthResponse? Response)> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var exists = await dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken);
        if (exists)
        {
            return (false, "Email already exists.", null);
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

        dbContext.Users.Add(user);
        dbContext.Profiles.Add(profile);

        var tokenResult = tokenService.GenerateTokens(user);
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            User = user,
            TokenHash = tokenResult.RefreshTokenHash,
            ExpiresAt = tokenResult.RefreshTokenExpiresAt,
            CreatedAt = utcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return (true, null, CreateAuthResponse(user, tokenResult));
    }

    public async Task<(bool Success, string? Error, AuthResponse? Response)> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

        if (user is null || !passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return (false, "Email or password is invalid.", null);
        }

        if (string.Equals(user.Status, "banned", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(user.Status, "remove", StringComparison.OrdinalIgnoreCase))
        {
            return (false, $"User status '{user.Status}' is not allowed to sign in.", null);
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

        return (true, null, CreateAuthResponse(user, tokenResult));
    }

    public async Task<(bool Success, string? Error, AuthResponse? Response)> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var refreshTokenHash = tokenService.ComputeRefreshTokenHash(refreshToken);
        var storedToken = await dbContext.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == refreshTokenHash, cancellationToken);

        if (storedToken is null || storedToken.RevokedAt.HasValue || storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            return (false, "Refresh token is invalid or expired.", null);
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

        return (true, null, CreateAuthResponse(storedToken.User, tokenResult));
    }

    private static AuthResponse CreateAuthResponse(User user, TokenResult tokenResult)
    {
        return new AuthResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            Status = user.Status,
            AccessToken = tokenResult.AccessToken,
            AccessTokenExpiresAt = tokenResult.AccessTokenExpiresAt,
            RefreshToken = tokenResult.RefreshToken,
            RefreshTokenExpiresAt = tokenResult.RefreshTokenExpiresAt
        };
    }
}
