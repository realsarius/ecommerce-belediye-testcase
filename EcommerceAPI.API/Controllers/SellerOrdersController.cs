using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/seller/orders")]
[Authorize(Roles = "Seller")]
public class SellerOrdersController : SellerApiControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ISellerProfileService _sellerProfileService;

    public SellerOrdersController(IOrderService orderService, ISellerProfileService sellerProfileService)
    {
        _orderService = orderService;
        _sellerProfileService = sellerProfileService;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        var sellerContext = await GetSellerContextAsync(_sellerProfileService);
        if (sellerContext == null)
        {
            return InvalidSellerSession();
        }
        if (sellerContext.SellerProfileId == null)
        {
            return MissingSellerProfile();
        }

        var result = await _orderService.GetOrdersForSellerAsync(sellerContext.SellerProfileId.Value);
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        var sellerContext = await GetSellerContextAsync(_sellerProfileService);
        if (sellerContext == null)
        {
            return InvalidSellerSession();
        }
        if (sellerContext.SellerProfileId == null)
        {
            return MissingSellerProfile();
        }

        var result = await _orderService.GetSellerOrderAsync(sellerContext.SellerProfileId.Value, id);
        return HandleResult(result);
    }

    [HttpPut("{id}/ship")]
    public async Task<IActionResult> ShipOrder(int id, [FromBody] ShipOrderRequest request)
    {
        var sellerContext = await GetSellerContextAsync(_sellerProfileService);
        if (sellerContext == null)
        {
            return InvalidSellerSession();
        }
        if (sellerContext.SellerProfileId == null)
        {
            return MissingSellerProfile();
        }

        var result = await _orderService.ShipOrderAsync(sellerContext.SellerProfileId.Value, id, request);
        return HandleResult(result);
    }
}
