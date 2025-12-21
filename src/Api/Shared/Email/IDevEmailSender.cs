namespace Api.Shared.Email;

public interface IDevEmailSender
{
    Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);
}
