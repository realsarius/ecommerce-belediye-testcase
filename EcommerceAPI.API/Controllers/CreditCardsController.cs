using EcommerceAPI.Business.Abstract;
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

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }

    [HttpGet]
    public async Task<IActionResult> GetMyCards()
    {
        var userId = GetCurrentUserId();
        var result = await _creditCardService.GetUserCardsAsync(userId);
        
        if (result.Success)
        {
            return Ok(result.Data);
        }
        return BadRequest(result);
    }

    [HttpPost]
    public async Task<IActionResult> AddCard([FromBody] AddCreditCardRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _creditCardService.AddCardAsync(userId, request);
        
        if (result.Success)
        {

            return Created("", result.Data);
        }
        return BadRequest(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCard(int id)
    {
        var userId = GetCurrentUserId();
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
        var userId = GetCurrentUserId();
        
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
