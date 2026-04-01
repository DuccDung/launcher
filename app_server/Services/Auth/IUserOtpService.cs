using app_server.Models;

namespace app_server.Services.Auth;

public interface IUserOtpService
{
    Task<(string Otp, DateTime ExpiresAtUtc)> CreateOtpAsync(User user, string purpose, CancellationToken cancellationToken = default);

    Task<(bool Success, string? ErrorCode, string? ErrorMessage)> ConsumeOtpAsync(
        Guid userId,
        string email,
        string purpose,
        string otp,
        CancellationToken cancellationToken = default);

    Task InvalidateActiveOtpsAsync(
        Guid userId,
        string email,
        string purpose,
        CancellationToken cancellationToken = default);
}
