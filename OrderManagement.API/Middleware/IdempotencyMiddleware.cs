using Microsoft.AspNetCore.Mvc;

namespace OrderManagement.API.Middleware;

public sealed class IdempotencyMiddleware(RequestDelegate next)
{
    public const string ItemKey = "IdempotencyKey";
    private const string Header = "Idempotency-Key";

    public async Task InvokeAsync(HttpContext context)
    {
        
        if (!context.Request.Method.Equals(HttpMethod.Post.Method, StringComparison.OrdinalIgnoreCase)
            || !context.Request.Path.StartsWithSegments("/api/orders", StringComparison.OrdinalIgnoreCase)
            || context.Request.Path.Value?.EndsWith("/cancel", StringComparison.OrdinalIgnoreCase) == true)
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(Header, out var keyValues)
            || string.IsNullOrWhiteSpace(keyValues))
        {
            var problem = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Title = "Missing Idempotency-Key",
                Status = StatusCodes.Status400BadRequest,
                Detail = "POST /api/orders requires an 'Idempotency-Key' header containing a UUID v4.",
                Instance = context.Request.Path
            };

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problem);
            return;
        }

        context.Items[ItemKey] = keyValues.ToString();
        await next(context);
    }
}
