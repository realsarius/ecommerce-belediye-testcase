using Serilog.Context;
using EcommerceAPI.Core.CrossCuttingConcerns;
using System.Diagnostics;

namespace EcommerceAPI.API.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICorrelationIdProvider _correlationIdProvider;
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next, ICorrelationIdProvider correlationIdProvider)
    {
        _next = next;
        _correlationIdProvider = correlationIdProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        context.Items["CorrelationId"] = correlationId.ToString();
        _correlationIdProvider.SetCorrelationId(correlationId.ToString());

        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationIdHeader))
            {
                context.Response.Headers.Append(CorrelationIdHeader, correlationId);
            }
            return Task.CompletedTask;
        });

        var currentActivity = Activity.Current;
        var traceId = currentActivity?.TraceId.ToString();
        var spanId = currentActivity?.SpanId.ToString();

        using (LogContext.PushProperty("CorrelationId", correlationId.ToString()))
        using (LogContext.PushProperty("TraceId", traceId ?? string.Empty))
        using (LogContext.PushProperty("SpanId", spanId ?? string.Empty))
        {
            await _next(context);
        }
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
