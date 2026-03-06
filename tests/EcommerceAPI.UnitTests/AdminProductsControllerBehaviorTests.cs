using System.Security.Claims;
using EcommerceAPI.API.Controllers;
using EcommerceAPI.Application.Abstractions.ServiceContracts;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
            platformSellerServiceMock.Object,
            new ConfigurationBuilder().Build());

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

    [Fact]
    public async Task CreateProduct_AdminRole_WhenPlatformAutoAssignmentDisabledAndNoSellerProvided_ShouldReturnBadRequest()
    {
        var request = new CreateProductRequest
        {
            Name = "Fallback Product",
            Description = "Admin create product fallback test",
            Price = 249.90m,
            CategoryId = 1,
            SKU = "PLATFORM-002"
        };

        var productServiceMock = new Mock<IProductService>();

        var sellerProfileServiceMock = new Mock<ISellerProfileService>();
        var platformSellerServiceMock = new Mock<IPlatformSellerService>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PlatformSeller:EnableAdminAutoAssignment"] = "false"
            })
            .Build();

        var controller = new AdminProductsController(
            productServiceMock.Object,
            sellerProfileServiceMock.Object,
            platformSellerServiceMock.Object,
            configuration);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "11"),
                    new Claim(ClaimTypes.Role, "Admin")
                ], "TestAuth"))
            }
        };

        var actionResult = await controller.CreateProduct(request);

        actionResult.Should().BeOfType<BadRequestObjectResult>();
        sellerProfileServiceMock.Verify(service => service.GetByIdAsync(It.IsAny<int>()), Times.Never);
        platformSellerServiceMock.Verify(service => service.GetOrCreatePlatformSellerIdAsync(), Times.Never);
        productServiceMock.Verify(service => service.CreateProductAsync(It.IsAny<CreateProductRequest>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task CreateProduct_AdminRole_WhenSellerPickerEnabledAndSellerSelected_ShouldPassSelectedSellerId()
    {
        var request = new CreateProductRequest
        {
            SellerId = 88,
            Name = "Seller Assigned Product",
            Description = "Admin selects seller",
            Price = 399.90m,
            CategoryId = 1,
            SKU = "SELLER-003"
        };

        var productServiceMock = new Mock<IProductService>();
        int? capturedSellerId = null;
        productServiceMock
            .Setup(service => service.CreateProductAsync(request, It.IsAny<int?>()))
            .Callback<CreateProductRequest, int?>((_, sellerId) => capturedSellerId = sellerId)
            .ReturnsAsync(new SuccessDataResult<ProductDto>(new ProductDto
            {
                Id = 44,
                Name = request.Name,
                SellerId = 88
            }));

        var sellerProfileServiceMock = new Mock<ISellerProfileService>();
        sellerProfileServiceMock
            .Setup(service => service.GetByIdAsync(88))
            .ReturnsAsync(new SuccessDataResult<SellerProfileDto>(new SellerProfileDto
            {
                Id = 88,
                UserId = 1001,
                BrandName = "Selected Seller",
                IsVerified = true
            }));

        var platformSellerServiceMock = new Mock<IPlatformSellerService>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FrontendFeatures:EnableAdminProductSellerPicker"] = "true",
                ["PlatformSeller:EnableAdminAutoAssignment"] = "true"
            })
            .Build();

        var controller = new AdminProductsController(
            productServiceMock.Object,
            sellerProfileServiceMock.Object,
            platformSellerServiceMock.Object,
            configuration);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "15"),
                    new Claim(ClaimTypes.Role, "Admin")
                ], "TestAuth"))
            }
        };

        var actionResult = await controller.CreateProduct(request);

        actionResult.Should().BeOfType<CreatedAtRouteResult>();
        capturedSellerId.Should().Be(88);
        sellerProfileServiceMock.Verify(service => service.GetByIdAsync(88), Times.Once);
        platformSellerServiceMock.Verify(service => service.GetOrCreatePlatformSellerIdAsync(), Times.Never);
        productServiceMock.Verify(service => service.CreateProductAsync(request, 88), Times.Once);
    }

    [Fact]
    public async Task CreateProduct_AdminRole_WhenSellerPickerDisabledAndSellerProvided_ShouldReturnBadRequest()
    {
        var request = new CreateProductRequest
        {
            SellerId = 77,
            Name = "Blocked Seller Assign Product",
            Description = "Picker disabled",
            Price = 199.90m,
            CategoryId = 1,
            SKU = "SELLER-004"
        };

        var productServiceMock = new Mock<IProductService>();
        var sellerProfileServiceMock = new Mock<ISellerProfileService>();
        var platformSellerServiceMock = new Mock<IPlatformSellerService>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FrontendFeatures:EnableAdminProductSellerPicker"] = "false",
                ["PlatformSeller:EnableAdminAutoAssignment"] = "true"
            })
            .Build();

        var controller = new AdminProductsController(
            productServiceMock.Object,
            sellerProfileServiceMock.Object,
            platformSellerServiceMock.Object,
            configuration);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "22"),
                    new Claim(ClaimTypes.Role, "Admin")
                ], "TestAuth"))
            }
        };

        var actionResult = await controller.CreateProduct(request);

        actionResult.Should().BeOfType<BadRequestObjectResult>();
        sellerProfileServiceMock.Verify(service => service.GetByIdAsync(It.IsAny<int>()), Times.Never);
        platformSellerServiceMock.Verify(service => service.GetOrCreatePlatformSellerIdAsync(), Times.Never);
        productServiceMock.Verify(service => service.CreateProductAsync(It.IsAny<CreateProductRequest>(), It.IsAny<int?>()), Times.Never);
    }
}
