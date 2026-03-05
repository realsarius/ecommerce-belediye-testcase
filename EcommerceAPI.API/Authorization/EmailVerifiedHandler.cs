using Microsoft.AspNetCore.Authorization;

namespace EcommerceAPI.API.Authorization;

public sealed class EmailVerifiedHandler : AuthorizationHandler<EmailVerifiedRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        EmailVerifiedRequirement requirement)
    {
        var isVerified = context.User.FindFirst("email_verified")?.Value;
        if (string.Equals(isVerified, "true", StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
