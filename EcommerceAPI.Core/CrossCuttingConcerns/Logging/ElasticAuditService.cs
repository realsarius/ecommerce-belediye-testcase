using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EcommerceAPI.Core.CrossCuttingConcerns.Logging;

/// <summary>
/// Elasticsearch tabanlı Audit Log servisi.
/// Serilog üzerinden logları Elasticsearch'e gönderir.
/// AuditLog = true property'si ile audit logları diğer loglardan ayırt edilir.
/// </summary>
public class ElasticAuditService : IAuditService
{
    private readonly ILogger<ElasticAuditService> _logger;

    public ElasticAuditService(ILogger<ElasticAuditService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void LogAction(string userId, string action, string resource, object? data = null)
    {
        var auditEntry = CreateAuditEntry(userId, action, resource, data);
        
        // AuditLog = true property'si ile Kibana'da filtreleme yapılabilir
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["AuditLog"] = true,
            ["UserId"] = userId,
            ["Action"] = action,
            ["Resource"] = resource,
            ["AuditTimestamp"] = auditEntry.Timestamp
        }))
        {
            _logger.LogInformation(
                "AUDIT: User {UserId} performed {Action} on {Resource}. Data: {AuditData}",
                userId, 
                action, 
                resource, 
                data != null ? JsonSerializer.Serialize(data) : "null"
            );
        }
    }

    /// <inheritdoc />
    public Task LogActionAsync(string userId, string action, string resource, object? data = null)
    {
        LogAction(userId, action, resource, data);
        return Task.CompletedTask;
    }

    private static AuditLogEntry CreateAuditEntry(string userId, string action, string resource, object? data)
    {
        return new AuditLogEntry
        {
            UserId = userId,
            Action = action,
            Resource = resource,
            Data = data,
            Timestamp = DateTime.UtcNow,
            MachineName = Environment.MachineName
        };
    }
}

/// <summary>
/// Audit log kaydı için model sınıfı.
/// </summary>
public class AuditLogEntry
{
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public object? Data { get; set; }
    public DateTime Timestamp { get; set; }
    public string MachineName { get; set; } = string.Empty;
}
