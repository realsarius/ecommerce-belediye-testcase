using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EcommerceAPI.Infrastructure.Services;

public sealed class ConsoleEmailNotificationService : IEmailNotificationService
{
    private readonly EmailNotificationSettings _settings;
    private readonly ILogger<ConsoleEmailNotificationService> _logger;

    public ConsoleEmailNotificationService(
        IOptions<EmailNotificationSettings> options,
        ILogger<ConsoleEmailNotificationService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public Task<bool> SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            return Task.FromResult(false);
        }

        if (string.IsNullOrWhiteSpace(toEmail))
        {
            return Task.FromResult(false);
        }

        _logger.LogInformation(
            "Console email notification dispatched. ToEmail={ToEmail}, Subject={Subject}, HtmlBody={HtmlBody}",
            toEmail,
            subject,
            htmlBody);

        return Task.FromResult(true);
    }
}
