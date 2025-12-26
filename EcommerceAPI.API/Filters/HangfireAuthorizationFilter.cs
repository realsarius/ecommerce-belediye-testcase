using Hangfire.Dashboard;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EcommerceAPI.API.Filters;

/// <summary>
/// Hangfire dashboard yetkilendirmesi. Development: Herkes, Production: Admin rolü gerekli.
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var env = httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        
        if (env.IsDevelopment())
            return true;
        
        return httpContext.User.Identity?.IsAuthenticated == true 
               && httpContext.User.IsInRole("Admin");
    }
}
