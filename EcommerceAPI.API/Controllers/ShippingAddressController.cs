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
            return Created("", result.Data);
        }
        return BadRequest(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAddress(int id, [FromBody] CreateShippingAddressRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _shippingAddressService.UpdateAddressAsync(userId, id, request);
        
        if (result.Success)
        {
            return Ok(result.Data);
        }
        return BadRequest(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAddress(int id)
    {
        var userId = GetCurrentUserId();
        var result = await _shippingAddressService.DeleteAddressAsync(userId, id);
        
        if (result.Success)
        {
            return Ok(result);
        }
        return BadRequest(result);
    }
}

