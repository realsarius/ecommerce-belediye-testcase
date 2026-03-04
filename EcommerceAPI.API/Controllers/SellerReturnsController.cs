using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/seller/returns")]
[Authorize(Roles = "Seller")]
public class SellerReturnsController : SellerApiControllerBase
{
    private readonly IReturnRequestService _returnRequestService;
    private readonly ISellerProfileService _sellerProfileService;

    public SellerReturnsController(
        IReturnRequestService returnRequestService,
        ISellerProfileService sellerProfileService)
    {
        _returnRequestService = returnRequestService;
        _sellerProfileService = sellerProfileService;
    }

    [HttpGet]
    public async Task<IActionResult> GetReturnRequests([FromQuery] string? status = null)
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

        var result = await _returnRequestService.GetReturnRequestsAsync(status, sellerContext.SellerProfileId.Value);
        return HandleResult(result);
    }

    [HttpPut("{id:int:min(1)}/approve")]
    public async Task<IActionResult> ApproveReturnRequest(int id, [FromBody] ReviewReturnRequestRequest request)
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

        request.Status = "Approved";
        var result = await _returnRequestService.ReviewReturnRequestAsync(
            id,
            sellerContext.UserId,
            request,
            sellerContext.SellerProfileId.Value);

        return HandleResult(result);
    }

    [HttpPut("{id:int:min(1)}/reject")]
    public async Task<IActionResult> RejectReturnRequest(int id, [FromBody] ReviewReturnRequestRequest request)
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

        request.Status = "Rejected";
        var result = await _returnRequestService.ReviewReturnRequestAsync(
            id,
            sellerContext.UserId,
            request,
            sellerContext.SellerProfileId.Value);

        return HandleResult(result);
    }
}
