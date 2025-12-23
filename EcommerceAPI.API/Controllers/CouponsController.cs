using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/coupons")]
public class CouponsController : ControllerBase
{
    private readonly ICouponService _couponService;

    public CouponsController(ICouponService couponService)
    {
        _couponService = couponService;
    }

    // Admin endpoints

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllCoupons()
    {
        var result = await _couponService.GetAllAsync();
        if (result.Success)
            return Ok(result);
        return BadRequest(result);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetCoupon(int id)
    {
        var result = await _couponService.GetByIdAsync(id);
        if (result.Success)
            return Ok(result);
        return NotFound(result);
    }

    [HttpGet("active")]
    [Authorize]
    public async Task<IActionResult> GetActiveCoupons()
    {
        var result = await _couponService.GetAllAsync();
        if (result.Success)
        {
            var activeCoupons = result.Data
                .Where(c => c.IsActive && c.ExpiresAt > DateTime.UtcNow)
                .ToList();
            return Ok(new Core.Utilities.Results.SuccessDataResult<List<CouponDto>>(activeCoupons));
        }
        return BadRequest(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCoupon([FromBody] CreateCouponRequest request)
    {
        var result = await _couponService.CreateAsync(request);
        if (result.Success)
            return CreatedAtAction(nameof(GetCoupon), new { id = result.Data!.Id }, result);
        return BadRequest(result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCoupon(int id, [FromBody] UpdateCouponRequest request)
    {
        var result = await _couponService.UpdateAsync(id, request);
        if (result.Success)
            return Ok(result);
        return BadRequest(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCoupon(int id)
    {
        var result = await _couponService.DeleteAsync(id);
        if (result.Success)
            return Ok(result);
        return BadRequest(result);
    }

    // User endpoints

    [HttpPost("validate")]
    [Authorize]
    public async Task<IActionResult> ValidateCoupon([FromBody] ValidateCouponRequest request)
    {
        var result = await _couponService.ValidateCouponAsync(request.Code, request.OrderTotal);
        if (result.Success)
            return Ok(result);
        return BadRequest(result);
    }
}
