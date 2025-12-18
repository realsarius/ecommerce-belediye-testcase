namespace EcommerceAPI.Core.Parameters;

public record ProductFilterParams(
    int Page,
    int PageSize,
    int? CategoryId,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? Search,
    bool? InStock,
    string SortBy,
    bool SortDescending
);
