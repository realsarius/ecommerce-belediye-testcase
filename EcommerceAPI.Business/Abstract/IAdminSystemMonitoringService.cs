using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Abstract;

public interface IAdminSystemMonitoringService
{
    Task<IDataResult<AdminSystemHealthDto>> GetSystemHealthAsync(
        int failedJobsLimit = 5,
        CancellationToken cancellationToken = default);

    Task<IDataResult<List<AdminErrorLogDto>>> GetErrorLogsAsync(
        int limit = 20,
        CancellationToken cancellationToken = default);
}
