using FluentAssertions;
using System.Net;

namespace EcommerceAPI.IntegrationTests;

[Collection("Integration")]
public class AuthMiddlewareTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthMiddlewareTests(CustomWebApplicationFactory factory)
        => _client = factory.CreateClient();

    [Fact]
    public async Task Protected_Endpoint_Without_Token_Returns_401()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/auth/me"); // This endpoint requires Auth

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
