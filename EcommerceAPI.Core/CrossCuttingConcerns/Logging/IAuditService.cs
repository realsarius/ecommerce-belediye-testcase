namespace EcommerceAPI.Core.CrossCuttingConcerns.Logging;

public interface IAuditService
{
    void LogAction(string userId, string action, string resource, object? data = null);
    Task LogActionAsync(string userId, string action, string resource, object? data = null);
}
