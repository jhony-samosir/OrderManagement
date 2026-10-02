using Microsoft.AspNetCore.Mvc;
using OrderManagement.Service.Exceptions;
using System.Net;

namespace OrderManagement.API.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for {Method} {Path}", 
                context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            NotFoundException => (HttpStatusCode.NotFound, "Resource Not Found"),
            InsufficientStockException => (HttpStatusCode.Conflict, "Insufficient Stock"),
            ConflictException => (HttpStatusCode.Conflict, "Concurrent Modification Conflict"),
            InvalidStatusTransitionException => (HttpStatusCode.UnprocessableEntity, "Invalid Status Transition"),
            _ => (HttpStatusCode.InternalServerError, "Internal Server Error")
        };

        var correlationId = context.Items.TryGetValue("CorrelationId", out var id) ? id?.ToString() : null;

        var problem = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7807",
            Title = title,
            Status = (int)statusCode,
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        if (correlationId is not null)
            problem.Extensions["correlationId"] = correlationId;

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problem);
    }
}
