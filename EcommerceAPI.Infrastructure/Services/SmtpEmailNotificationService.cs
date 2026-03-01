using System.Net;
using System.Net.Mail;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EcommerceAPI.Infrastructure.Services;

public sealed class SmtpEmailNotificationService : IEmailNotificationService
{
    private readonly EmailNotificationSettings _settings;
    private readonly ILogger<SmtpEmailNotificationService> _logger;

    public SmtpEmailNotificationService(
        IOptions<EmailNotificationSettings> options,
        ILogger<SmtpEmailNotificationService> logger)
    {
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

        if (string.IsNullOrWhiteSpace(_settings.Host) || string.IsNullOrWhiteSpace(_settings.FromAddress))
        {
            _logger.LogWarning(
                "Email notification skipped because SMTP settings are incomplete. HostConfigured={HasHost}, FromConfigured={HasFrom}",
                !string.IsNullOrWhiteSpace(_settings.Host),
                !string.IsNullOrWhiteSpace(_settings.FromAddress));
            return false;
        }

        if (string.IsNullOrWhiteSpace(toEmail))
        {
            return false;
        }

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.EnableSsl,
            UseDefaultCredentials = false
        };

        if (!string.IsNullOrWhiteSpace(_settings.Username))
        {
            client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromAddress, _settings.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        message.To.Add(toEmail);

        try
        {
            await client.SendMailAsync(message, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Email notification could not be delivered. ToEmail={ToEmail}, Subject={Subject}",
                toEmail,
                subject);
            return false;
        }
    }
}
