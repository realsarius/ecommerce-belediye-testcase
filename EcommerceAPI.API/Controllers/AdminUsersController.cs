using EcommerceAPI.Application.Abstractions.ServiceContracts;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : BaseApiController
{
    private readonly IAdminUserService _adminUserService;

    public AdminUsersController(IAdminUserService adminUserService)
    {
        _adminUserService = adminUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] AdminUsersQueryRequest request)
    {
        var result = await _adminUserService.GetUsersAsync(request);
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserDetail(int id)
    {
        var result = await _adminUserService.GetUserDetailAsync(id);
        return HandleResult(result);
    }

    [HttpPut("{id}/role")]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateUserRoleRequest request)
    {
        var result = await _adminUserService.UpdateUserRoleAsync(id, request);
        return HandleResult(result);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateUserStatusRequest request)
    {
        var result = await _adminUserService.UpdateUserStatusAsync(id, request);
        return HandleResult(result);
    }
}
