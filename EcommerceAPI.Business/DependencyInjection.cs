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

        // Managers are registered via Autofac BusinessModule
        // services.AddScoped<IProductService, ProductManager>();
        // services.AddScoped<IOrderService, OrderManager>();
        // ...

        services.AddScoped<ICartMapper, CartMapper>();

        services.AddValidatorsFromAssemblyContaining<CreateProductRequestValidator>();

        return services;
    }
}
