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
        if (string.IsNullOrWhiteSpace(_emailOptions.FromAddress))
        {
            throw new InvalidOperationException("Email sender address is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_smtpOptions.Host))
        {
            throw new InvalidOperationException("SMTP host is not configured.");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_emailOptions.FromName, _emailOptions.FromAddress));
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
