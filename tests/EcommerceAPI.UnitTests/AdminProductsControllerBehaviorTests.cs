using System.Security.Claims;
using EcommerceAPI.API.Controllers;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace EcommerceAPI.UnitTests;

public class AdminProductsControllerBehaviorTests
{
    [Fact]
    public async Task CreateProduct_AdminRole_ShouldPassPlatformSellerIdToProductService()
    {
        var request = new CreateProductRequest
        {
            Name = "Platform Product",
            Description = "Admin create product test",
            Price = 1499.90m,
            CategoryId = 1,
            SKU = "PLATFORM-001"
        };

        var productServiceMock = new Mock<IProductService>();
        int? capturedSellerId = null;
        productServiceMock
            .Setup(service => service.CreateProductAsync(request, It.IsAny<int?>()))
            .Callback<CreateProductRequest, int?>((_, sellerId) => capturedSellerId = sellerId)
            .ReturnsAsync(new SuccessDataResult<ProductDto>(new ProductDto
            {
                Id = 42,
                Name = request.Name,
                SellerId = 7001
            }));

        var sellerProfileServiceMock = new Mock<ISellerProfileService>();
        var platformSellerServiceMock = new Mock<IPlatformSellerService>();
        platformSellerServiceMock
            .Setup(service => service.GetOrCreatePlatformSellerIdAsync())
            .ReturnsAsync(new SuccessDataResult<int>(7001));

        var controller = new AdminProductsController(
            productServiceMock.Object,
            sellerProfileServiceMock.Object,
            platformSellerServiceMock.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "9"),
                    new Claim(ClaimTypes.Role, "Admin")
                ], "TestAuth"))
            }
        };

        var actionResult = await controller.CreateProduct(request);

        actionResult.Should().BeOfType<CreatedAtRouteResult>();
        capturedSellerId.Should().Be(7001);
        platformSellerServiceMock.Verify(service => service.GetOrCreatePlatformSellerIdAsync(), Times.Once);
        productServiceMock.Verify(service => service.CreateProductAsync(request, 7001), Times.Once);
    }
}
