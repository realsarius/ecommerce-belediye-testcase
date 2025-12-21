using EcommerceAPI.Core.DTOs;
using EcommerceAPI.Core.Entities;
using EcommerceAPI.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShippingAddressController : ControllerBase
{
    private readonly IRepository<ShippingAddress> _addressRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ShippingAddressController(
        IRepository<ShippingAddress> addressRepository,
        IUnitOfWork unitOfWork)
    {
        _addressRepository = addressRepository;
        _unitOfWork = unitOfWork;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }

    [HttpGet]
    public async Task<ActionResult<List<ShippingAddressDto>>> GetMyAddresses()
    {
        var userId = GetCurrentUserId();
        var addresses = await _addressRepository.FindAsync(a => a.UserId == userId);
        
        return Ok(addresses.Select(a => new ShippingAddressDto
        {
            Id = a.Id,
            Title = a.Title,
            FullName = a.FullName,
            Phone = a.Phone,
            City = a.City,
            District = a.District,
            AddressLine = a.AddressLine,
            PostalCode = a.PostalCode,
            IsDefault = a.IsDefault
        }).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<ShippingAddressDto>> CreateAddress([FromBody] CreateShippingAddressRequest request)
    {
        var userId = GetCurrentUserId();

        var address = new ShippingAddress
        {
            UserId = userId,
            Title = request.Title,
            FullName = request.FullName,
            Phone = request.Phone,
            City = request.City,
            District = request.District,
            AddressLine = request.AddressLine,
            PostalCode = request.PostalCode,
            IsDefault = request.IsDefault
        };

        await _addressRepository.AddAsync(address);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new ShippingAddressDto
        {
            Id = address.Id,
            Title = address.Title,
            FullName = address.FullName,
            Phone = address.Phone,
            City = address.City,
            District = address.District,
            AddressLine = address.AddressLine,
            PostalCode = address.PostalCode,
            IsDefault = address.IsDefault
        });
    }
}
