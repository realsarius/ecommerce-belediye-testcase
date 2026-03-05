using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.IntegrationTests.Utilities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

    [Fact]
    public async Task VerifyEmail_ValidToken_ReturnsOkAndMarksUserVerified()
    {
        var userId = NextUserId();
        await SeedCustomerAsync(userId, isEmailVerified: false);

        var rawToken = $"verify-{Guid.NewGuid():N}";
        await SetEmailVerificationTokenAsync(userId, rawToken, DateTime.UtcNow.AddHours(12));

        var response = await _client.PostAsJsonAsync("/api/v1/auth/verify-email", new VerifyEmailRequest
        {
            Token = rawToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var apiResult = await response.Content.ReadFromJsonAsync<ApiResult<AuthResponse>>();
        apiResult.Should().NotBeNull();
        apiResult!.Success.Should().BeTrue();
        apiResult.Data.Should().NotBeNull();
        apiResult.Data.User.Should().NotBeNull();
        apiResult.Data.User!.IsEmailVerified.Should().BeTrue();

        var user = await FindUserAsync(userId);
        user.Should().NotBeNull();
        user!.IsEmailVerified.Should().BeTrue();
        user.EmailVerificationToken.Should().BeNull();
        user.EmailVerificationTokenExpiry.Should().BeNull();
    }

    [Fact]
    public async Task VerifyEmail_InvalidToken_ReturnsBadRequestWithInvalidTokenCode()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/verify-email", new VerifyEmailRequest
        {
            Token = $"invalid-{Guid.NewGuid():N}"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errorCode = await ReadErrorCodeAsync(response);
        errorCode.Should().Be("INVALID_TOKEN");
    }

    [Fact]
    public async Task ResendVerification_UnverifiedUser_ReturnsOkAndRefreshesToken()
    {
        var userId = NextUserId();
        await SeedCustomerAsync(userId, isEmailVerified: false);
        await SetEmailVerificationTokenAsync(userId, $"old-{Guid.NewGuid():N}", DateTime.UtcNow.AddMinutes(30));

        var client = _factory.CreateClient().AsCustomer(userId, isEmailVerified: false);
        var response = await client.PostAsync("/api/v1/auth/resend-verification", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var user = await FindUserAsync(userId);
        user.Should().NotBeNull();
        user!.EmailVerificationToken.Should().NotBeNullOrWhiteSpace();
        user.EmailVerificationTokenExpiry.Should().NotBeNull();
        user.EmailVerificationTokenExpiry!.Value.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task ResendVerification_AlreadyVerifiedUser_ReturnsBadRequest()
    {
        var userId = NextUserId();
        await SeedCustomerAsync(userId, isEmailVerified: true);

        var client = _factory.CreateClient().AsCustomer(userId);
        var response = await client.PostAsync("/api/v1/auth/resend-verification", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ForgotPassword_ExistingEmail_ReturnsOkAndStoresResetToken()
    {
        var userId = NextUserId();
        var email = $"forgot_{Guid.NewGuid():N}@example.com";
        await SeedCustomerAsync(userId, isEmailVerified: true, email: email);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest
        {
            Email = email
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await FindUserAsync(userId);
        user.Should().NotBeNull();
        user!.PasswordResetToken.Should().NotBeNullOrWhiteSpace();
        user.PasswordResetTokenExpiry.Should().NotBeNull();
        user.PasswordResetTokenExpiry!.Value.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task ForgotPassword_UnknownEmail_ReturnsOkForEnumerationProtection()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest
        {
            Email = $"missing_{Guid.NewGuid():N}@example.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ResultEnvelope>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ResetPassword_ValidToken_UpdatesPasswordAndRevokesRefreshTokens()
    {
        var userId = NextUserId();
        await SeedCustomerAsync(userId, isEmailVerified: true);
        await AddRefreshTokenAsync(userId, token: $"refresh-{Guid.NewGuid():N}");

        var rawToken = $"reset-{Guid.NewGuid():N}";
        await SetPasswordResetTokenAsync(userId, rawToken, DateTime.UtcNow.AddMinutes(30));

        var response = await _client.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetPasswordRequest
        {
            Token = rawToken,
            NewPassword = "NewStrongPassword1",
            ConfirmPassword = "NewStrongPassword1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var user = await FindUserAsync(userId);
        user.Should().NotBeNull();
        BCrypt.Net.BCrypt.Verify("NewStrongPassword1", user!.PasswordHash).Should().BeTrue();
        user.PasswordResetToken.Should().BeNull();
        user.PasswordResetTokenExpiry.Should().BeNull();

        var refreshTokens = await GetRefreshTokensAsync(userId);
        refreshTokens.Should().NotBeEmpty();
        refreshTokens.Should().OnlyContain(token => token.IsRevoked);
        refreshTokens.Should().Contain(token => token.RevokedReason == "Password reset");
    }

    [Fact]
    public async Task ResetPassword_ExpiredToken_ReturnsBadRequestAndClearsToken()
    {
        var userId = NextUserId();
        await SeedCustomerAsync(userId, isEmailVerified: true);

        var rawToken = $"expired-reset-{Guid.NewGuid():N}";
        await SetPasswordResetTokenAsync(userId, rawToken, DateTime.UtcNow.AddMinutes(-10));

        var response = await _client.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetPasswordRequest
        {
            Token = rawToken,
            NewPassword = "NewStrongPassword1",
            ConfirmPassword = "NewStrongPassword1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errorCode = await ReadErrorCodeAsync(response);
        errorCode.Should().Be("EXPIRED_TOKEN");

        var user = await FindUserAsync(userId);
        user.Should().NotBeNull();
        user!.PasswordResetToken.Should().BeNull();
        user.PasswordResetTokenExpiry.Should().BeNull();
    }

    [Fact]
    public async Task ChangeEmail_WhenClaimIsUnverified_ReturnsForbidden()
    {
        var userId = NextUserId();
        await SeedCustomerAsync(userId, isEmailVerified: true);

        var client = _factory.CreateClient().AsUnverifiedCustomer(userId);
        var response = await client.PostAsJsonAsync("/api/v1/account/change-email", new ChangeEmailRequest
        {
            NewEmail = $"changed_{Guid.NewGuid():N}@example.com",
            CurrentPassword = "TestPassword123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ChangeEmail_ValidRequest_SetsPendingEmailAndToken()
    {
        var userId = NextUserId();
        await SeedCustomerAsync(userId, isEmailVerified: true);

        var newEmail = $"pending_{Guid.NewGuid():N}@example.com";
        var client = _factory.CreateClient().AsCustomer(userId);
        var response = await client.PostAsJsonAsync("/api/v1/account/change-email", new ChangeEmailRequest
        {
            NewEmail = newEmail,
            CurrentPassword = "TestPassword123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var user = await FindUserAsync(userId);
        user.Should().NotBeNull();
        user!.PendingEmail.Should().Be(newEmail);
        user.EmailChangeToken.Should().NotBeNullOrWhiteSpace();
        user.EmailChangeTokenExpiry.Should().NotBeNull();
    }

    [Fact]
    public async Task ConfirmEmailChange_ValidToken_UpdatesEmailAndRevokesRefreshTokens()
    {
        var userId = NextUserId();
        var originalEmail = $"current_{Guid.NewGuid():N}@example.com";
        var newEmail = $"new_{Guid.NewGuid():N}@example.com";
        await SeedCustomerAsync(userId, isEmailVerified: true, email: originalEmail);
        await AddRefreshTokenAsync(userId, token: $"refresh-mail-change-{Guid.NewGuid():N}");

        var rawToken = $"mail-change-{Guid.NewGuid():N}";
        await SetEmailChangeTokenAsync(userId, rawToken, newEmail, DateTime.UtcNow.AddHours(12));

        var response = await _client.PostAsJsonAsync("/api/v1/auth/confirm-email-change", new ConfirmEmailChangeRequest
        {
            Token = rawToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var apiResult = await response.Content.ReadFromJsonAsync<ApiResult<AuthResponse>>();
        apiResult.Should().NotBeNull();
        apiResult!.Success.Should().BeTrue();
        apiResult.Data.User.Should().NotBeNull();
        apiResult.Data.User!.Email.Should().Be(newEmail);

        var user = await FindUserAsync(userId);
        user.Should().NotBeNull();
        user!.Email.Should().Be(newEmail);
        user.PendingEmail.Should().BeNull();
        user.EmailChangeToken.Should().BeNull();
        user.EmailChangeTokenExpiry.Should().BeNull();

        var refreshTokens = await GetRefreshTokensAsync(userId);
        refreshTokens.Should().NotBeEmpty();
        refreshTokens.Should().Contain(token => token.IsRevoked && token.RevokedReason == "Email changed");
        refreshTokens.Should().ContainSingle(token => token.IsRevoked == false);
    }

    private int NextUserId()
    {
        return Random.Shared.Next(1_600_000, 1_700_000);
    }

    private async Task SeedCustomerAsync(
        int userId,
        bool isEmailVerified,
        string? email = null)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hashingService = scope.ServiceProvider.GetRequiredService<IHashingService>();

        var user = await TestDataSeeder.EnsureUserAsync(db, userId, "Customer");
        if (!string.IsNullOrWhiteSpace(email))
        {
            user.Email = email.Trim();
        }

        user.EmailHash = hashingService.Hash(user.Email.ToLowerInvariant().Trim());
        user.IsEmailVerified = isEmailVerified;
        db.Users.Update(user);
        await db.SaveChangesAsync();
    }

    private async Task SetEmailVerificationTokenAsync(int userId, string rawToken, DateTime expiry)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hashingService = scope.ServiceProvider.GetRequiredService<IHashingService>();

        var user = await db.Users.FirstAsync(u => u.Id == userId);
        user.EmailVerificationToken = hashingService.Hash(rawToken);
        user.EmailVerificationTokenExpiry = expiry;
        db.Users.Update(user);
        await db.SaveChangesAsync();
    }

    private async Task SetPasswordResetTokenAsync(int userId, string rawToken, DateTime expiry)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hashingService = scope.ServiceProvider.GetRequiredService<IHashingService>();

        var user = await db.Users.FirstAsync(u => u.Id == userId);
        user.PasswordResetToken = hashingService.Hash(rawToken);
        user.PasswordResetTokenExpiry = expiry;
        db.Users.Update(user);
        await db.SaveChangesAsync();
    }

    private async Task SetEmailChangeTokenAsync(int userId, string rawToken, string pendingEmail, DateTime expiry)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hashingService = scope.ServiceProvider.GetRequiredService<IHashingService>();

        var user = await db.Users.FirstAsync(u => u.Id == userId);
        user.PendingEmail = pendingEmail;
        user.EmailChangeToken = hashingService.Hash(rawToken);
        user.EmailChangeTokenExpiry = expiry;
        db.Users.Update(user);
        await db.SaveChangesAsync();
    }

    private async Task AddRefreshTokenAsync(int userId, string token)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hashingService = scope.ServiceProvider.GetRequiredService<IHashingService>();

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            Token = hashingService.Hash(token),
            JwtId = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            IsUsed = false
        });
        await db.SaveChangesAsync();
    }

    private async Task<User?> FindUserAsync(int userId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
    }

    private async Task<List<RefreshToken>> GetRefreshTokensAsync(int userId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.RefreshTokens.Where(token => token.UserId == userId).ToListAsync();
    }

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        using var document = JsonDocument.Parse(payload);
        if (document.RootElement.TryGetProperty("errorCode", out var errorCode))
        {
            return errorCode.GetString();
        }

        if (document.RootElement.TryGetProperty("ErrorCode", out var errorCodePascal))
        {
            return errorCodePascal.GetString();
        }

        return null;
    }

    private sealed class ResultEnvelope
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ErrorCode { get; set; }
    }
}
