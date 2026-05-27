using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace StudentPass.Api.Services;

public class SmtpSettings
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string User { get; set; } = "";
    public string Password { get; set; } = "";
    public string From { get; set; } = "";
    /// <summary>true — SSL на подключении (порт 465); false — STARTTLS (порт 587).</summary>
    public bool UseSsl { get; set; }
}

public class SmtpEmailSender(IOptions<SmtpSettings> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string email, string subject, string message, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var mime = new MimeMessage();
        mime.From.Add(MailboxAddress.Parse(settings.From));
        mime.To.Add(MailboxAddress.Parse(email));
        mime.Subject = subject;
        mime.Body = new TextPart("plain") { Text = message };

        using var client = new SmtpClient();
        var socketOptions = settings.UseSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTlsWhenAvailable;

        await client.ConnectAsync(settings.Host, settings.Port, socketOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(settings.User))
            await client.AuthenticateAsync(settings.User, settings.Password, cancellationToken);

        await client.SendAsync(mime, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        logger.LogInformation("Email sent to {Email}, subject: {Subject}", email, subject);
    }
}
