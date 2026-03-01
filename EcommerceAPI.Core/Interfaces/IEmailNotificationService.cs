namespace EcommerceAPI.Core.Interfaces;

public interface IEmailNotificationService
{
    Task<bool> SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
