using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;

namespace EcommerceAPI.Infrastructure.Services;

public sealed class ResendEmailNotificationService : IEmailNotificationService
{
    private readonly IResend _resend;
    private readonly EmailNotificationSettings _settings;
    private readonly ILogger<ResendEmailNotificationService> _logger;

    public ResendEmailNotificationService(
        IResend resend,
        IOptions<EmailNotificationSettings> options,
        ILogger<ResendEmailNotificationService> logger)
    {
        _resend = resend;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(toEmail))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_settings.FromAddress))
        {
            _logger.LogWarning(
                "Email notification skipped because sender address is not configured for Resend");
            return false;
        }

        var message = new EmailMessage
        {
            From = BuildFromAddress(),
            Subject = subject,
            HtmlBody = htmlBody
        };
        message.To.Add(toEmail);

        try
        {
            await _resend.EmailSendAsync(message, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Resend email notification could not be delivered. ToEmail={ToEmail}, Subject={Subject}",
                toEmail,
                subject);
            return false;
        }
    }

    private string BuildFromAddress()
    {
        if (string.IsNullOrWhiteSpace(_settings.FromName))
        {
            return _settings.FromAddress.Trim();
        }

        return $"{_settings.FromName.Trim()} <{_settings.FromAddress.Trim()}>";
    }
}
