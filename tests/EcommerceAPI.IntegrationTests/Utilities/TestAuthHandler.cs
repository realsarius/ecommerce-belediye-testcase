using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EcommerceAPI.IntegrationTests.Utilities;

// custom auth handler for integration tests
// reads user info from request headers to simulate different users/roles
// usage: client.DefaultRequestHeaders.Add("X-Test-UserId", "1");

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "TestScheme";
    public const string UserIdHeader = "X-Test-UserId";
    public const string RoleHeader = "X-Test-Role";
    public const string EmailHeader = "X-Test-Email";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // If no auth headers, treat as anonymous
        if (!Request.Headers.ContainsKey(UserIdHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var userId = Request.Headers[UserIdHeader].ToString();
        var role = Request.Headers.ContainsKey(RoleHeader) 
            ? Request.Headers[RoleHeader].ToString() 
            : "Customer";
        var email = Request.Headers.ContainsKey(EmailHeader)
            ? Request.Headers[EmailHeader].ToString()
            : $"testuser{userId}@test.com";

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, $"Test User {userId}")
        };

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

// Extension methods for easy auth header setup

public static class HttpClientAuthExtensions
{
    public static HttpClient AsUser(this HttpClient client, int userId, string role = "Customer")
    {
        client.DefaultRequestHeaders.Remove(TestAuthHandler.UserIdHeader);
        client.DefaultRequestHeaders.Remove(TestAuthHandler.RoleHeader);
        
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);
        
        return client;
    }

    public static HttpClient AsAdmin(this HttpClient client, int userId = 1)
    {
        return client.AsUser(userId, "Admin");
    }

    public static HttpClient AsCustomer(this HttpClient client, int userId = 1)
    {
        return client.AsUser(userId, "Customer");
    }

    public static HttpClient AsAnonymous(this HttpClient client)
    {
        client.DefaultRequestHeaders.Remove(TestAuthHandler.UserIdHeader);
        client.DefaultRequestHeaders.Remove(TestAuthHandler.RoleHeader);
        return client;
    }

    public static HttpClient AsSeller(this HttpClient client, int userId = 1)
    {
        return client.AsUser(userId, "Seller");
    }
}
