using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[Route("api/v1/admin/sellers")]
[Authorize(Roles = "Admin")]
public class AdminSellersController : BaseApiController
{
    private readonly IAdminSellerService _adminSellerService;

    public AdminSellersController(IAdminSellerService adminSellerService)
    {
        _adminSellerService = adminSellerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSellers([FromQuery] string? status = null)
    {
        var result = await _adminSellerService.GetSellersAsync(status);
        return HandleResult(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetSellerDetail(int id)
    {
        var result = await _adminSellerService.GetSellerDetailAsync(id);
        return HandleResult(result);
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateSellerStatus(int id, [FromBody] UpdateAdminSellerStatusRequest request)
    {
        var result = await _adminSellerService.UpdateSellerStatusAsync(id, request);
        return HandleResult(result);
    }

    [HttpPut("{id:int}/commission")]
    public async Task<IActionResult> UpdateSellerCommission(int id, [FromBody] UpdateAdminSellerCommissionRequest request)
    {
        var result = await _adminSellerService.UpdateSellerCommissionAsync(id, request);
        return HandleResult(result);
    }

    [HttpPut("applications/{id:int}/approve")]
    public async Task<IActionResult> ApproveApplication(int id, [FromBody] ReviewSellerApplicationRequest request)
    {
        var result = await _adminSellerService.ApproveApplicationAsync(id, request);
        return HandleResult(result);
    }

    [HttpPut("applications/{id:int}/reject")]
    public async Task<IActionResult> RejectApplication(int id, [FromBody] ReviewSellerApplicationRequest request)
    {
        var result = await _adminSellerService.RejectApplicationAsync(id, request);
        return HandleResult(result);
    }
}
