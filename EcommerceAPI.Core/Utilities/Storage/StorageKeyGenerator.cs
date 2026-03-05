namespace EcommerceAPI.Core.Utilities.Storage;

public static class StorageKeyGenerator
{
    public static string ProductImage(int sellerId, int productId, string extension = "webp")
    {
        var normalizedExtension = NormalizeExtension(extension);
        return $"products/seller-{sellerId}/product-{productId}/{Guid.NewGuid():N}.{normalizedExtension}";
    }

    public static string CategoryImage(int categoryId, string extension = "webp")
    {
        var normalizedExtension = NormalizeExtension(extension);
        return $"categories/category-{categoryId}/{Guid.NewGuid():N}.{normalizedExtension}";
    }

    public static string SellerLogo(int sellerId, string extension = "webp")
    {
        var normalizedExtension = NormalizeExtension(extension);
        return $"sellers/seller-{sellerId}/logo.{normalizedExtension}";
    }

    public static string SellerBanner(int sellerId, string extension = "webp")
    {
        var normalizedExtension = NormalizeExtension(extension);
        return $"sellers/seller-{sellerId}/banner.{normalizedExtension}";
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return "webp";
        }

        return extension.Trim().TrimStart('.').ToLowerInvariant();
    }
}
