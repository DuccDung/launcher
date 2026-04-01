using app_server.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace app_server.Services.Auth;

public class SmtpEmailSender(
    IOptions<SmtpOptions> smtpOptions,
    IOptions<EmailOptions> emailOptions) : IEmailSender
{
    private readonly SmtpOptions _smtpOptions = smtpOptions.Value;
    private readonly EmailOptions _emailOptions = emailOptions.Value;

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var fromAddress = string.IsNullOrWhiteSpace(_emailOptions.FromAddress)
            ? _smtpOptions.User?.Trim()
            : _emailOptions.FromAddress.Trim();
        var fromName = string.IsNullOrWhiteSpace(_emailOptions.FromName)
            ? string.IsNullOrWhiteSpace(_emailOptions.BrandName) ? fromAddress : _emailOptions.BrandName.Trim()
            : _emailOptions.FromName.Trim();

        if (string.IsNullOrWhiteSpace(fromAddress))
        {
            throw new InvalidOperationException("Email sender address is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_smtpOptions.Host))
        {
            throw new InvalidOperationException("SMTP host is not configured.");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        client.Timeout = (int)TimeSpan.FromSeconds(Math.Max(5, _smtpOptions.TimeoutSeconds)).TotalMilliseconds;

        await client.ConnectAsync(
            _smtpOptions.Host,
            _smtpOptions.Port,
            _smtpOptions.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(_smtpOptions.User))
        {
            await client.AuthenticateAsync(_smtpOptions.User, _smtpOptions.Pass, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
