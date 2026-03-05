using System.Reflection;
using EcommerceAPI.API.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;

namespace EcommerceAPI.UnitTests;

public class AuthorizationAttributesTests
{
    public static IEnumerable<object[]> AdminControllerTypes()
    {
        yield return new object[] { typeof(AdminAnnouncementsController) };
        yield return new object[] { typeof(AdminCategoriesController) };
        yield return new object[] { typeof(AdminDashboardController) };
        yield return new object[] { typeof(AdminFinanceController) };
        yield return new object[] { typeof(AdminOrdersController) };
        yield return new object[] { typeof(AdminReturnsController) };
        yield return new object[] { typeof(AdminSellersController) };
        yield return new object[] { typeof(AdminSystemController) };
        yield return new object[] { typeof(AdminUsersController) };
        yield return new object[] { typeof(AdminReviewsController) };
        yield return new object[] { typeof(AdminCampaignsController) };
        yield return new object[] { typeof(AdminNotificationsController) };
    }

    public static IEnumerable<object[]> SellerControllerTypes()
    {
        yield return new object[] { typeof(SellerAnalyticsController) };
        yield return new object[] { typeof(SellerDashboardController) };
        yield return new object[] { typeof(SellerOrdersController) };
        yield return new object[] { typeof(SellerProductsController) };
        yield return new object[] { typeof(SellerProfileController) };
        yield return new object[] { typeof(SellerReviewsController) };
    }

    [Theory]
    [MemberData(nameof(AdminControllerTypes))]
    public void AdminControllerlar_Admin_Roluyle_Korunmali(Type controllerType)
    {
        var attribute = GetAuthorizeAttribute(controllerType);

        SplitRoles(attribute.Roles).Should().BeEquivalentTo(["Admin"]);
    }

    [Theory]
    [MemberData(nameof(SellerControllerTypes))]
    public void SellerControllerlar_Seller_Roluyle_Korunmali(Type controllerType)
    {
        var attribute = GetAuthorizeAttribute(controllerType);

        SplitRoles(attribute.Roles).Should().BeEquivalentTo(["Seller"]);
    }

    [Fact]
    public void AdminProductsController_Admin_ve_Seller_Icin_Korumali_Olmali()
    {
        var attribute = GetAuthorizeAttribute(typeof(AdminProductsController));

        SplitRoles(attribute.Roles).Should().BeEquivalentTo(["Admin", "Seller"]);
    }

    [Fact]
    public void AdminProductsController_BulkUpdate_Yalnizca_Admin_Olmali()
    {
        var method = typeof(AdminProductsController).GetMethod(nameof(AdminProductsController.BulkUpdateProducts));

        method.Should().NotBeNull();
        var attribute = GetAuthorizeAttribute(method!);
        SplitRoles(attribute.Roles).Should().BeEquivalentTo(["Admin"]);
    }

    [Theory]
    [InlineData(nameof(ReviewsController.CreateReview))]
    [InlineData(nameof(ReviewsController.UpdateReview))]
    [InlineData(nameof(ReviewsController.DeleteReview))]
    [InlineData(nameof(ReviewsController.CanUserReview))]
    public void PublicReviewController_Yazma_Aksiyonlari_Giris_Zorunlu_Olmali(string methodName)
    {
        var method = typeof(ReviewsController).GetMethod(methodName);

        method.Should().NotBeNull();
        var attribute = GetAuthorizeAttribute(method!);
        attribute.Roles.Should().BeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(nameof(CouponsController.GetAllCoupons))]
    [InlineData(nameof(CouponsController.GetCoupon))]
    [InlineData(nameof(CouponsController.CreateCoupon))]
    [InlineData(nameof(CouponsController.UpdateCoupon))]
    [InlineData(nameof(CouponsController.DeleteCoupon))]
    public void Coupon_Yonetim_Aksiyonlari_Yalnizca_Admin_Olmali(string methodName)
    {
        var method = typeof(CouponsController).GetMethod(methodName);

        method.Should().NotBeNull();
        var attribute = GetAuthorizeAttribute(method!);
        SplitRoles(attribute.Roles).Should().BeEquivalentTo(["Admin"]);
    }

    [Theory]
    [InlineData(nameof(CouponsController.GetActiveCoupons))]
    [InlineData(nameof(CouponsController.ValidateCoupon))]
    public void Coupon_Kullanici_Aksiyonlari_Yalnizca_Giris_Zorunlu_Olmali(string methodName)
    {
        var method = typeof(CouponsController).GetMethod(methodName);

        method.Should().NotBeNull();
        var attribute = GetAuthorizeAttribute(method!);
        attribute.Roles.Should().BeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(typeof(CartController), nameof(CartController.AddToCart))]
    [InlineData(typeof(CartController), nameof(CartController.Reorder))]
    [InlineData(typeof(CartController), nameof(CartController.UpdateCartItem))]
    [InlineData(typeof(CartController), nameof(CartController.RemoveFromCart))]
    [InlineData(typeof(CartController), nameof(CartController.ClearCart))]
    [InlineData(typeof(OrdersController), nameof(OrdersController.Checkout))]
    [InlineData(typeof(PaymentsController), nameof(PaymentsController.ProcessPayment))]
    [InlineData(typeof(ShippingAddressController), nameof(ShippingAddressController.CreateAddress))]
    [InlineData(typeof(ShippingAddressController), nameof(ShippingAddressController.UpdateAddress))]
    [InlineData(typeof(ShippingAddressController), nameof(ShippingAddressController.DeleteAddress))]
    public void Shopping_Aksiyonlari_EmailVerified_Policy_Ile_Korunmali(Type controllerType, string methodName)
    {
        var method = controllerType.GetMethod(methodName);

        method.Should().NotBeNull();
        var attribute = method!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault(x => string.Equals(x.Policy, "EmailVerified", StringComparison.Ordinal));

        attribute.Should().NotBeNull($"{controllerType.Name}.{methodName} EmailVerified policy ile korunmalı");
    }

    private static AuthorizeAttribute GetAuthorizeAttribute(MemberInfo member)
    {
        var attribute = member.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        attribute.Should().NotBeNull($"{member.Name} rol korumasi icermeli");
        return attribute!;
    }

    private static string[] SplitRoles(string? roles)
    {
        return (roles ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
