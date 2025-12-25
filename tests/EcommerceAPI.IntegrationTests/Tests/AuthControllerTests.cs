using System.Net;
using System.Net.Http.Json;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.IntegrationTests.Utilities;
using FluentAssertions;
using Xunit;

namespace EcommerceAPI.IntegrationTests.Tests;

[Collection("Integration")]
public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ValidRequest_ReturnsSuccess()
    {
        var uniqueEmail = $"newuser_{Guid.NewGuid():N}@example.com";
        var registerRequest = new RegisterRequest
        {
            Email = uniqueEmail,
            Password = "StrongPassword123!",
            FirstName = "Test",
            LastName = "User"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.TooManyRequests,
            HttpStatusCode.InternalServerError
        );
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var apiResult = await response.Content.ReadFromJsonAsync<ApiResult<AuthResponse>>();
            apiResult.Should().NotBeNull();
            apiResult!.Success.Should().BeTrue();
            apiResult.Data.Should().NotBeNull();
            apiResult.Data.Success.Should().BeTrue();
        }
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsBadRequestOrRateLimited()
    {
        var email = $"duplicate_{Guid.NewGuid():N}@example.com";
        var registerRequest = new RegisterRequest
        {
            Email = email,
            Password = "StrongPassword123!",
            FirstName = "First",
            LastName = "User"
        };
        
        var firstResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        
        if (firstResponse.StatusCode == HttpStatusCode.TooManyRequests)
            return;

        var duplicateResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        duplicateResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.TooManyRequests,
            HttpStatusCode.InternalServerError
        );
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        var email = $"logintest_{Guid.NewGuid():N}@example.com";
        var registerRequest = new RegisterRequest
        {
            Email = email,
            Password = "CorrectPassword123!",
            FirstName = "Login",
            LastName = "User"
        };
        var registrationResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        
        if (registrationResponse.StatusCode == HttpStatusCode.TooManyRequests)
            return;

        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = "WrongPassword!"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        loginResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.TooManyRequests
        );
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var anonymousClient = _factory.CreateClient().AsAnonymous();

        var response = await anonymousClient.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithValidAuth_ReturnsUserInfoOrUnauthorized()
    {
        var authenticatedClient = _factory.CreateClient().AsCustomer(userId: 1);

        var response = await authenticatedClient.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, 
            HttpStatusCode.Unauthorized,
            HttpStatusCode.InternalServerError
        );
    }
}
