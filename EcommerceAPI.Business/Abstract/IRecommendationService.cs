using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Abstract;

public interface IRecommendationService
{
    Task<IResult> TrackProductViewAsync(int productId, int? userId, string? sessionId, CancellationToken cancellationToken = default);
    Task<IResult> TrackRecommendationClickAsync(int productId, int targetProductId, string source, int? userId, string? sessionId, CancellationToken cancellationToken = default);
    Task<IDataResult<List<ProductDto>>> GetAlsoViewedProductsAsync(int productId, int take = 4, CancellationToken cancellationToken = default);
    Task<IDataResult<List<ProductDto>>> GetFrequentlyBoughtTogetherProductsAsync(int productId, int take = 4, CancellationToken cancellationToken = default);
}
