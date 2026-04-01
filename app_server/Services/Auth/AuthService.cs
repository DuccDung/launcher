using app_server.Contracts.Auth;
using app_server.Models;
using Microsoft.EntityFrameworkCore;

namespace app_server.Services.Auth;

public class AuthService(
    LauncherDbContext dbContext,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IUserOtpService userOtpService,
    IAuthEmailService authEmailService,
    ILogger<AuthService> logger)
{
    private const string ActiveStatus = "active";
    private const string InactiveStatus = "inactive";
    private const string DefaultUserRoleCode = "USER";
    private const string AdminRoleCode = "ADMIN";

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var utcNow = DateTime.UtcNow;

        var defaultRole = await dbContext.Roles
            .FirstOrDefaultAsync(item => item.RoleCode == DefaultUserRoleCode, cancellationToken);

        if (defaultRole is null)
        {
            return new AuthResult
            {
                Success = false,
                ErrorCode = AuthErrorCodes.DefaultRoleMissing,
                Error = $"Default role '{DefaultUserRoleCode}' was not found."
            };
        }

        var user = await dbContext.Users
            .Include(item => item.Profile)
            .Include(item => item.UserRoles)
            .ThenInclude(item => item.Role)
            .FirstOrDefaultAsync(item => item.Email == email, cancellationToken);

        if (user is not null && user.EmailVerified)
        {
            return new AuthResult
            {
                Success = false,
                ErrorCode = AuthErrorCodes.EmailAlreadyExists,
                Error = "Email này đã được sử dụng."
            };
        }

        if (user is not null && IsBlocked(user.Status))
        {
            return new AuthResult
            {
                Success = false,
                ErrorCode = AuthErrorCodes.AccountBlocked,
                Error = $"Tài khoản đang ở trạng thái '{user.Status}', không thể đăng ký lại."
            };
        }

        if (user is null)
        {
            user = new User
            {
                Email = email,
                PasswordHash = passwordHasher.HashPassword(request.Password),
                Phone = NormalizePhone(request.Phone),
                Status = InactiveStatus,
                EmailVerified = false,
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            };

            user.Profile = new Profile
            {
                User = user,
                DisplayName = GetRequestedDisplayName(request, email),
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            };

            dbContext.Users.Add(user);
            dbContext.Profiles.Add(user.Profile);
        }
        else
        {
            user.PasswordHash = passwordHasher.HashPassword(request.Password);
            user.Phone = NormalizePhone(request.Phone);
            user.Status = InactiveStatus;
            user.EmailVerified = false;
            user.UpdatedAt = utcNow;

            if (user.Profile is null)
            {
                user.Profile = new Profile
                {
                    UserId = user.UserId,
                    DisplayName = GetRequestedDisplayName(request, email),
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow
                };
                dbContext.Profiles.Add(user.Profile);
            }
            else
            {
                user.Profile.DisplayName = GetRequestedDisplayName(request, email);
                user.Profile.UpdatedAt = utcNow;
            }
        }

        EnsureUserHasRole(user, defaultRole, utcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await IssueOtpChallengeAsync(
            user,
            AuthOtpPurpose.EmailVerification,
            "Tài khoản đã được tạo. Hãy nhập mã OTP được gửi về email để kích hoạt.",
            cancellationToken);
    }

    public async Task<AuthResult> VerifyEmailOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        var user = await LoadUserWithProfileAndRolesAsync(request.Email, cancellationToken);
        if (user is null)
        {
            return new AuthResult
            {
                Success = false,
                ErrorCode = AuthErrorCodes.UserNotFound,
                Error = "Không tìm thấy tài khoản với email này."
            };
        }

        if (IsBlocked(user.Status))
        {
            return new AuthResult
            {
                Success = false,
                ErrorCode = AuthErrorCodes.AccountBlocked,
                Error = $"Tài khoản đang ở trạng thái '{user.Status}', không thể xác thực."
            };
        }

        var otpResult = await userOtpService.ConsumeOtpAsync(
            user.UserId,
            user.Email,
            AuthOtpPurpose.EmailVerification,
            request.Otp,
            cancellationToken);

        if (!otpResult.Success)
        {
            return new AuthResult
            {
                Success = false,
                ErrorCode = otpResult.ErrorCode,
                Error = otpResult.ErrorMessage
            };
        }

        user.EmailVerified = true;
        user.Status = ActiveStatus;
        user.UpdatedAt = DateTime.UtcNow;

        var tokenResult = tokenService.GenerateTokens(user);
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.UserId,
            TokenHash = tokenResult.RefreshTokenHash,
            ExpiresAt = tokenResult.RefreshTokenExpiresAt,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResult
        {
            Success = true,
            Response = CreateAuthResponse(user, tokenResult),
            Tokens = tokenResult
        };
    }

    public async Task<AuthResult> ResendEmailVerificationOtpAsync(ResendOtpRequest request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(item => item.Profile)
            .FirstOrDefaultAsync(item => item.Email == NormalizeEmail(request.Email), cancellationToken);

        if (user is null)
        {
            return new AuthResult
            {
                Success = false,
                ErrorCode = AuthErrorCodes.UserNotFound,
                Error = "Không tìm thấy tài khoản với email này."
            };
        }

        if (user.EmailVerified)
        {
            return new AuthResult
            {
                Success = false,
                Error = "Email này đã được xác thực.",
                ErrorCode = AuthErrorCodes.EmailAlreadyExists
            };
        }

        if (IsBlocked(user.Status))
        {
            return new AuthResult
            {
                Success = false,
                ErrorCode = AuthErrorCodes.AccountBlocked,
                Error = $"Tài khoản đang ở trạng thái '{user.Status}', không thể gửi lại OTP."
            };
        }

        return await IssueOtpChallengeAsync(
            user,
            AuthOtpPurpose.EmailVerification,
            "Mã OTP xác thực email mới đã được tạo.",
            cancellationToken);
    }

    public async Task<AuthResult> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(item => item.Profile)
            .FirstOrDefaultAsync(item => item.Email == NormalizeEmail(request.Email), cancellationToken);

        if (user is null)
        {
            return new AuthResult
            {
                Success = false,
                ErrorCode = AuthErrorCodes.UserNotFound,
                Error = "Không tìm thấy tài khoản với email này."
            };
        }

        if (IsBlocked(user.Status))
        {
            return new AuthResult
            {
                Success = false,
                ErrorCode = AuthErrorCodes.AccountBlocked,
                Error = $"Tài khoản đang ở trạng thái '{user.Status}', không thể khôi phục mật khẩu."
            };
        }

        if (!user.EmailVerified || !string.Equals(user.Status, ActiveStatus, StringComparison.OrdinalIgnoreCase))
        {
            return new AuthResult
            {
                Success = false,
                ErrorCode = AuthErrorCodes.EmailVerificationRequired,
                Error = "Tài khoản chưa xác thực email nên chưa thể khôi phục mật khẩu."
            };
        }

        return await IssueOtpChallengeAsync(
            user,
            AuthOtpPurpose.PasswordReset,
            "Mã OTP đặt lại mật khẩu đã được gửi tới email của bạn.",
            cancellationToken);
    }

    public async Task<AuthResult> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await LoadUserWithProfileAndRolesAsync(request.Email, cancellationToken);
        if (user is null)
        {
            return new AuthResult
            {
                Success = false,
                ErrorCode = AuthErrorCodes.UserNotFound,
                Error = "Không tìm thấy tài khoản với email này."
            };
        }

        if (IsBlocked(user.Status))
        {
            return new AuthResult
            {
                Success = false,
                ErrorCode = AuthErrorCodes.AccountBlocked,
                Error = $"Tài khoản đang ở trạng thái '{user.Status}', không thể đặt lại mật khẩu."
            };
        }

        var otpResult = await userOtpService.ConsumeOtpAsync(
            user.UserId,
            user.Email,
            AuthOtpPurpose.PasswordReset,
            request.Otp,
            cancellationToken);

        if (!otpResult.Success)
        {
            return new AuthResult
            {
                Success = false,
                ErrorCode = otpResult.ErrorCode,
                Error = otpResult.ErrorMessage
            };
        }

        user.PasswordHash = passwordHasher.HashPassword(request.Password);
        user.UpdatedAt = DateTime.UtcNow;

        await RevokeAllRefreshTokensAsync(user.UserId, cancellationToken);

        var tokenResult = tokenService.GenerateTokens(user);
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.UserId,
            TokenHash = tokenResult.RefreshTokenHash,
            ExpiresAt = tokenResult.RefreshTokenExpiresAt,
            CreatedAt = DateTime.UtcNow
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

    private async Task<AuthResult> LoginInternalAsync(
        LoginRequest request,
        string? requiredRoleCode,
        CancellationToken cancellationToken)
    {
        var user = await LoadUserWithProfileAndRolesAsync(request.Email, cancellationToken);

        if (user is null || !passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return new AuthResult
            {
                Success = false,
                ErrorCode = AuthErrorCodes.InvalidCredentials,
                Error = "Email hoặc mật khẩu không đúng."
            };
        }

        if (IsBlocked(user.Status))
        {
            return new AuthResult
            {
                Success = false,
                ErrorCode = AuthErrorCodes.AccountBlocked,
                Error = $"Tài khoản đang ở trạng thái '{user.Status}', không được phép đăng nhập."
            };
        }

        if (!string.IsNullOrWhiteSpace(requiredRoleCode) && !UserHasRole(user, requiredRoleCode))
        {
            return new AuthResult
            {
                Success = false,
                ErrorCode = AuthErrorCodes.AccountBlocked,
                Error = $"Tài khoản phải có quyền '{requiredRoleCode}'."
            };
        }

        if (!user.EmailVerified || !string.Equals(user.Status, ActiveStatus, StringComparison.OrdinalIgnoreCase))
        {
            return await IssueOtpChallengeAsync(
                user,
                AuthOtpPurpose.EmailVerification,
                "Tài khoản chưa xác thực. Chúng tôi đã tạo một mã OTP mới để bạn kích hoạt email.",
                cancellationToken);
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

    private async Task<AuthResult> RefreshInternalAsync(
        string refreshToken,
        string? requiredRoleCode,
        CancellationToken cancellationToken)
    {
        var refreshTokenHash = tokenService.ComputeRefreshTokenHash(refreshToken);
        var storedToken = await dbContext.RefreshTokens
            .Include(item => item.User)
            .ThenInclude(item => item.Profile)
            .Include(item => item.User)
            .ThenInclude(item => item.UserRoles)
            .ThenInclude(item => item.Role)
            .FirstOrDefaultAsync(item => item.TokenHash == refreshTokenHash, cancellationToken);

        if (storedToken is null || storedToken.RevokedAt.HasValue || storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            return new AuthResult
            {
                Success = false,
                ErrorCode = AuthErrorCodes.InvalidCredentials,
                Error = "Refresh token không hợp lệ hoặc đã hết hạn."
            };
        }

        if (IsBlocked(storedToken.User.Status))
        {
            return new AuthResult
            {
                Success = false,
                ErrorCode = AuthErrorCodes.AccountBlocked,
                Error = $"Tài khoản đang ở trạng thái '{storedToken.User.Status}', không thể làm mới đăng nhập."
            };
        }

        if (!storedToken.User.EmailVerified || !string.Equals(storedToken.User.Status, ActiveStatus, StringComparison.OrdinalIgnoreCase))
        {
            return new AuthResult
            {
                Success = false,
                ErrorCode = AuthErrorCodes.EmailVerificationRequired,
                Error = "Tài khoản chưa xác thực email."
            };
        }

        if (!string.IsNullOrWhiteSpace(requiredRoleCode) && !UserHasRole(storedToken.User, requiredRoleCode))
        {
            return new AuthResult
            {
                Success = false,
                ErrorCode = AuthErrorCodes.AccountBlocked,
                Error = $"Tài khoản phải có quyền '{requiredRoleCode}'."
            };
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

    private async Task<AuthResult> IssueOtpChallengeAsync(
        User user,
        string purpose,
        string successMessage,
        CancellationToken cancellationToken)
    {
        var displayName = GetDisplayName(user);
        var (otp, expiresAtUtc) = await userOtpService.CreateOtpAsync(user, purpose, cancellationToken);

        var challenge = new OtpChallengeResponse
        {
            Email = user.Email,
            Purpose = purpose,
            Message = successMessage,
            ExpiresAtUtc = expiresAtUtc,
            EmailDispatched = true
        };

        try
        {
            if (string.Equals(purpose, AuthOtpPurpose.PasswordReset, StringComparison.Ordinal))
            {
                await authEmailService.SendPasswordResetOtpAsync(user.Email, displayName, otp, cancellationToken);
            }
            else
            {
                await authEmailService.SendEmailVerificationOtpAsync(user.Email, displayName, otp, cancellationToken);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to send {Purpose} OTP email to {Email}.", purpose, user.Email);
            challenge.EmailDispatched = false;
            challenge.Message = string.Equals(purpose, AuthOtpPurpose.PasswordReset, StringComparison.Ordinal)
                ? "Mã OTP đã được tạo nhưng chưa gửi được email. Bạn hãy thử gửi lại mã khôi phục."
                : "Mã OTP đã được tạo nhưng chưa gửi được email. Bạn hãy thử gửi lại OTP xác thực.";
        }

        return new AuthResult
        {
            Success = true,
            Challenge = challenge
        };
    }

    private void EnsureUserHasRole(User user, Role role, DateTime createdAtUtc)
    {
        if (user.UserRoles.Any(item => item.RoleId == role.RoleId))
        {
            return;
        }

        var userRole = new UserRole
        {
            User = user,
            Role = role,
            CreatedAt = createdAtUtc
        };

        user.UserRoles.Add(userRole);
        dbContext.UserRoles.Add(userRole);
    }

    private async Task<User?> LoadUserWithProfileAndRolesAsync(string email, CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .Include(item => item.Profile)
            .Include(item => item.UserRoles)
            .ThenInclude(item => item.Role)
            .FirstOrDefaultAsync(item => item.Email == NormalizeEmail(email), cancellationToken);
    }

    private async Task RevokeAllRefreshTokensAsync(Guid userId, CancellationToken cancellationToken)
    {
        var tokens = await dbContext.RefreshTokens
            .Where(item => item.UserId == userId && !item.RevokedAt.HasValue)
            .ToListAsync(cancellationToken);

        var revokedAtUtc = DateTime.UtcNow;
        foreach (var token in tokens)
        {
            token.RevokedAt = revokedAtUtc;
        }
    }

    private static bool UserHasRole(User user, string roleCode)
    {
        return user.UserRoles.Any(item =>
            item.Role is not null &&
            string.Equals(item.Role.RoleCode, roleCode, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsBlocked(string? status)
    {
        return string.Equals(status, "banned", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(status, "remove", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static string? NormalizePhone(string? phone)
    {
        return string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
    }

    private static string GetRequestedDisplayName(RegisterRequest request, string email)
    {
        return string.IsNullOrWhiteSpace(request.DisplayName)
            ? email
            : request.DisplayName.Trim();
    }

    private static string GetDisplayName(User user)
    {
        if (!string.IsNullOrWhiteSpace(user.Profile?.DisplayName))
        {
            return user.Profile.DisplayName;
        }

        return user.Email;
    }

    private static AuthResponse CreateAuthResponse(User user, TokenResult tokenResult)
    {
        return new AuthResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            DisplayName = GetDisplayName(user),
            Status = user.Status,
            EmailVerified = user.EmailVerified,
            Roles = user.UserRoles
                .Where(item => item.Role is not null && !string.IsNullOrWhiteSpace(item.Role.RoleCode))
                .Select(item => item.Role!.RoleCode)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            AccessTokenExpiresAt = tokenResult.AccessTokenExpiresAt,
            RefreshTokenExpiresAt = tokenResult.RefreshTokenExpiresAt
        };
    }
}
