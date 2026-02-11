using System.Net;
using System.Text.Json;
using EcommerceAPI.Core.Exceptions;

namespace EcommerceAPI.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var traceId = context.Items["CorrelationId"]?.ToString() ?? context.TraceIdentifier;
        var errorResponse = new ErrorResponse { TraceId = traceId };

        switch (exception)
        {
            case NotFoundException notFoundEx:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.ErrorCode = notFoundEx.ErrorCode ?? "RESOURCE_NOT_FOUND";
                errorResponse.Message = notFoundEx.Message;
                errorResponse.Details = new { notFoundEx.ResourceType, notFoundEx.ResourceId };
                _logger.LogWarning("Resource not found: {ResourceType} with ID {ResourceId}", 
                    notFoundEx.ResourceType, notFoundEx.ResourceId);
                break;

            case InsufficientStockException stockEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.ErrorCode = stockEx.ErrorCode ?? "INSUFFICIENT_STOCK";
                errorResponse.Message = stockEx.Message;
                errorResponse.Details = new 
                { 
                    stockEx.ProductId, 
                    stockEx.RequestedQuantity, 
                    stockEx.AvailableQuantity 
                };
                _logger.LogWarning("Insufficient stock for product {ProductId}: requested {Requested}, available {Available}",
                    stockEx.ProductId, stockEx.RequestedQuantity, stockEx.AvailableQuantity);
                break;

            case Core.Exceptions.ValidationException validationEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.ErrorCode = validationEx.ErrorCode ?? "VALIDATION_ERROR";
                errorResponse.Message = validationEx.Message;
                errorResponse.Details = validationEx.Errors;
                break;

            case BusinessException businessEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.ErrorCode = businessEx.ErrorCode ?? "BUSINESS_ERROR";
                errorResponse.Message = businessEx.Message;
                _logger.LogWarning("Business exception: {Message}", businessEx.Message);
                break;

            case DomainException domainEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.ErrorCode = domainEx.ErrorCode;
                errorResponse.Message = domainEx.Message;
                _logger.LogWarning("Domain exception: {Message}", domainEx.Message);
                break;

            case UnauthorizedAccessException:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse.ErrorCode = "UNAUTHORIZED";
                errorResponse.Message = "Yetkisiz erişim";
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.ErrorCode = "INTERNAL_ERROR";
                errorResponse.Message = "Beklenmeyen bir hata oluştu";

                if (_env.IsDevelopment() || _env.IsEnvironment("Test"))
                {
                    errorResponse.Details = exception.ToString();
                }

                _logger.LogError(exception, "Unhandled exception occurred");
                break;
        }

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var result = JsonSerializer.Serialize(errorResponse, jsonOptions);
        await response.WriteAsync(result);
    }
}

public class ErrorResponse
{
    public string? ErrorCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Details { get; set; }
    public string? TraceId { get; set; }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
