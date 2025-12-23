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

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }

    [HttpGet]
    public async Task<IActionResult> GetMyAddresses()
    {
        var userId = GetCurrentUserId();
        var result = await _shippingAddressService.GetUserAddressesAsync(userId);
        
        if (result.Success)
        {
            return Ok(result.Data);
        }
        return BadRequest(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAddress([FromBody] CreateShippingAddressRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _shippingAddressService.AddAddressAsync(userId, request);
        
        if (result.Success)
        {
            return Ok(result.Data);
        }
        return BadRequest(result);
    }
}

