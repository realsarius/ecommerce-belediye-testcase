using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EcommerceAPI.Business.Extensions;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using Microsoft.Extensions.Hosting;
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
    private readonly bool _waitForRefreshOnWrite;

    public ElasticProductSearchIndexService(
        IHttpClientFactory httpClientFactory,
        IProductDal productDal,
        IHostEnvironment hostEnvironment,
        ILogger<ElasticProductSearchIndexService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("elasticsearch");
        _productDal = productDal;
        _logger = logger;
        _waitForRefreshOnWrite = hostEnvironment.IsEnvironment("Test");
    }

    public async Task<PaginatedResponse<ProductDto>> SearchAsync(ProductListRequest request, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);

        try {
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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Elasticsearch unavailable, fallback to DB search.");
            return await FallbackSearchAsync(normalized);
        }
    }

    public async Task<List<ProductDto>> SuggestAsync(string query, int limit = 8, CancellationToken cancellationToken = default)
    {
        var normalizedQuery = query?.Trim() ?? string.Empty;
        var normalizedLimit = Math.Clamp(limit, 1, 20);

        if (normalizedQuery.Length < 2)
        {
            return new List<ProductDto>();
        }

        try
        {
            await EnsureIndexAsync(cancellationToken);

            var payload = BuildSuggestPayload(normalizedQuery, normalizedLimit);
            var response = await _httpClient.PostAsJsonAsync($"/{IndexName}/_search", payload, JsonOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Elasticsearch suggest failed: {Status}", response.StatusCode);
                return await FallbackSuggestAsync(normalizedQuery, normalizedLimit);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var suggestResponse = await JsonSerializer.DeserializeAsync<ElasticSuggestResponse>(stream, JsonOptions, cancellationToken);

            var options = suggestResponse?.Suggest?
                .GetValueOrDefault("product_suggest")?
                .FirstOrDefault()?
                .Options ?? new List<ElasticSuggestOption>();

            var suggestions = options
                .Select(x => x.Source)
                .Where(x => x is not null)
                .GroupBy(x => x!.Id)
                .Select(x => MapToDto(x.First()!))
                .Take(normalizedLimit)
                .ToList();

            return suggestions.Count > 0
                ? suggestions
                : await FallbackSuggestAsync(normalizedQuery, normalizedLimit);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Elasticsearch unavailable, fallback to DB suggestions.");
            return await FallbackSuggestAsync(normalizedQuery, normalizedLimit);
        }
    }


    public async Task IndexProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureIndexAsync(cancellationToken);

            var product = await _productDal.GetByIdWithDetailsAsync(productId);
            if (product == null || !product.IsActive)
            {
                await DeleteProductAsync(productId, cancellationToken);
                return;
            }

            var doc = MapToDocument(product);
            var writePath = _waitForRefreshOnWrite
                ? $"/{IndexName}/_doc/{productId}?refresh=wait_for"
                : $"/{IndexName}/_doc/{productId}";
            var response = await _httpClient.PutAsJsonAsync(writePath, doc, JsonOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Elasticsearch index update failed for product {ProductId}. Status: {Status}, Body: {Body}",
                    productId,
                    response.StatusCode,
                    body);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Elasticsearch unavailable, product index update skipped for ProductId={ProductId}", productId);
        }
    }

    public async Task DeleteProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureIndexAsync(cancellationToken);

            var deletePath = _waitForRefreshOnWrite
                ? $"/{IndexName}/_doc/{productId}?refresh=wait_for"
                : $"/{IndexName}/_doc/{productId}";
            var response = await _httpClient.DeleteAsync(deletePath, cancellationToken);

            if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Elasticsearch delete failed for product {ProductId}. Status: {Status}, Body: {Body}", productId, response.StatusCode, body);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Elasticsearch unavailable, product index delete skipped for ProductId={ProductId}", productId);
        }
    }

    public async Task EnsureIndexAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _indexInitialized, 1, 1) == 1)
        {
            return;
        }

        var indexCreated = false;
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
                        name = new
                        {
                            type = "text",
                            fields = new
                            {
                                keyword = new { type = "keyword", ignore_above = 256 }
                            }
                        },
                        nameSuggest = new { type = "completion" },
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

            indexCreated = true;
        }
        else if (!head.IsSuccessStatusCode)
        {
            var body = await head.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Elasticsearch index check failed. Status: {head.StatusCode}, Body: {body}");
        }

        if (!indexCreated)
        {
            await EnsureSearchFieldMappingsAsync(cancellationToken);
            await EnsureSuggestionMappingAsync(cancellationToken);
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

    private async Task EnsureSearchFieldMappingsAsync(CancellationToken cancellationToken)
    {
        var mappingPayload = new
        {
            properties = new
            {
                name = new
                {
                    type = "text",
                    fields = new
                    {
                        keyword = new { type = "keyword", ignore_above = 256 }
                    }
                }
            }
        };

        var response = await _httpClient.PutAsJsonAsync($"/{IndexName}/_mapping", mappingPayload, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Elasticsearch search mapping update failed. Status: {Status}, Body: {Body}",
                response.StatusCode,
                body);
        }
    }


    private async Task EnsureSuggestionMappingAsync(CancellationToken cancellationToken)
    {
        var mappingPayload = new
        {
            properties = new
            {
                nameSuggest = new { type = "completion" }
            }
        };

        var response = await _httpClient.PutAsJsonAsync($"/{IndexName}/_mapping", mappingPayload, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Elasticsearch suggestion mapping update failed. Status: {Status}, Body: {Body}",
                response.StatusCode,
                body);
        }
    }

    private async Task<List<ProductDto>> FallbackSuggestAsync(string query, int limit)
    {
        var (items, _) = await _productDal.GetPagedAsync(
            page: 1,
            pageSize: limit,
            search: query,
            inStock: true,
            sortBy: "name",
            sortDescending: false);

        return items.Select(x => x.ToDto()).Take(limit).ToList();
    }


    private async Task<PaginatedResponse<ProductDto>> FallbackSearchAsync(ProductListRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Search))
        {
            var (items, defaultTotalCount) = await _productDal.GetPagedAsync(
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
                TotalCount = defaultTotalCount
            };
        }

        var search = NormalizeSearchText(request.Search);
        var filteredProducts = (await _productDal.GetAllActiveWithDetailsAsync())
            .Where(p => !request.CategoryId.HasValue || p.CategoryId == request.CategoryId.Value)
            .Where(p => !request.MinPrice.HasValue || p.Price >= request.MinPrice.Value)
            .Where(p => !request.MaxPrice.HasValue || p.Price <= request.MaxPrice.Value)
            .Where(p => request.InStock != true || (p.Inventory?.QuantityAvailable ?? 0) > 0)
            .Select(product => new FallbackSearchCandidate(
                product,
                CalculateFallbackSearchScore(product, search)))
            .Where(x => x.Score > 0);

        var orderedProducts = ApplyFallbackOrdering(filteredProducts, request).ToList();
        var totalCount = orderedProducts.Count;

        return new PaginatedResponse<ProductDto>
        {
            Items = orderedProducts
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => x.Product.ToDto())
                .ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }

    private static IOrderedEnumerable<FallbackSearchCandidate> ApplyFallbackOrdering(
        IEnumerable<FallbackSearchCandidate> products,
        ProductListRequest request)
    {
        var requestedSort = request.SortBy?.Trim().ToLowerInvariant();

        return requestedSort switch
        {
            "price" => request.SortDescending
                ? products.OrderByDescending(x => x.Score)
                    .ThenByDescending(x => x.Product.Price)
                    .ThenByDescending(GetProductStock)
                    .ThenByDescending(x => x.Product.CreatedAt)
                : products.OrderByDescending(x => x.Score)
                    .ThenBy(x => x.Product.Price)
                    .ThenByDescending(GetProductStock)
                    .ThenByDescending(x => x.Product.CreatedAt),
            "created" or "createdat" => request.SortDescending
                ? products.OrderByDescending(x => x.Score)
                    .ThenByDescending(x => x.Product.CreatedAt)
                    .ThenByDescending(GetProductStock)
                : products.OrderByDescending(x => x.Score)
                    .ThenBy(x => x.Product.CreatedAt)
                    .ThenByDescending(GetProductStock),
            _ => products.OrderByDescending(x => x.Score)
                .ThenByDescending(GetProductStock)
                .ThenByDescending(x => x.Product.CreatedAt)
        };

        static int GetProductStock(FallbackSearchCandidate item) => item.Product.Inventory?.QuantityAvailable ?? 0;
    }

    private static int CalculateFallbackSearchScore(Product product, string search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return 0;
        }

        var score = 0;
        var name = NormalizeSearchText(product.Name);
        var sku = NormalizeSearchText(product.SKU);
        var description = NormalizeSearchText(product.Description);

        score += GetExactMatchScore(sku, search, 1200);
        score += GetExactMatchScore(name, search, 1000);
        score += GetStartsWithScore(sku, search, 800);
        score += GetStartsWithScore(name, search, 700);
        score += GetContainsScore(sku, search, 650);
        score += GetContainsScore(name, search, 500);
        score += GetContainsScore(description, search, 180);
        score += GetFuzzyScore(name, search, 360);
        score += GetFuzzyScore(sku, search, 260);

        return score;
    }

    private static int GetExactMatchScore(string value, string search, int score)
        => value == search ? score : 0;

    private static int GetStartsWithScore(string value, string search, int score)
        => !string.IsNullOrEmpty(value) && value.StartsWith(search, StringComparison.Ordinal) ? score : 0;

    private static int GetContainsScore(string value, string search, int score)
        => !string.IsNullOrEmpty(value) && value.Contains(search, StringComparison.Ordinal) ? score : 0;

    private static int GetFuzzyScore(string value, string search, int baseScore)
    {
        if (string.IsNullOrEmpty(value))
        {
            return 0;
        }

        var distance = GetLevenshteinDistance(value, search);
        return distance switch
        {
            0 => baseScore,
            1 => baseScore - 80,
            2 => baseScore - 160,
            _ => 0
        };
    }

    private static int GetLevenshteinDistance(string source, string target)
    {
        if (source == target)
        {
            return 0;
        }

        if (source.Length == 0)
        {
            return target.Length;
        }

        if (target.Length == 0)
        {
            return source.Length;
        }

        var matrix = new int[source.Length + 1, target.Length + 1];

        for (var i = 0; i <= source.Length; i++)
        {
            matrix[i, 0] = i;
        }

        for (var j = 0; j <= target.Length; j++)
        {
            matrix[0, j] = j;
        }

        for (var i = 1; i <= source.Length; i++)
        {
            for (var j = 1; j <= target.Length; j++)
            {
                var substitutionCost = source[i - 1] == target[j - 1] ? 0 : 1;

                matrix[i, j] = Math.Min(
                    Math.Min(
                        matrix[i - 1, j] + 1,
                        matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + substitutionCost);
            }
        }

        return matrix[source.Length, target.Length];
    }

    private static string NormalizeSearchText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value
            .Trim()
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }

    private static object BuildSearchPayload(ProductListRequest request)
    {
        var filter = new List<object>
        {
            new { term = new Dictionary<string, object> { ["isActive"] = true } }
        };

        if (request.CategoryId.HasValue)
            filter.Add(new { term = new Dictionary<string, object> { ["categoryId"] = request.CategoryId.Value } });

        var priceRange = new Dictionary<string, object>();
        if (request.MinPrice.HasValue) priceRange["gte"] = request.MinPrice.Value;
        if (request.MaxPrice.HasValue) priceRange["lte"] = request.MaxPrice.Value;
        if (priceRange.Count > 0)
            filter.Add(new { range = new Dictionary<string, object> { ["price"] = priceRange } });

        if (request.InStock == true)
            filter.Add(new { range = new Dictionary<string, object> { ["stockQuantity"] = new { gte = 1 } } });

        var hasSearch = !string.IsNullOrWhiteSpace(request.Search);
        var normalizedQuery = request.Search?.Trim() ?? string.Empty;

        object query = hasSearch
            ? new
            {
                function_score = new
                {
                    query = new
                    {
                        @bool = new
                        {
                            filter,
                            should = BuildSearchShouldClauses(normalizedQuery),
                            minimum_should_match = 1
                        }
                    },
                    functions = new object[]
                    {
                        new
                        {
                            filter = new
                            {
                                range = new
                                {
                                    stockQuantity = new { gte = 1 }
                                }
                            },
                            weight = 1.15
                        }
                    },
                    score_mode = "sum",
                    boost_mode = "multiply"
                }
            }
            : new
            {
                @bool = new
                {
                    must = new object[] { new { match_all = new { } } },
                    filter
                }
            };

        return new
        {
            from = (request.Page - 1) * request.PageSize,
            size = request.PageSize,
            query,
            sort = BuildSearchSort(request, hasSearch)
        };
    }

    private static object[] BuildSearchShouldClauses(string query)
    {
        return new object[]
        {
            new
            {
                term = new Dictionary<string, object>
                {
                    ["sku"] = new
                    {
                        value = query,
                        boost = 40
                    }
                }
            },
            new
            {
                match_phrase = new Dictionary<string, object>
                {
                    ["name"] = new
                    {
                        query,
                        boost = 18
                    }
                }
            },
            new
            {
                match_phrase_prefix = new Dictionary<string, object>
                {
                    ["name"] = new
                    {
                        query,
                        boost = 12,
                        max_expansions = 20
                    }
                }
            },
            new
            {
                multi_match = new
                {
                    query,
                    type = "best_fields",
                    fields = new[] { "name^8", "sku^10", "description^2" },
                    @operator = "and",
                    boost = 6
                }
            },
            new
            {
                multi_match = new
                {
                    query,
                    fields = new[] { "name^6", "description", "sku^8" },
                    fuzziness = "AUTO",
                    prefix_length = 1,
                    max_expansions = 25,
                    boost = 3
                }
            },
            new
            {
                multi_match = new
                {
                    query,
                    type = "bool_prefix",
                    fields = new[] { "name^7", "description" },
                    boost = 4
                }
            }
        };
    }

    private static object[] BuildSearchSort(ProductListRequest request, bool hasSearch)
    {
        if (!hasSearch)
        {
            var sortField = request.SortBy?.ToLowerInvariant() switch
            {
                "price" => "price",
                "name" => "name.keyword",
                "created" => "createdAt",
                "createdat" => "createdAt",
                _ => "name.keyword"
            };

            return new object[]
            {
                new Dictionary<string, object>
                {
                    [sortField] = new { order = request.SortDescending ? "desc" : "asc" }
                },
                new Dictionary<string, object>
                {
                    ["createdAt"] = new { order = "desc" }
                }
            };
        }

        var sort = new List<object>
        {
            new Dictionary<string, object>
            {
                ["_score"] = new { order = "desc" }
            }
        };

        var requestedSort = request.SortBy?.ToLowerInvariant();
        if (requestedSort is "price" or "created" or "createdat")
        {
            var sortField = requestedSort == "price" ? "price" : "createdAt";
            sort.Add(new Dictionary<string, object>
            {
                [sortField] = new { order = request.SortDescending ? "desc" : "asc" }
            });
        }
        else
        {
            sort.Add(new Dictionary<string, object>
            {
                ["stockQuantity"] = new { order = "desc" }
            });
        }

        sort.Add(new Dictionary<string, object>
        {
            ["createdAt"] = new { order = "desc" }
        });

        return sort.ToArray();
    }


    private static object BuildSuggestPayload(string query, int limit)
    {
        return new
        {
            size = 0,
            suggest = new
            {
                product_suggest = new
                {
                    prefix = query,
                    completion = new
                    {
                        field = "nameSuggest",
                        size = limit,
                        skip_duplicates = true,
                        fuzzy = new
                        {
                            fuzziness = "AUTO",
                            min_length = 2
                        }
                    }
                }
            }
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
            CreatedAt = product.CreatedAt,
            NameSuggest = new ElasticCompletionField { Input = BuildSuggestionInputs(product) },
        };
    }

    private sealed class ElasticCompletionField
    {
        [JsonPropertyName("input")]
        public List<string> Input { get; set; } = new();
    }


    private static List<string> BuildSuggestionInputs(Product product)
    {
        var inputs = new List<string>();

        if (!string.IsNullOrWhiteSpace(product.Name))
            inputs.Add(product.Name.Trim());

        if (!string.IsNullOrWhiteSpace(product.SKU))
            inputs.Add(product.SKU.Trim());

        var categoryName = product.Category?.Name;
        if (!string.IsNullOrWhiteSpace(categoryName) && !string.IsNullOrWhiteSpace(product.Name))
            inputs.Add($"{categoryName} {product.Name}".Trim());

        return inputs
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
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

    private sealed record FallbackSearchCandidate(Product Product, int Score);

    private sealed class ElasticSearchResponse
    {
        [JsonPropertyName("hits")]
        public ElasticHits? Hits { get; set; }
    }

    private sealed class ElasticSuggestResponse
    {
        [JsonPropertyName("suggest")]
        public Dictionary<string, List<ElasticSuggestEntry>>? Suggest { get; set; }
    }

    private sealed class ElasticSuggestEntry
    {
        [JsonPropertyName("options")]
        public List<ElasticSuggestOption> Options { get; set; } = new();
    }

    private sealed class ElasticSuggestOption
    {
        [JsonPropertyName("_source")]
        public ElasticProductDocument? Source { get; set; }
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

        [JsonPropertyName("nameSuggest")]
        public ElasticCompletionField NameSuggest { get; set; } = new();

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
