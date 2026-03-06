using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.Entities.IntegrationEvents;
using MassTransit;

namespace EcommerceAPI.Business.Concrete;

public class AdminSellerManager : IAdminSellerService
{
    private const decimal DefaultCommissionRate = 10m;

    private static readonly OrderStatus[] RevenueStatuses =
    {
        OrderStatus.Paid,
        OrderStatus.Processing,
        OrderStatus.Shipped,
        OrderStatus.Delivered
    };

    private readonly ISellerProfileDal _sellerProfileDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;

    public AdminSellerManager(
        ISellerProfileDal sellerProfileDal,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint)
    {
        _sellerProfileDal = sellerProfileDal;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<IDataResult<List<AdminSellerListItemDto>>> GetSellersAsync(string? status = null)
    {
        var sellers = await _sellerProfileDal.GetAdminListWithDetailsAsync();
        var items = sellers
            .Select(MapToListItem)
            .Where(item => MatchesStatus(item.Status, status))
            .OrderByDescending(item => item.Status == "Pending")
            .ThenBy(item => item.BrandName)
            .ToList();

        return new SuccessDataResult<List<AdminSellerListItemDto>>(items);
    }

    public async Task<IDataResult<AdminSellerDetailDto>> GetSellerDetailAsync(int sellerId)
    {
        var seller = await _sellerProfileDal.GetAdminDetailWithDetailsAsync(sellerId);
        if (seller == null)
        {
            return new ErrorDataResult<AdminSellerDetailDto>("Satıcı profili bulunamadı");
        }

        return new SuccessDataResult<AdminSellerDetailDto>(MapToDetail(seller));
    }

    public async Task<IDataResult<AdminSellerDetailDto>> UpdateSellerStatusAsync(int sellerId, UpdateAdminSellerStatusRequest request)
    {
        var seller = await _sellerProfileDal.GetAdminDetailWithDetailsAsync(sellerId);
        if (seller == null)
        {
            return new ErrorDataResult<AdminSellerDetailDto>("Satıcı profili bulunamadı");
        }

        var normalizedStatus = NormalizeStatus(request.Status);
        if (normalizedStatus == null)
        {
            return new ErrorDataResult<AdminSellerDetailDto>("Geçersiz satıcı durumu.");
        }

        ApplyStatus(seller, normalizedStatus);
        if (!string.IsNullOrWhiteSpace(request.ReviewNote))
        {
            seller.ApplicationReviewNote = request.ReviewNote.Trim();
            seller.ApplicationReviewedAt = DateTime.UtcNow;
        }

        seller.UpdatedAt = DateTime.UtcNow;
        _sellerProfileDal.Update(seller);
        await _unitOfWork.SaveChangesAsync();

        return new SuccessDataResult<AdminSellerDetailDto>(MapToDetail(seller), "Satıcı durumu güncellendi");
    }

    public async Task<IDataResult<AdminSellerDetailDto>> UpdateSellerCommissionAsync(int sellerId, UpdateAdminSellerCommissionRequest request)
    {
        var seller = await _sellerProfileDal.GetAdminDetailWithDetailsAsync(sellerId);
        if (seller == null)
        {
            return new ErrorDataResult<AdminSellerDetailDto>("Satıcı profili bulunamadı");
        }

        if (request.Rate.HasValue && (request.Rate < 0 || request.Rate > 100))
        {
            return new ErrorDataResult<AdminSellerDetailDto>("Komisyon oranı 0 ile 100 arasında olmalıdır.");
        }

        seller.CommissionRateOverride = request.Rate;
        seller.UpdatedAt = DateTime.UtcNow;
        _sellerProfileDal.Update(seller);
        await _unitOfWork.SaveChangesAsync();

        return new SuccessDataResult<AdminSellerDetailDto>(MapToDetail(seller), "Satıcı komisyon oranı güncellendi");
    }

    public async Task<IDataResult<AdminSellerDetailDto>> ApproveApplicationAsync(int sellerId, ReviewSellerApplicationRequest request)
    {
        var seller = await _sellerProfileDal.GetAdminDetailWithDetailsAsync(sellerId);
        if (seller == null)
        {
            return new ErrorDataResult<AdminSellerDetailDto>("Satıcı profili bulunamadı");
        }

        seller.IsVerified = true;
        seller.User.AccountStatus = UserAccountStatus.Active;
        seller.ApplicationReviewNote = request.ReviewNote?.Trim();
        seller.ApplicationReviewedAt = DateTime.UtcNow;
        seller.UpdatedAt = DateTime.UtcNow;

        _sellerProfileDal.Update(seller);
        await _unitOfWork.SaveChangesAsync();
        await PublishSellerApplicationReviewedAsync(seller, "Approved");

        return new SuccessDataResult<AdminSellerDetailDto>(MapToDetail(seller), "Satıcı başvurusu onaylandı");
    }

    public async Task<IDataResult<AdminSellerDetailDto>> RejectApplicationAsync(int sellerId, ReviewSellerApplicationRequest request)
    {
        var seller = await _sellerProfileDal.GetAdminDetailWithDetailsAsync(sellerId);
        if (seller == null)
        {
            return new ErrorDataResult<AdminSellerDetailDto>("Satıcı profili bulunamadı");
        }

        seller.IsVerified = false;
        seller.User.AccountStatus = UserAccountStatus.Suspended;
        seller.ApplicationReviewNote = string.IsNullOrWhiteSpace(request.ReviewNote)
            ? "Başvuru admin tarafından reddedildi."
            : request.ReviewNote.Trim();
        seller.ApplicationReviewedAt = DateTime.UtcNow;
        seller.UpdatedAt = DateTime.UtcNow;

        _sellerProfileDal.Update(seller);
        await _unitOfWork.SaveChangesAsync();
        await PublishSellerApplicationReviewedAsync(seller, "Rejected");

        return new SuccessDataResult<AdminSellerDetailDto>(MapToDetail(seller), "Satıcı başvurusu reddedildi");
    }

    private async Task PublishSellerApplicationReviewedAsync(SellerProfile seller, string decision)
    {
        await _publishEndpoint.Publish(new SellerApplicationReviewedEvent
        {
            SellerProfileId = seller.Id,
            UserId = seller.UserId,
            UserEmail = seller.User.Email,
            CustomerName = $"{seller.User.FirstName} {seller.User.LastName}".Trim(),
            BrandName = seller.BrandName,
            Decision = decision,
            ReviewNote = seller.ApplicationReviewNote,
            ReviewedAt = seller.ApplicationReviewedAt ?? DateTime.UtcNow
        });
    }

    private static bool MatchesStatus(string currentStatus, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return true;
        }

        return string.Equals(currentStatus, NormalizeStatus(filter), StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeStatus(string? status)
    {
        return status?.Trim().ToLowerInvariant() switch
        {
            "active" => "Active",
            "pending" => "Pending",
            "suspended" => "Suspended",
            "closed" => "Closed",
            _ => null
        };
    }

    private static void ApplyStatus(SellerProfile seller, string status)
    {
        switch (status)
        {
            case "Active":
                seller.User.AccountStatus = UserAccountStatus.Active;
                seller.IsVerified = true;
                seller.ApplicationReviewedAt ??= DateTime.UtcNow;
                break;
            case "Pending":
                seller.User.AccountStatus = UserAccountStatus.Active;
                seller.IsVerified = false;
                break;
            case "Suspended":
                seller.User.AccountStatus = UserAccountStatus.Suspended;
                break;
            case "Closed":
                seller.User.AccountStatus = UserAccountStatus.Banned;
                break;
        }
    }

    private static AdminSellerListItemDto MapToListItem(SellerProfile seller)
    {
        var metrics = BuildMetrics(seller);

        return new AdminSellerListItemDto
        {
            Id = seller.Id,
            UserId = seller.UserId,
            BrandName = seller.BrandName,
            IsPlatformAccount = seller.IsPlatformAccount,
            SellerFirstName = seller.User.FirstName,
            SellerLastName = seller.User.LastName,
            OwnerEmail = seller.User.Email,
            Status = GetStatus(seller),
            ProductCount = metrics.ProductCount,
            ActiveProductCount = metrics.ActiveProductCount,
            TotalStock = metrics.TotalStock,
            TotalSales = metrics.TotalSales,
            AverageRating = metrics.AverageRating,
            CommissionRate = seller.CommissionRateOverride ?? DefaultCommissionRate,
            HasCommissionOverride = seller.CommissionRateOverride.HasValue,
            CreatedAt = seller.CreatedAt,
            IsVerified = seller.IsVerified
        };
    }

    private static AdminSellerDetailDto MapToDetail(SellerProfile seller)
    {
        var metrics = BuildMetrics(seller);

        return new AdminSellerDetailDto
        {
            Id = seller.Id,
            UserId = seller.UserId,
            BrandName = seller.BrandName,
            IsPlatformAccount = seller.IsPlatformAccount,
            BrandDescription = seller.BrandDescription,
            LogoUrl = seller.LogoUrl,
            BannerImageUrl = seller.BannerImageUrl,
            ContactEmail = seller.ContactEmail,
            ContactPhone = seller.ContactPhone,
            WebsiteUrl = seller.WebsiteUrl,
            InstagramUrl = seller.InstagramUrl,
            FacebookUrl = seller.FacebookUrl,
            XUrl = seller.XUrl,
            IsVerified = seller.IsVerified,
            Status = GetStatus(seller),
            SellerFirstName = seller.User.FirstName,
            SellerLastName = seller.User.LastName,
            OwnerEmail = seller.User.Email,
            ProductCount = metrics.ProductCount,
            ActiveProductCount = metrics.ActiveProductCount,
            TotalStock = metrics.TotalStock,
            TotalSales = metrics.TotalSales,
            AverageRating = metrics.AverageRating,
            CommissionRate = seller.CommissionRateOverride ?? DefaultCommissionRate,
            CommissionRateOverride = seller.CommissionRateOverride,
            Currency = metrics.Currency,
            ApplicationReviewNote = seller.ApplicationReviewNote,
            ApplicationReviewedAt = seller.ApplicationReviewedAt,
            CreatedAt = seller.CreatedAt,
            Products = seller.Products
                .OrderByDescending(product => product.IsActive)
                .ThenBy(product => product.Name)
                .Select(product => new AdminSellerProductSummaryDto
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    CategoryName = product.Category?.Name ?? "Kategori yok",
                    Price = product.Price,
                    Currency = product.Currency,
                    StockQuantity = product.Inventory?.QuantityAvailable ?? 0,
                    IsActive = product.IsActive,
                    AverageRating = product.Reviews.Any(review => review.ModerationStatus == ProductReviewModerationStatus.Approved)
                        ? Math.Round(product.Reviews.Where(review => review.ModerationStatus == ProductReviewModerationStatus.Approved).Average(review => review.Rating), 1)
                        : 0
                })
                .ToList()
        };
    }

    private static (int ProductCount, int ActiveProductCount, int TotalStock, decimal TotalSales, double AverageRating, string Currency) BuildMetrics(SellerProfile seller)
    {
        var products = seller.Products.ToList();
        var approvedReviews = products
            .SelectMany(product => product.Reviews)
            .Where(review => review.ModerationStatus == ProductReviewModerationStatus.Approved)
            .ToList();

        var totalSales = products
            .SelectMany(product => product.OrderItems)
            .Where(item => item.Order != null && RevenueStatuses.Contains(item.Order.Status))
            .Sum(item => item.PriceSnapshot * item.Quantity);

        return (
            products.Count,
            products.Count(product => product.IsActive),
            products.Sum(product => product.Inventory?.QuantityAvailable ?? 0),
            Math.Round(totalSales, 2),
            approvedReviews.Count > 0 ? Math.Round(approvedReviews.Average(review => review.Rating), 1) : 0,
            products.FirstOrDefault()?.Currency ?? "TRY"
        );
    }

    private static string GetStatus(SellerProfile seller)
    {
        return seller.User.AccountStatus switch
        {
            UserAccountStatus.Suspended => "Suspended",
            UserAccountStatus.Banned => "Closed",
            _ => seller.IsVerified ? "Active" : "Pending"
        };
    }
}
