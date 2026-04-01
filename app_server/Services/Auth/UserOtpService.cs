using app_server.Models;
using app_server.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace app_server.Services.Auth;

public class UserOtpService(
    LauncherDbContext dbContext,
    IOtpGenerator otpGenerator,
    IOptions<OtpOptions> otpOptions) : IUserOtpService
{
    private readonly OtpOptions _otpOptions = otpOptions.Value;

    public async Task<(string Otp, DateTime ExpiresAtUtc)> CreateOtpAsync(
        User user,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        var email = user.Email.Trim().ToLowerInvariant();
        await InvalidateActiveOtpsAsync(user.UserId, email, purpose, cancellationToken);

        var otp = otpGenerator.GenerateOtp();
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(GetExpireMinutes(purpose));

        dbContext.UserOtps.Add(new UserOtp
        {
            UserId = user.UserId,
            Email = email,
            Purpose = purpose,
            OtpCode = otp,
            ExpiresAt = expiresAtUtc,
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return (otp, expiresAtUtc);
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> ConsumeOtpAsync(
        Guid userId,
        string email,
        string purpose,
        string otp,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var normalizedOtp = otp.Trim();

        var entity = await dbContext.UserOtps
            .Where(item =>
                item.UserId == userId &&
                item.Email == normalizedEmail &&
                item.Purpose == purpose &&
                !item.IsUsed &&
                item.OtpCode == normalizedOtp)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
        {
            return (false, AuthErrorCodes.OtpInvalid, "Mã OTP không đúng hoặc đã được sử dụng.");
        }

        if (entity.ExpiresAt <= DateTime.UtcNow)
        {
            return (false, AuthErrorCodes.OtpExpired, "Mã OTP đã hết hạn.");
        }

        entity.IsUsed = true;
        entity.UsedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    public async Task InvalidateActiveOtpsAsync(
        Guid userId,
        string email,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var activeOtps = await dbContext.UserOtps
            .Where(item =>
                item.UserId == userId &&
                item.Email == normalizedEmail &&
                item.Purpose == purpose &&
                !item.IsUsed &&
                item.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        if (activeOtps.Count == 0)
        {
            return;
        }

        var usedAt = DateTime.UtcNow;
        foreach (var item in activeOtps)
        {
            item.IsUsed = true;
            item.UsedAt = usedAt;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private int GetExpireMinutes(string purpose)
    {
        return string.Equals(purpose, AuthOtpPurpose.PasswordReset, StringComparison.Ordinal)
            ? Math.Max(1, _otpOptions.PasswordResetExpireMinutes)
            : Math.Max(1, _otpOptions.ExpireMinutes);
    }
}
