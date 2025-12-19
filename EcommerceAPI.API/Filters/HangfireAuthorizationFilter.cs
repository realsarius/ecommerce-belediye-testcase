using Hangfire.Dashboard;

namespace EcommerceAPI.API.Filters;
// Development ortamında herkese izin ver, productionda sakın kullanma
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {

        return true;
    }
}
