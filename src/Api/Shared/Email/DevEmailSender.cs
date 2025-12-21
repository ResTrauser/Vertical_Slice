using Microsoft.Extensions.Logging;

namespace Api.Shared.Email;

public sealed class DevEmailSender(ILogger<DevEmailSender> logger) : IDevEmailSender
{
    public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[DEV EMAIL] To: {To} Subject: {Subject} Body: {Body}", toEmail, subject, body);
        return Task.CompletedTask;
    }
}
