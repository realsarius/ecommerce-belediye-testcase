using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ShippingAddressController : ControllerBase
{
    private readonly IShippingAddressService _shippingAddressService;

    public ShippingAddressController(IShippingAddressService shippingAddressService)
    {
        _shippingAddressService = shippingAddressService;
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out userId) && userId > 0;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyAddresses()
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _shippingAddressService.GetUserAddressesAsync(userId);
        
        if (result.Success)
        {
            return Ok(result.Data);
        }
        return BadRequest(result);
    }

    [HttpPost]
    [Authorize(Policy = "EmailVerified")]
    public async Task<IActionResult> CreateAddress([FromBody] CreateShippingAddressRequest request)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _shippingAddressService.AddAddressAsync(userId, request);
        
        if (result.Success)
        {
            return Created("", result.Data);
        }
        return BadRequest(result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "EmailVerified")]
    public async Task<IActionResult> UpdateAddress(int id, [FromBody] CreateShippingAddressRequest request)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _shippingAddressService.UpdateAddressAsync(userId, id, request);
        
        if (result.Success)
        {
            return Ok(result.Data);
        }
        return BadRequest(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "EmailVerified")]
    public async Task<IActionResult> DeleteAddress(int id)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _shippingAddressService.DeleteAddressAsync(userId, id);
        
        if (result.Success)
        {
            return Ok(result);
        }
        return BadRequest(result);
    }
}
