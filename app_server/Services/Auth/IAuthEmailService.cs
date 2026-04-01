namespace app_server.Services.Auth;

public interface IAuthEmailService
{
    Task SendEmailVerificationOtpAsync(string to, string displayName, string otp, CancellationToken cancellationToken = default);

    Task SendPasswordResetOtpAsync(string to, string displayName, string otp, CancellationToken cancellationToken = default);
}
