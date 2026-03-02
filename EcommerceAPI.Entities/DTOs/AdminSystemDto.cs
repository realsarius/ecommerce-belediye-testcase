using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class AdminSystemHealthDto : IDto
{
    public string OverallStatus { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public List<AdminServiceHealthDto> Services { get; set; } = new();
    public AdminHangfireSummaryDto Hangfire { get; set; } = new();
}

public class AdminServiceHealthDto : IDto
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; }
    public long? ResponseTimeMs { get; set; }
}

public class AdminHangfireSummaryDto : IDto
{
    public bool Enabled { get; set; }
    public long ProcessingCount { get; set; }
    public long FailedCount { get; set; }
    public long ScheduledCount { get; set; }
    public long EnqueuedCount { get; set; }
    public long SucceededCount { get; set; }
    public List<AdminHangfireFailedJobDto> FailedJobs { get; set; } = new();
}

public class AdminHangfireFailedJobDto : IDto
{
    public string Id { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? ExceptionType { get; set; }
    public string? ExceptionMessage { get; set; }
    public DateTime? FailedAt { get; set; }
}

public class AdminErrorLogDto : IDto
{
    public DateTime? Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public string? CorrelationId { get; set; }
}
