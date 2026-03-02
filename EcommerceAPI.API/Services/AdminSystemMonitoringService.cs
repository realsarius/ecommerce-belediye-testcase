using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;
using Hangfire;
using Hangfire.Storage.Monitoring;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EcommerceAPI.API.Services;

public sealed class AdminSystemMonitoringService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HealthCheckService _healthCheckService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminSystemMonitoringService> _logger;

    public AdminSystemMonitoringService(
        HealthCheckService healthCheckService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<AdminSystemMonitoringService> logger)
    {
        _healthCheckService = healthCheckService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IDataResult<AdminSystemHealthDto>> GetSystemHealthAsync(
        int failedJobsLimit = 5,
        CancellationToken cancellationToken = default)
    {
        var healthReport = await _healthCheckService.CheckHealthAsync(_ => true, cancellationToken);
        var checkedAt = DateTime.UtcNow;

        var services = healthReport.Entries
            .Where(entry => entry.Key != "self")
            .Select(entry => new AdminServiceHealthDto
            {
                Name = NormalizeServiceName(entry.Key),
                Status = entry.Value.Status.ToString(),
                Description = entry.Value.Description ?? GetDefaultDescription(entry.Key, entry.Value.Status.ToString()),
                CheckedAt = checkedAt,
                ResponseTimeMs = (long)entry.Value.Duration.TotalMilliseconds,
            })
            .OrderBy(service => service.Name)
            .ToList();

        services.Add(await CheckRabbitMqAsync(cancellationToken));
        services.Add(await CheckElasticsearchAsync(cancellationToken));

        var hangfire = GetHangfireSummary(failedJobsLimit);
        var overallStatus = CalculateOverallStatus(services.Select(service => service.Status));

        return new SuccessDataResult<AdminSystemHealthDto>(new AdminSystemHealthDto
        {
            OverallStatus = overallStatus,
            GeneratedAt = checkedAt,
            Services = services.OrderBy(service => service.Name).ToList(),
            Hangfire = hangfire,
        });
    }

    public async Task<IDataResult<List<AdminErrorLogDto>>> GetErrorLogsAsync(
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 100);

        try
        {
            var client = _httpClientFactory.CreateClient("elasticsearch");
            var payload = new
            {
                size = safeLimit,
                sort = new object[]
                {
                    new Dictionary<string, object>
                    {
                        ["@timestamp"] = new { order = "desc" }
                    }
                },
                query = new
                {
                    @bool = new
                    {
                        filter = new object[]
                        {
                            new
                            {
                                terms = new Dictionary<string, string[]>
                                {
                                    ["level"] = new[] { "Error", "Fatal" }
                                }
                            }
                        }
                    }
                }
            };

            using var response = await client.PostAsync(
                "/ecommerce-logs-*/_search",
                new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json"),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Elasticsearch hata logu sorgusu başarısız oldu. StatusCode={StatusCode}", response.StatusCode);
                return new SuccessDataResult<List<AdminErrorLogDto>>(new List<AdminErrorLogDto>());
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var logs = new List<AdminErrorLogDto>();
            if (!document.RootElement.TryGetProperty("hits", out var hitsNode) ||
                !hitsNode.TryGetProperty("hits", out var hitItems))
            {
                return new SuccessDataResult<List<AdminErrorLogDto>>(logs);
            }

            foreach (var hit in hitItems.EnumerateArray())
            {
                if (!hit.TryGetProperty("_source", out var source))
                {
                    continue;
                }

                logs.Add(new AdminErrorLogDto
                {
                    Timestamp = ParseDateTime(source, "@timestamp") ?? ParseDateTime(source, "Timestamp"),
                    Level = ReadString(source, "level") ?? ReadString(source, "Level") ?? "Error",
                    Message =
                        ReadString(source, "RenderedMessage")
                        ?? ReadString(source, "renderedMessage")
                        ?? ReadString(source, "message")
                        ?? ReadString(source, "MessageTemplate")
                        ?? "Hata mesajı alınamadı",
                    Exception = ReadString(source, "Exception") ?? ReadString(source, "exception"),
                    CorrelationId = ReadNestedString(source, "Properties", "CorrelationId")
                        ?? ReadNestedString(source, "properties", "CorrelationId"),
                });
            }

            return new SuccessDataResult<List<AdminErrorLogDto>>(logs);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Elasticsearch hata logları alınırken hata oluştu.");
            return new SuccessDataResult<List<AdminErrorLogDto>>(new List<AdminErrorLogDto>());
        }
    }

    private async Task<AdminServiceHealthDto> CheckRabbitMqAsync(CancellationToken cancellationToken)
    {
        var host = _configuration["RabbitMQ:Host"]
                   ?? Environment.GetEnvironmentVariable("RABBITMQ_HOST")
                   ?? "localhost";
        var port = _configuration.GetValue("RabbitMQ:Port", 5672);
        var checkedAt = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var tcpClient = new TcpClient();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(3));

            await tcpClient.ConnectAsync(host, port, timeoutCts.Token);
            stopwatch.Stop();

            return new AdminServiceHealthDto
            {
                Name = "RabbitMQ",
                Status = "Healthy",
                Description = $"{host}:{port} erişilebilir.",
                CheckedAt = checkedAt,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "RabbitMQ health probe başarısız oldu.");

            return new AdminServiceHealthDto
            {
                Name = "RabbitMQ",
                Status = "Unhealthy",
                Description = $"{host}:{port} erişilemiyor.",
                CheckedAt = checkedAt,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
            };
        }
    }

    private async Task<AdminServiceHealthDto> CheckElasticsearchAsync(CancellationToken cancellationToken)
    {
        var checkedAt = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var client = _httpClientFactory.CreateClient("elasticsearch");
            using var response = await client.GetAsync("/", cancellationToken);
            stopwatch.Stop();

            return new AdminServiceHealthDto
            {
                Name = "Elasticsearch",
                Status = response.IsSuccessStatusCode ? "Healthy" : "Degraded",
                Description = response.IsSuccessStatusCode
                    ? "Elasticsearch erişilebilir."
                    : $"Elasticsearch yanıt verdi ancak durum kodu {response.StatusCode}.",
                CheckedAt = checkedAt,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Elasticsearch health probe başarısız oldu.");

            return new AdminServiceHealthDto
            {
                Name = "Elasticsearch",
                Status = "Unhealthy",
                Description = "Elasticsearch erişilemiyor.",
                CheckedAt = checkedAt,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
            };
        }
    }

    private static AdminHangfireSummaryDto GetHangfireSummary(int failedJobsLimit)
    {
        try
        {
            var monitoringApi = JobStorage.Current.GetMonitoringApi();
            var statistics = monitoringApi.GetStatistics();
            var failedJobs = monitoringApi.FailedJobs(0, Math.Clamp(failedJobsLimit, 1, 20))
                .Select(job => new AdminHangfireFailedJobDto
                {
                    Id = job.Key,
                    Reason = job.Value.Reason,
                    ExceptionType = job.Value.ExceptionType,
                    ExceptionMessage = job.Value.ExceptionMessage,
                    FailedAt = job.Value.FailedAt,
                })
                .ToList();

            return new AdminHangfireSummaryDto
            {
                Enabled = true,
                ProcessingCount = statistics.Processing,
                FailedCount = statistics.Failed,
                ScheduledCount = statistics.Scheduled,
                EnqueuedCount = statistics.Enqueued,
                SucceededCount = statistics.Succeeded,
                FailedJobs = failedJobs,
            };
        }
        catch
        {
            return new AdminHangfireSummaryDto
            {
                Enabled = false,
            };
        }
    }

    private static string NormalizeServiceName(string rawName)
    {
        return rawName.ToLowerInvariant() switch
        {
            "postgresql" => "PostgreSQL",
            "redis" => "Redis",
            _ => rawName,
        };
    }

    private static string GetDefaultDescription(string rawName, string status)
    {
        return $"{NormalizeServiceName(rawName)} durumu: {status}.";
    }

    private static string CalculateOverallStatus(IEnumerable<string> statuses)
    {
        var normalized = statuses.ToList();
        if (normalized.Any(status => string.Equals(status, "Unhealthy", StringComparison.OrdinalIgnoreCase)))
        {
            return "Unhealthy";
        }

        if (normalized.Any(status => string.Equals(status, "Degraded", StringComparison.OrdinalIgnoreCase)))
        {
            return "Degraded";
        }

        return "Healthy";
    }

    private static string? ReadString(JsonElement source, string propertyName)
    {
        if (!source.TryGetProperty(propertyName, out var node))
        {
            return null;
        }

        return node.ValueKind switch
        {
            JsonValueKind.String => node.GetString(),
            JsonValueKind.Number => node.GetRawText(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            _ => null,
        };
    }

    private static string? ReadNestedString(JsonElement source, string parentName, string propertyName)
    {
        if (!source.TryGetProperty(parentName, out var parent))
        {
            return null;
        }

        if (parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(propertyName, out var child))
        {
            return child.ValueKind == JsonValueKind.String ? child.GetString() : child.GetRawText();
        }

        return null;
    }

    private static DateTime? ParseDateTime(JsonElement source, string propertyName)
    {
        var raw = ReadString(source, propertyName);
        return DateTime.TryParse(raw, out var parsed) ? parsed : null;
    }
}
