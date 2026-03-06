using EcommerceAPI.Application.Abstractions.ServiceContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = "Admin")]
public class AdminSystemController : BaseApiController
{
    private readonly IAdminSystemMonitoringService _adminSystemMonitoringService;

    public AdminSystemController(IAdminSystemMonitoringService adminSystemMonitoringService)
    {
        _adminSystemMonitoringService = adminSystemMonitoringService;
    }

    [HttpGet("health")]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
    {
        var result = await _adminSystemMonitoringService.GetSystemHealthAsync(cancellationToken: cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("logs/errors")]
    public async Task<IActionResult> GetErrorLogs([FromQuery] int limit = 20, CancellationToken cancellationToken = default)
    {
        var result = await _adminSystemMonitoringService.GetErrorLogsAsync(limit, cancellationToken);
        return HandleResult(result);
    }
}
