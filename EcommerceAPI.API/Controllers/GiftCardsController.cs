using System.Security.Claims;
using EcommerceAPI.Application.Abstractions.ServiceContracts;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[Route("api/v1/gift-cards")]
[Authorize]
public class GiftCardsController : BaseApiController
{
    private readonly IGiftCardService _giftCardService;

    public GiftCardsController(IGiftCardService giftCardService)
    {
        _giftCardService = giftCardService;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var userId))
        {
            throw new UnauthorizedAccessException("Geçersiz kullanıcı kimliği");
        }

        return userId;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _giftCardService.GetAllAsync();
        return HandleResult(result);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _giftCardService.GetByIdAsync(id);
        return HandleResult(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateGiftCardRequest request)
    {
        var result = await _giftCardService.CreateAsync(request);
        return HandleCreatedResult(result, nameof(GetById), new { id = result.Data?.Id });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateGiftCardRequest request)
    {
        var result = await _giftCardService.UpdateAsync(id, request);
        return HandleResult(result);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMine()
    {
        var result = await _giftCardService.GetUserGiftCardsAsync(GetCurrentUserId());
        return HandleResult(result);
    }

    [HttpGet("my/summary")]
    public async Task<IActionResult> GetSummary()
    {
        var result = await _giftCardService.GetSummaryAsync(GetCurrentUserId());
        return HandleResult(result);
    }

    [HttpGet("my/history")]
    public async Task<IActionResult> GetHistory([FromQuery] int limit = 50)
    {
        var result = await _giftCardService.GetHistoryAsync(GetCurrentUserId(), Math.Clamp(limit, 1, 100));
        return HandleResult(result);
    }

    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] ValidateGiftCardRequest request)
    {
        var result = await _giftCardService.ValidateAsync(GetCurrentUserId(), request.Code, request.OrderTotal);
        return HandleResult(result);
    }
}
