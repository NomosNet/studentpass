namespace StudentPass.Api.Services;

public interface IEmailSender
{
    Task SendAsync(string email, string subject, string message, CancellationToken cancellationToken = default);
}

public class ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string email, string subject, string message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Email to {Email} | {Subject} | {Message}", email, subject, message);
        return Task.CompletedTask;
    }
}
