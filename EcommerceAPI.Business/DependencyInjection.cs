using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Business.Mappers;
using EcommerceAPI.Business.Validators;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceAPI.Business;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services, string connectionString)
    {
        services.AddDataAccessServices(connectionString);

        services.AddScoped<IProductService, ProductManager>();
        services.AddScoped<IOrderService, OrderManager>();
        services.AddScoped<ICategoryService, CategoryManager>();
        services.AddScoped<ICartService, CartManager>();
        services.AddScoped<IInventoryService, InventoryManager>();
        services.AddScoped<IAuthService, AuthManager>();
        services.AddScoped<IShippingAddressService, ShippingAddressManager>();
        services.AddScoped<ICouponService, CouponManager>();
        services.AddScoped<ISellerProfileService, SellerProfileManager>();
        services.AddScoped<ICreditCardService, CreditCardManager>();

        services.AddScoped<ICartMapper, CartMapper>();


        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddSingleton<IHashingService, HashingService>();

        services.AddValidatorsFromAssemblyContaining<CreateProductRequestValidator>();

        return services;
    }
}
