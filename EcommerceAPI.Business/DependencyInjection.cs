using EcommerceAPI.Business.Mappers;
using EcommerceAPI.Business.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceAPI.Business;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        // Managers are registered via Autofac BusinessModule
        // services.AddScoped<IProductService, ProductManager>();
        // services.AddScoped<IOrderService, OrderManager>();
        // ...

        services.AddScoped<ICartMapper, CartMapper>();
        services.AddScoped<IWishlistMapper, WishlistMapper>();

        services.AddValidatorsFromAssemblyContaining<CreateProductRequestValidator>();

        return services;
    }
}
