
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceAPI.Core.Utilities.IoC;

public static class ServiceTool
{
    public static IServiceProvider ServiceProvider { get; private set; }

    public static IServiceCollection Create(IServiceCollection services)
    {
        ServiceProvider = services.BuildServiceProvider();
        return services;
    }

    public static void SetProvider(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }
}
