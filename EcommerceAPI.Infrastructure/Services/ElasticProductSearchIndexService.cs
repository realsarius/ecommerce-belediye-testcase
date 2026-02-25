using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EcommerceAPI.Business.Extensions;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using Microsoft.Extensions.Logging;

namespace EcommerceAPI.Infrastructure.Services;

public class ElasticProductSearchIndexService : IProductSearchIndexService
{
    private const string IndexName = "products-v1";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static int _indexInitialized;

    private readonly HttpClient _httpClient;
    private readonly IProductDal _productDal;
    private readonly ILogger<ElasticProductSearchIndexService> _logger;

    public ElasticProductSearchIndexService(
        IHttpClientFactory httpClientFactory,
        IProductDal productDal,
        ILogger<ElasticProductSearchIndexService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("elasticsearch");
        _productDal = productDal;
        _logger = logger;
    }

    public async Task<PaginatedResponse<ProductDto>> SearchAsync(ProductListRequest request, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        await EnsureIndexAsync(cancellationToken);

        var payload = BuildSearchPayload(normalized);
        var response = await _httpClient.PostAsJsonAsync($"/{IndexName}/_search", payload, JsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Elasticsearch search failed: {Status}", response.StatusCode);
            return await FallbackSearchAsync(normalized);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var searchResponse = await JsonSerializer.DeserializeAsync<ElasticSearchResponse>(stream, JsonOptions, cancellationToken);

        if (searchResponse?.Hits?.Hits == null)
        {
            return await FallbackSearchAsync(normalized);
        }

        var items = searchResponse.Hits.Hits
            .Select(h => h.Source)
            .Where(s => s is not null)
            .Select(MapToDto)
            .ToList();

        var totalCount = (int)Math.Min(searchResponse.Hits.Total?.Value ?? items.Count, int.MaxValue);

        return new PaginatedResponse<ProductDto>
        {
            Items = items,
            Page = normalized.Page,
            PageSize = normalized.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task IndexProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        await EnsureIndexAsync(cancellationToken);

        var product = await _productDal.GetByIdWithDetailsAsync(productId);
        if (product == null || !product.IsActive)
        {
            await DeleteProductAsync(productId, cancellationToken);
            return;
        }

        var doc = MapToDocument(product);
        var response = await _httpClient.PutAsJsonAsync($"/{IndexName}/_doc/{productId}", doc, JsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Elasticsearch index update failed for product {productId}. Status: {response.StatusCode}, Body: {body}");
        }
    }

    public async Task DeleteProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        await EnsureIndexAsync(cancellationToken);

        var response = await _httpClient.DeleteAsync($"/{IndexName}/_doc/{productId}", cancellationToken);

        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Elasticsearch delete failed for product {ProductId}. Status: {Status}, Body: {Body}", productId, response.StatusCode, body);
        }
    }

    public async Task EnsureIndexAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _indexInitialized, 1, 1) == 1)
        {
            return;
        }

        var head = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, $"/{IndexName}"), cancellationToken);

        if (head.StatusCode == HttpStatusCode.NotFound)
        {
            var indexDefinition = new
            {
                mappings = new
                {
                    properties = new
                    {
                        id = new { type = "integer" },
                        name = new { type = "text" },
                        description = new { type = "text" },
                        sku = new { type = "keyword" },
                        price = new { type = "double" },
                        currency = new { type = "keyword" },
                        isActive = new { type = "boolean" },
                        categoryId = new { type = "integer" },
                        categoryName = new { type = "keyword" },
                        stockQuantity = new { type = "integer" },
                        sellerId = new { type = "integer" },
                        sellerBrandName = new { type = "keyword" },
                        createdAt = new { type = "date" }
                    }
                }
            };

            var create = await _httpClient.PutAsJsonAsync($"/{IndexName}", indexDefinition, JsonOptions, cancellationToken);
            if (!create.IsSuccessStatusCode)
            {
                var body = await create.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"Elasticsearch index create failed. Status: {create.StatusCode}, Body: {body}");
            }
        }
        else if (!head.IsSuccessStatusCode)
        {
            var body = await head.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Elasticsearch index check failed. Status: {head.StatusCode}, Body: {body}");
        }

        await BackfillIfEmptyAsync(cancellationToken);

        Interlocked.Exchange(ref _indexInitialized, 1);
    }

    private async Task BackfillIfEmptyAsync(CancellationToken cancellationToken)
    {
        var countResponse = await _httpClient.GetAsync($"/{IndexName}/_count", cancellationToken);
        if (!countResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("Could not read Elasticsearch count for index {IndexName}", IndexName);
            return;
        }

        await using var stream = await countResponse.Content.ReadAsStreamAsync(cancellationToken);
        var countPayload = await JsonSerializer.DeserializeAsync<CountResponse>(stream, JsonOptions, cancellationToken);

        if ((countPayload?.Count ?? 0) > 0)
        {
            return;
        }

        var products = await _productDal.GetAllActiveWithDetailsAsync();

        foreach (var product in products)
        {
            var doc = MapToDocument(product);
            var response = await _httpClient.PutAsJsonAsync($"/{IndexName}/_doc/{product.Id}", doc, JsonOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Backfill failed for product {ProductId}. Status: {Status}, Body: {Body}", product.Id, response.StatusCode, body);
            }
        }
    }

    private async Task<PaginatedResponse<ProductDto>> FallbackSearchAsync(ProductListRequest request)
    {
        var (items, totalCount) = await _productDal.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.CategoryId,
            request.MinPrice,
            request.MaxPrice,
            request.Search,
            request.InStock,
            request.SortBy,
            request.SortDescending
        );

        return new PaginatedResponse<ProductDto>
        {
            Items = items.Select(p => p.ToDto()).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }

    private static object BuildSearchPayload(ProductListRequest request)
    {
        var must = new List<object>();
        var filter = new List<object>();

        if (string.IsNullOrWhiteSpace(request.Search))
        {
            must.Add(new { match_all = new { } });
        }
        else
        {
            must.Add(new
            {
                multi_match = new
                {
                    query = request.Search,
                    fields = new[] { "name^3", "description", "sku" }
                }
            });
        }

        filter.Add(new { term = new Dictionary<string, object> { ["isActive"] = true } });

        if (request.CategoryId.HasValue)
            filter.Add(new { term = new Dictionary<string, object> { ["categoryId"] = request.CategoryId.Value } });

        var priceRange = new Dictionary<string, object>();
        if (request.MinPrice.HasValue) priceRange["gte"] = request.MinPrice.Value;
        if (request.MaxPrice.HasValue) priceRange["lte"] = request.MaxPrice.Value;
        if (priceRange.Count > 0)
            filter.Add(new { range = new Dictionary<string, object> { ["price"] = priceRange } });

        if (request.InStock == true)
            filter.Add(new { range = new Dictionary<string, object> { ["stockQuantity"] = new { gte = 1 } } });

        var sortField = request.SortBy?.ToLowerInvariant() switch
        {
            "price" => "price",
            "name" => "name.keyword",
            "created" => "createdAt",
            "createdat" => "createdAt",
            _ => "createdAt"
        };

        var sort = new[]
        {
            new Dictionary<string, object>
            {
                [sortField] = new { order = request.SortDescending ? "desc" : "asc" }
            }
        };

        return new
        {
            from = (request.Page - 1) * request.PageSize,
            size = request.PageSize,
            query = new
            {
                @bool = new
                {
                    must,
                    filter
                }
            },
            sort
        };
    }

    private static ProductListRequest Normalize(ProductListRequest request)
    {
        if (request.Page <= 0) request.Page = 1;
        if (request.PageSize <= 0) request.PageSize = 10;
        if (request.PageSize > 100) request.PageSize = 100;
        return request;
    }

    private static ElasticProductDocument MapToDocument(Product product)
    {
        return new ElasticProductDocument
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Currency = product.Currency,
            Sku = product.SKU,
            IsActive = product.IsActive,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? string.Empty,
            StockQuantity = product.Inventory?.QuantityAvailable ?? 0,
            SellerId = product.SellerId,
            SellerBrandName = product.Seller?.BrandName,
            CreatedAt = product.CreatedAt
        };
    }

    private static ProductDto MapToDto(ElasticProductDocument doc)
    {
        return new ProductDto
        {
            Id = doc.Id,
            Name = doc.Name,
            Description = doc.Description,
            Price = doc.Price,
            Currency = doc.Currency,
            SKU = doc.Sku,
            IsActive = doc.IsActive,
            CategoryId = doc.CategoryId,
            CategoryName = doc.CategoryName,
            StockQuantity = doc.StockQuantity,
            SellerId = doc.SellerId,
            SellerBrandName = doc.SellerBrandName
        };
    }

    private sealed class CountResponse
    {
        [JsonPropertyName("count")]
        public long Count { get; set; }
    }

    private sealed class ElasticSearchResponse
    {
        [JsonPropertyName("hits")]
        public ElasticHits? Hits { get; set; }
    }

    private sealed class ElasticHits
    {
        [JsonPropertyName("total")]
        public ElasticTotal? Total { get; set; }

        [JsonPropertyName("hits")]
        public List<ElasticHit> Hits { get; set; } = new();
    }

    private sealed class ElasticTotal
    {
        [JsonPropertyName("value")]
        public long Value { get; set; }
    }

    private sealed class ElasticHit
    {
        [JsonPropertyName("_source")]
        public ElasticProductDocument? Source { get; set; }
    }

    private sealed class ElasticProductDocument
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "TRY";

        [JsonPropertyName("sku")]
        public string Sku { get; set; } = string.Empty;

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        [JsonPropertyName("categoryId")]
        public int CategoryId { get; set; }

        [JsonPropertyName("categoryName")]
        public string CategoryName { get; set; } = string.Empty;

        [JsonPropertyName("stockQuantity")]
        public int StockQuantity { get; set; }

        [JsonPropertyName("sellerId")]
        public int? SellerId { get; set; }

        [JsonPropertyName("sellerBrandName")]
        public string? SellerBrandName { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
    }
}
