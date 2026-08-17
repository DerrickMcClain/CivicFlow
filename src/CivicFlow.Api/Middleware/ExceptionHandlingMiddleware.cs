using System.Net;
using System.Text.Json;
using CivicFlow.Application.Common;

namespace CivicFlow.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException ex)
        {
            await WriteAsync(context, ex.Status, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            var message = context.RequestServices
                .GetRequiredService<IHostEnvironment>()
                .IsProduction()
                ? "An unexpected error occurred."
                : ex.Message;
            await WriteAsync(context, (int)HttpStatusCode.InternalServerError, message);
        }
    }

    private static async Task WriteAsync(HttpContext context, int status, string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status,
            message,
            traceId = context.TraceIdentifier
        }, JsonOptions));
    }
}
