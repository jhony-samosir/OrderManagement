namespace OrderManagement.API.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string Header = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(Header, out var incoming) && !string.IsNullOrWhiteSpace(incoming)
            ? incoming.ToString()
            : Guid.NewGuid().ToString("N");

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers[Header] = correlationId;
        
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
