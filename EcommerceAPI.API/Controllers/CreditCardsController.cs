using EcommerceAPI.Application.Abstractions.ServiceContracts;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class CreditCardsController : ControllerBase
{
    private readonly ICreditCardService _creditCardService;

    public CreditCardsController(ICreditCardService creditCardService)
    {
        _creditCardService = creditCardService;
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out userId) && userId > 0;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyCards()
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _creditCardService.GetUserCardsAsync(userId);
        
        if (result.Success)
        {
            return Ok(result.Data);
        }
        return BadRequest(result);
    }

    [HttpPost]
    public IActionResult AddCard([FromBody] AddCreditCardRequest request)
    {
        _ = request;
        return BadRequest(new
        {
            message = "Kart kaydetmek için checkout sırasında 'Bu kartı kaydet' seçeneğini kullanın."
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCard(int id)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _creditCardService.DeleteCardAsync(userId, id);
        
        if (result.Success)
        {
            return Ok(result);
        }
        return BadRequest(result);
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> SetDefaultCard(int id, [FromBody] SetDefaultCreditCardRequest request)
    {
        // Pragmatic REST: PATCH method with body
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }
        
        // request.IsDefault logic can be checked if needed, 
        // assuming calling this endpoint implies setting it to true or handling logic inside service
        // For now, service method SetDefaultCardAsync assumes setting to true.
        if (!request.IsDefault)
        {
             return BadRequest(new { message = "Only setting default (true) is supported via this endpoint currently." });
        }

        var result = await _creditCardService.SetDefaultCardAsync(userId, id);
        
        if (result.Success)
        {
            return Ok(result);
        }
        return BadRequest(result);
    }
}
