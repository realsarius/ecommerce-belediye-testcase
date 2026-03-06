using System.Net;
using System.Net.Http.Json;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceAPI.IntegrationTests.Tests;

[Collection("Integration")]
public class ContactControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ContactControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static string CreateUniqueForwardedFor()
    {
        var bytes = Guid.NewGuid().ToByteArray();
        var thirdOctet = bytes[0];
        var fourthOctet = bytes[1] == 0 ? (byte)1 : bytes[1];
        return $"198.51.{thirdOctet}.{fourthOctet}";
    }

    [Fact]
    public async Task Create_ShouldPersistContactMessage()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", CreateUniqueForwardedFor());
        var uniqueToken = Guid.NewGuid().ToString("N")[..8];
        var email = $"berkan-{uniqueToken}@test.com";
        var subject = $"Genel bilgi {uniqueToken}";

        var response = await client.PostAsJsonAsync("/api/v1/contact", new CreateContactMessageRequest
        {
            Name = "Berkan Sözer",
            Email = email,
            Subject = subject,
            Message = "Sipariş süreciyle ilgili birkaç detay öğrenmek istiyorum."
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.ContactMessages.Should().ContainSingle(x =>
            x.Email == email &&
            x.Subject == subject);
    }

    [Fact]
    public async Task Create_WithInvalidPayload_ShouldReturnBadRequest()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", CreateUniqueForwardedFor());

        var response = await client.PostAsJsonAsync("/api/v1/contact", new CreateContactMessageRequest
        {
            Name = "",
            Email = "invalid-email",
            Subject = "",
            Message = "kısa"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturnTooManyRequests_WhenIpExceedsHourlyLimit()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", CreateUniqueForwardedFor());

        for (var index = 0; index < 5; index++)
        {
            var okResponse = await client.PostAsJsonAsync("/api/v1/contact", new CreateContactMessageRequest
            {
                Name = $"Kullanıcı {index}",
                Email = $"contact{index}@test.com",
                Subject = "Destek",
                Message = "Siparişim hakkında destek talebi oluşturmak istiyorum."
            });

            okResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var rateLimitedResponse = await client.PostAsJsonAsync("/api/v1/contact", new CreateContactMessageRequest
        {
            Name = "Limit Kullanıcı",
            Email = "limit@test.com",
            Subject = "Destek",
            Message = "Altıncı istek ile rate limit devreye girmeli."
        });

        rateLimitedResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
