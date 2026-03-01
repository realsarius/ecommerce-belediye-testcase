using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceAPI.DataAccess;

public static class DependencyInjection
{
    public static IServiceCollection AddDataAccessServices(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IProductDal, EfProductDal>();
        services.AddScoped<IOrderDal, EfOrderDal>();
        services.AddScoped<ICategoryDal, EfCategoryDal>();
        services.AddScoped<ICartDal, EfCartDal>();
        services.AddScoped<IInventoryDal, EfInventoryDal>();
        services.AddScoped<IUserDal, EfUserDal>();
        services.AddScoped<IRoleDal, EfRoleDal>();
        services.AddScoped<IRefreshTokenDal, EfRefreshTokenDal>();
        services.AddScoped<IPaymentDal, EfPaymentDal>();
        services.AddScoped<IShippingAddressDal, EfShippingAddressDal>();
        services.AddScoped<ICouponDal, EfCouponDal>();
        services.AddScoped<ISellerProfileDal, EfSellerProfileDal>();
        services.AddScoped<ICreditCardDal, EfCreditCardDal>();
        services.AddScoped<ISupportConversationDal, EfSupportConversationDal>();
        services.AddScoped<ISupportMessageDal, EfSupportMessageDal>();
        services.AddScoped<IProductReviewDal, EfProductReviewDal>();
        services.AddScoped<IWishlistDal, EfWishlistDal>();
        services.AddScoped<IWishlistCollectionDal, EfWishlistCollectionDal>();
        services.AddScoped<IWishlistItemDal, EfWishlistItemDal>();
        services.AddScoped<IPriceAlertDal, EfPriceAlertDal>();
        services.AddScoped<IReturnRequestDal, EfReturnRequestDal>();
        services.AddScoped<IRefundRequestDal, EfRefundRequestDal>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
