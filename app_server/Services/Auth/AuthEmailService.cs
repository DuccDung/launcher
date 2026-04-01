using System.Net;
using app_server.Options;
using Microsoft.Extensions.Options;

namespace app_server.Services.Auth;

public class AuthEmailService(
    IEmailSender emailSender,
    IOptions<EmailOptions> emailOptions,
    IOptions<OtpOptions> otpOptions) : IAuthEmailService
{
    private readonly EmailOptions _emailOptions = emailOptions.Value;
    private readonly OtpOptions _otpOptions = otpOptions.Value;

    public Task SendEmailVerificationOtpAsync(
        string to,
        string displayName,
        string otp,
        CancellationToken cancellationToken = default)
    {
        return emailSender.SendAsync(
            to,
            $"Mã OTP xác thực tài khoản {_emailOptions.BrandName}",
            BuildOtpHtml(
                displayName,
                otp,
                "Xác thực email của bạn",
                "Bạn đang tạo tài khoản mới. Hãy nhập mã OTP bên dưới để kích hoạt tài khoản và bắt đầu sử dụng ngay.",
                Math.Max(1, _otpOptions.ExpireMinutes),
                "Nếu bạn không thực hiện đăng ký này, hãy bỏ qua email."),
            cancellationToken);
    }

    public Task SendPasswordResetOtpAsync(
        string to,
        string displayName,
        string otp,
        CancellationToken cancellationToken = default)
    {
        return emailSender.SendAsync(
            to,
            $"Mã OTP đặt lại mật khẩu {_emailOptions.BrandName}",
            BuildOtpHtml(
                displayName,
                otp,
                "Đặt lại mật khẩu an toàn",
                "Chúng tôi đã nhận được yêu cầu khôi phục mật khẩu. Hãy dùng mã OTP này để đặt lại mật khẩu cho tài khoản của bạn.",
                Math.Max(1, _otpOptions.PasswordResetExpireMinutes),
                "Nếu bạn không yêu cầu đổi mật khẩu, hãy đổi mật khẩu email của bạn và bỏ qua thông báo này."),
            cancellationToken);
    }

    private string BuildOtpHtml(
        string displayName,
        string otp,
        string title,
        string description,
        int expireMinutes,
        string footer)
    {
        var safeBrand = WebUtility.HtmlEncode(_emailOptions.BrandName);
        var safeName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(displayName) ? "game thủ" : displayName.Trim());
        var safeOtp = WebUtility.HtmlEncode(otp);
        var safeTitle = WebUtility.HtmlEncode(title);
        var safeDescription = WebUtility.HtmlEncode(description);
        var safeFooter = WebUtility.HtmlEncode(footer);

        return $$"""
            <!DOCTYPE html>
            <html lang="vi">
            <head>
                <meta charset="UTF-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                <title>{{safeTitle}}</title>
            </head>
            <body style="margin:0;padding:24px;background:#060606;font-family:Arial,Helvetica,sans-serif;color:#f4f4f4;">
                <div style="max-width:640px;margin:0 auto;background:linear-gradient(180deg,#111215 0%,#17181d 100%);border:1px solid rgba(255,255,255,0.08);border-radius:24px;overflow:hidden;box-shadow:0 28px 60px rgba(0,0,0,0.34);">
                    <div style="padding:28px 32px;background:radial-gradient(circle at top right,rgba(226,132,67,0.18),transparent 34%),#0b0c0f;border-bottom:1px solid rgba(255,255,255,0.08);">
                        <div style="display:inline-flex;align-items:center;gap:10px;padding:8px 14px;border-radius:999px;background:rgba(226,132,67,0.14);color:#ffd1a8;font-size:12px;font-weight:700;letter-spacing:.12em;text-transform:uppercase;">
                            {{safeBrand}}
                        </div>
                        <h1 style="margin:18px 0 10px;font-size:30px;line-height:1.15;color:#ffffff;">{{safeTitle}}</h1>
                        <p style="margin:0;font-size:15px;line-height:1.75;color:#d2d2d8;">Xin chào {{safeName}}, {{safeDescription}}</p>
                    </div>
                    <div style="padding:32px;">
                        <div style="padding:24px;border-radius:20px;background:linear-gradient(180deg,rgba(255,255,255,0.05),rgba(255,255,255,0.025));border:1px solid rgba(255,255,255,0.08);text-align:center;">
                            <div style="margin-bottom:12px;font-size:13px;letter-spacing:.14em;text-transform:uppercase;color:#9b9ba5;">Mã OTP của bạn</div>
                            <div style="display:inline-block;padding:18px 28px;border-radius:18px;background:#fff6ef;border:1px dashed #e28443;font-size:36px;font-weight:800;letter-spacing:10px;color:#a84f17;">
                                {{safeOtp}}
                            </div>
                            <p style="margin:16px 0 0;font-size:14px;line-height:1.7;color:#c7c7ce;">Mã này có hiệu lực trong <strong style="color:#ffffff;">{{expireMinutes}} phút</strong>.</p>
                        </div>
                        <div style="margin-top:22px;padding:18px 20px;border-radius:18px;background:rgba(255,255,255,0.04);border:1px solid rgba(255,255,255,0.06);font-size:14px;line-height:1.8;color:#bcbcc6;">
                            {{safeFooter}}
                        </div>
                    </div>
                </div>
            </body>
            </html>
            """;
    }
}
