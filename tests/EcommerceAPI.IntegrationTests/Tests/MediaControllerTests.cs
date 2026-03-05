using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EcommerceAPI.API;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.IntegrationTests.Utilities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EcommerceAPI.IntegrationTests.Tests;

[Collection("Integration")]
public class MediaControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private static int _idSeed = (int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 500_000_000) + 1_500_000_000;
    private static readonly byte[] ValidJpegHeader =
    [
        0xFF, 0xD8, 0xFF, 0xE0,
        0x00, 0x10, 0x4A, 0x46,
        0x49, 0x46, 0x00, 0x01
    ];

    private readonly CustomWebApplicationFactory _factory;

    public MediaControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PresignUpload_ProductAsOwnerSeller_ReturnsOkWithExpectedObjectKeyPrefix()
    {
        var storage = new FakeObjectStorageService();
        using var factory = CreateFactoryWithStorage(storage);

        var seeded = await SeedSellerProductAsync(factory.Services);
        var resolvedStorage = factory.Services.GetRequiredService<IObjectStorageService>();
        resolvedStorage.Should().BeSameAs(storage);

        var client = factory.CreateClient().AsSeller(seeded.UserId);

        var response = await client.PostAsJsonAsync("/api/v1/media/presign", new PresignMediaUploadRequest
        {
            Context = "product",
            ReferenceId = seeded.ProductId,
            ContentType = "image/webp",
            FileSizeBytes = 2_048
        });

        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"response body: {responseBody}");

        var result = await response.Content.ReadFromJsonAsync<ApiResult<PresignedMediaUploadDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.ObjectKey.Should().StartWith($"products/seller-{seeded.SellerProfileId}/product-{seeded.ProductId}/");
        result.Data.ObjectKey.Should().EndWith(".webp");
        result.Data.PublicUrl.Should().Be(storage.GetPublicUrl(result.Data.ObjectKey));
        storage.Exists(result.Data.ObjectKey).Should().BeTrue();
    }

    [Fact]
    public async Task PresignUpload_ProductAsDifferentSeller_ReturnsForbidden()
    {
        var storage = new FakeObjectStorageService();
        using var factory = CreateFactoryWithStorage(storage);

        var seeded = await SeedSellerProductAsync(factory.Services);

        var attackerUserId = NextId();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await TestDataSeeder.EnsureSellerProfileAsync(db, attackerUserId, $"attacker-{Guid.NewGuid():N}");
        }

        var client = factory.CreateClient().AsSeller(attackerUserId);

        var response = await client.PostAsJsonAsync("/api/v1/media/presign", new PresignMediaUploadRequest
        {
            Context = "product",
            ReferenceId = seeded.ProductId,
            ContentType = "image/webp",
            FileSizeBytes = 2_048
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ReadErrorMessageAsync(response)).Should().Be("Yetkiniz yok");
    }

    [Fact]
    public async Task PresignUpload_CategoryAsAdmin_ReturnsOkWithExpectedObjectKeyPrefix()
    {
        var storage = new FakeObjectStorageService();
        using var factory = CreateFactoryWithStorage(storage);

        var categoryId = await SeedCategoryAsync(factory.Services);
        var client = factory.CreateClient().AsAdmin(NextId());

        var response = await client.PostAsJsonAsync("/api/v1/media/presign", new PresignMediaUploadRequest
        {
            Context = "category",
            ReferenceId = categoryId,
            ContentType = "image/png",
            FileSizeBytes = 4_096
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResult<PresignedMediaUploadDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.ObjectKey.Should().StartWith($"categories/category-{categoryId}/");
        result.Data.ObjectKey.Should().EndWith(".png");
    }

    [Fact]
    public async Task PresignUpload_CategoryAsSeller_ReturnsForbidden()
    {
        var storage = new FakeObjectStorageService();
        using var factory = CreateFactoryWithStorage(storage);

        var categoryId = await SeedCategoryAsync(factory.Services);
        var seller = await SeedSellerProfileAsync(factory.Services);
        var client = factory.CreateClient().AsSeller(seller.UserId);

        var response = await client.PostAsJsonAsync("/api/v1/media/presign", new PresignMediaUploadRequest
        {
            Context = "category",
            ReferenceId = categoryId,
            ContentType = "image/png",
            FileSizeBytes = 4_096
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ReadErrorMessageAsync(response)).Should().Be("Yetkiniz yok");
    }

    [Fact]
    public async Task ConfirmUpload_ProductValidObjectKey_PersistsImage()
    {
        var storage = new FakeObjectStorageService();
        using var factory = CreateFactoryWithStorage(storage);

        var seeded = await SeedSellerProductAsync(factory.Services);
        var objectKey = $"products/seller-{seeded.SellerProfileId}/product-{seeded.ProductId}/{Guid.NewGuid():N}.webp";
        storage.SeedObject(objectKey, ValidJpegHeader);

        var client = factory.CreateClient().AsSeller(seeded.UserId);

        var response = await client.PostAsJsonAsync("/api/v1/media/confirm", new ConfirmMediaUploadRequest
        {
            Context = "product",
            ReferenceId = seeded.ProductId,
            ObjectKey = objectKey,
            IsPrimary = true,
            SortOrder = 0
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiResult = await response.Content.ReadFromJsonAsync<ApiResult<ConfirmMediaUploadDto>>();
        apiResult.Should().NotBeNull();
        apiResult!.Success.Should().BeTrue();
        apiResult.Data.ImageId.Should().NotBeNull();
        apiResult.Data.ObjectKey.Should().Be(objectKey);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var product = await db.Products
            .Include(x => x.Images)
            .SingleAsync(x => x.Id == seeded.ProductId);

        product.Images.Should().ContainSingle(image =>
            image.ObjectKey == objectKey &&
            image.IsPrimary &&
            image.SortOrder == 0 &&
            image.ImageUrl == storage.GetPublicUrl(objectKey));
    }

    [Fact]
    public async Task ConfirmUpload_ProductWithDifferentSellerPrefix_ReturnsBadRequest()
    {
        var storage = new FakeObjectStorageService();
        using var factory = CreateFactoryWithStorage(storage);

        var seeded = await SeedSellerProductAsync(factory.Services);
        var foreignSellerId = seeded.SellerProfileId + 999;
        var objectKey = $"products/seller-{foreignSellerId}/product-{seeded.ProductId}/{Guid.NewGuid():N}.webp";
        storage.SeedObject(objectKey, ValidJpegHeader);

        var client = factory.CreateClient().AsSeller(seeded.UserId);

        var response = await client.PostAsJsonAsync("/api/v1/media/confirm", new ConfirmMediaUploadRequest
        {
            Context = "product",
            ReferenceId = seeded.ProductId,
            ObjectKey = objectKey,
            IsPrimary = true,
            SortOrder = 0
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadErrorMessageAsync(response)).Should().Be("Geçersiz object key prefix");
    }

    [Fact]
    public async Task ConfirmUpload_CategoryAsAdmin_UpdatesCategoryImage()
    {
        var storage = new FakeObjectStorageService();
        using var factory = CreateFactoryWithStorage(storage);

        var categoryId = await SeedCategoryAsync(factory.Services);
        var objectKey = $"categories/category-{categoryId}/{Guid.NewGuid():N}.webp";
        storage.SeedObject(objectKey, ValidJpegHeader);
        var client = factory.CreateClient().AsAdmin(NextId());

        var response = await client.PostAsJsonAsync("/api/v1/media/confirm", new ConfirmMediaUploadRequest
        {
            Context = "category",
            ReferenceId = categoryId,
            ObjectKey = objectKey
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var category = await db.Categories.SingleAsync(x => x.Id == categoryId);

        category.ImageObjectKey.Should().Be(objectKey);
        category.ImageUrl.Should().Be(storage.GetPublicUrl(objectKey));
    }

    [Fact]
    public async Task ConfirmUpload_CategoryAsSeller_ReturnsForbidden()
    {
        var storage = new FakeObjectStorageService();
        using var factory = CreateFactoryWithStorage(storage);

        var categoryId = await SeedCategoryAsync(factory.Services);
        var objectKey = $"categories/category-{categoryId}/{Guid.NewGuid():N}.webp";
        storage.SeedObject(objectKey, ValidJpegHeader);
        var seller = await SeedSellerProfileAsync(factory.Services);
        var client = factory.CreateClient().AsSeller(seller.UserId);

        var response = await client.PostAsJsonAsync("/api/v1/media/confirm", new ConfirmMediaUploadRequest
        {
            Context = "category",
            ReferenceId = categoryId,
            ObjectKey = objectKey
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ReadErrorMessageAsync(response)).Should().Be("Yetkiniz yok");
    }

    [Fact]
    public async Task ConfirmUpload_SellerLogoAsOwnerSeller_UpdatesLogo()
    {
        var storage = new FakeObjectStorageService();
        using var factory = CreateFactoryWithStorage(storage);

        var seller = await SeedSellerProfileAsync(factory.Services);
        var objectKey = $"sellers/seller-{seller.SellerProfileId}/logo.webp";
        storage.SeedObject(objectKey, ValidJpegHeader);
        var client = factory.CreateClient().AsSeller(seller.UserId);

        var response = await client.PostAsJsonAsync("/api/v1/media/confirm", new ConfirmMediaUploadRequest
        {
            Context = "seller-logo",
            ReferenceId = seller.SellerProfileId,
            ObjectKey = objectKey
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var profile = await db.SellerProfiles.SingleAsync(x => x.Id == seller.SellerProfileId);

        profile.LogoObjectKey.Should().Be(objectKey);
        profile.LogoUrl.Should().Be(storage.GetPublicUrl(objectKey));
    }

    [Fact]
    public async Task ConfirmUpload_SellerBannerAsDifferentSeller_ReturnsForbidden()
    {
        var storage = new FakeObjectStorageService();
        using var factory = CreateFactoryWithStorage(storage);

        var ownerSeller = await SeedSellerProfileAsync(factory.Services);
        var attackerSeller = await SeedSellerProfileAsync(factory.Services);
        var objectKey = $"sellers/seller-{ownerSeller.SellerProfileId}/banner.webp";
        storage.SeedObject(objectKey, ValidJpegHeader);

        var client = factory.CreateClient().AsSeller(attackerSeller.UserId);
        var response = await client.PostAsJsonAsync("/api/v1/media/confirm", new ConfirmMediaUploadRequest
        {
            Context = "seller-banner",
            ReferenceId = ownerSeller.SellerProfileId,
            ObjectKey = objectKey
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ReadErrorMessageAsync(response)).Should().Be("Yetkiniz yok");
    }

    [Fact]
    public async Task DeleteImage_WhenPrimaryDeleted_AssignsFallbackPrimaryAndDeletesFromStorage()
    {
        var storage = new FakeObjectStorageService();
        using var factory = CreateFactoryWithStorage(storage);

        var seeded = await SeedSellerProductWithImagesAsync(factory.Services, storage);
        var client = factory.CreateClient().AsSeller(seeded.UserId);

        var response = await client.DeleteAsync($"/api/v1/media/{seeded.PrimaryImageId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        storage.DeletedObjectKeys.Should().Contain(seeded.PrimaryObjectKey);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var product = await db.Products
            .Include(x => x.Images)
            .SingleAsync(x => x.Id == seeded.ProductId);

        product.Images.Should().HaveCount(1);
        product.Images.Single().Id.Should().Be(seeded.SecondaryImageId);
        product.Images.Single().IsPrimary.Should().BeTrue();
        product.Images.Single().SortOrder.Should().Be(0);
    }

    [Fact]
    public async Task ReorderProductImages_WhenOwnedBySeller_UpdatesSortAndPrimary()
    {
        var storage = new FakeObjectStorageService();
        using var factory = CreateFactoryWithStorage(storage);

        var seeded = await SeedSellerProductWithImagesAsync(factory.Services, storage);
        var client = factory.CreateClient().AsSeller(seeded.UserId);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/media/products/{seeded.ProductId}/images/reorder",
            new ReorderProductImagesRequest
            {
                ImageOrders =
                [
                    new ReorderProductImageItemRequest
                    {
                        ImageId = seeded.SecondaryImageId,
                        DisplayOrder = 0,
                        IsPrimary = true
                    },
                    new ReorderProductImageItemRequest
                    {
                        ImageId = seeded.PrimaryImageId,
                        DisplayOrder = 1,
                        IsPrimary = false
                    }
                ]
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var product = await db.Products
            .Include(x => x.Images)
            .SingleAsync(x => x.Id == seeded.ProductId);

        var first = product.Images.Single(x => x.Id == seeded.SecondaryImageId);
        var second = product.Images.Single(x => x.Id == seeded.PrimaryImageId);

        first.SortOrder.Should().Be(0);
        first.IsPrimary.Should().BeTrue();
        second.SortOrder.Should().Be(1);
        second.IsPrimary.Should().BeFalse();
    }

    [Fact]
    public async Task ReorderProductImages_WhenDifferentSeller_ReturnsForbidden()
    {
        var storage = new FakeObjectStorageService();
        using var factory = CreateFactoryWithStorage(storage);

        var seeded = await SeedSellerProductWithImagesAsync(factory.Services, storage);
        var attacker = await SeedSellerProfileAsync(factory.Services);
        var client = factory.CreateClient().AsSeller(attacker.UserId);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/media/products/{seeded.ProductId}/images/reorder",
            new ReorderProductImagesRequest
            {
                ImageOrders =
                [
                    new ReorderProductImageItemRequest
                    {
                        ImageId = seeded.SecondaryImageId,
                        DisplayOrder = 0,
                        IsPrimary = true
                    },
                    new ReorderProductImageItemRequest
                    {
                        ImageId = seeded.PrimaryImageId,
                        DisplayOrder = 1,
                        IsPrimary = false
                    }
                ]
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ReadErrorMessageAsync(response)).Should().Be("Yetkiniz yok");
    }

    private WebApplicationFactory<Program> CreateFactoryWithStorage(FakeObjectStorageService storage)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IObjectStorageService>();
                services.AddSingleton<IObjectStorageService>(storage);
            });
        });
    }

    private static int NextId() => Interlocked.Increment(ref _idSeed);

    private static async Task<string?> ReadErrorMessageAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(stream);

        if (json.RootElement.TryGetProperty("message", out var messageElement))
        {
            return messageElement.GetString();
        }

        return null;
    }

    private static async Task<(int UserId, int SellerProfileId, int ProductId)> SeedSellerProductAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var userId = NextId();
        var sellerProfile = await TestDataSeeder.EnsureSellerProfileAsync(db, userId, $"seller-{userId}");

        var categoryId = NextId();
        await TestDataSeeder.EnsureCategoryAsync(db, categoryId, $"category-{categoryId}");

        var productId = NextId();
        await TestDataSeeder.EnsureProductWithStockAsync(db, productId, categoryId, stockQuantity: 50, sellerId: sellerProfile.Id);

        return (userId, sellerProfile.Id, productId);
    }

    private static async Task<int> SeedCategoryAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var categoryId = NextId();
        var category = await TestDataSeeder.EnsureCategoryAsync(db, categoryId, $"category-{categoryId}");
        return category.Id;
    }

    private static async Task<(int UserId, int SellerProfileId)> SeedSellerProfileAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var userId = NextId();
        var sellerProfile = await TestDataSeeder.EnsureSellerProfileAsync(db, userId, $"seller-{userId}");
        return (userId, sellerProfile.Id);
    }

    private static async Task<(int UserId, int ProductId, int PrimaryImageId, int SecondaryImageId, string PrimaryObjectKey, string SecondaryObjectKey)> SeedSellerProductWithImagesAsync(
        IServiceProvider services,
        FakeObjectStorageService storage)
    {
        var seeded = await SeedSellerProductAsync(services);

        var primaryObjectKey = $"products/seller-{seeded.SellerProfileId}/product-{seeded.ProductId}/{Guid.NewGuid():N}.webp";
        var secondaryObjectKey = $"products/seller-{seeded.SellerProfileId}/product-{seeded.ProductId}/{Guid.NewGuid():N}.webp";

        storage.SeedObject(primaryObjectKey, ValidJpegHeader);
        storage.SeedObject(secondaryObjectKey, ValidJpegHeader);

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var product = await db.Products
            .Include(x => x.Images)
            .SingleAsync(x => x.Id == seeded.ProductId);

        product.Images.Add(new ProductImage
        {
            ImageUrl = storage.GetPublicUrl(primaryObjectKey),
            ObjectKey = primaryObjectKey,
            SortOrder = 0,
            IsPrimary = true
        });

        product.Images.Add(new ProductImage
        {
            ImageUrl = storage.GetPublicUrl(secondaryObjectKey),
            ObjectKey = secondaryObjectKey,
            SortOrder = 3,
            IsPrimary = false
        });

        await db.SaveChangesAsync();

        var primaryImageId = product.Images.Single(x => x.ObjectKey == primaryObjectKey).Id;
        var secondaryImageId = product.Images.Single(x => x.ObjectKey == secondaryObjectKey).Id;

        return (seeded.UserId, seeded.ProductId, primaryImageId, secondaryImageId, primaryObjectKey, secondaryObjectKey);
    }

    private sealed class FakeObjectStorageService : IObjectStorageService
    {
        private readonly ConcurrentDictionary<string, byte[]> _objects = new(StringComparer.OrdinalIgnoreCase);
        private readonly string _publicBaseUrl = "https://img.test.local";

        public ConcurrentBag<string> DeletedObjectKeys { get; } = [];

        public void SeedObject(string objectKey, byte[]? headerBytes = null)
        {
            var normalized = NormalizeObjectKey(objectKey);
            _objects[normalized] = headerBytes ?? ValidJpegHeader;
        }

        public bool Exists(string objectKey)
        {
            return _objects.ContainsKey(NormalizeObjectKey(objectKey));
        }

        public Task<PresignedUploadUrlResult> GeneratePresignedUploadUrlAsync(
            string objectKey,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            var normalized = NormalizeObjectKey(objectKey);
            _objects.TryAdd(normalized, ValidJpegHeader);

            var uploadUrl = $"https://upload.test.local/{Uri.EscapeDataString(normalized)}";
            var publicUrl = GetPublicUrl(normalized);

            return Task.FromResult(new PresignedUploadUrlResult(uploadUrl, publicUrl, normalized));
        }

        public Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Exists(objectKey));
        }

        public Task<byte[]?> GetObjectHeaderBytesAsync(
            string objectKey,
            int maxBytes = 64,
            CancellationToken cancellationToken = default)
        {
            var normalized = NormalizeObjectKey(objectKey);
            if (!_objects.TryGetValue(normalized, out var bytes))
            {
                return Task.FromResult<byte[]?>(null);
            }

            var length = Math.Min(maxBytes, bytes.Length);
            return Task.FromResult<byte[]?>(bytes.Take(length).ToArray());
        }

        public Task<IReadOnlyList<ObjectStorageObjectInfo>> ListObjectsAsync(
            string? prefix = null,
            int? maxKeys = null,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<string> keys = _objects.Keys;
            if (!string.IsNullOrWhiteSpace(prefix))
            {
                var normalizedPrefix = NormalizeObjectKey(prefix);
                keys = keys.Where(key => key.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase));
            }

            var limited = keys
                .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                .Take(maxKeys ?? int.MaxValue)
                .Select(key => new ObjectStorageObjectInfo(key, DateTime.UtcNow.AddMinutes(-5)))
                .ToList();

            return Task.FromResult<IReadOnlyList<ObjectStorageObjectInfo>>(limited);
        }

        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
        {
            var normalized = NormalizeObjectKey(objectKey);
            _objects.TryRemove(normalized, out _);
            DeletedObjectKeys.Add(normalized);
            return Task.CompletedTask;
        }

        public string GetPublicUrl(string objectKey)
        {
            var normalized = NormalizeObjectKey(objectKey);
            return $"{_publicBaseUrl}/{normalized}";
        }

        private static string NormalizeObjectKey(string objectKey)
        {
            return objectKey.Trim().TrimStart('/');
        }
    }
}
