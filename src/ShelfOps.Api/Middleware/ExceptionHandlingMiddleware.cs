using System.Net;
using System.Text.Json;

namespace ShelfOps.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

#pragma warning disable CA1848 // Log performance
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business logic violation");
            await WriteProblemDetails(context, HttpStatusCode.BadRequest, "Business rule violation", ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument");
            await WriteProblemDetails(context, HttpStatusCode.BadRequest, "Invalid input", ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found");
            await WriteProblemDetails(context, HttpStatusCode.NotFound, "Not found", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access");
            await WriteProblemDetails(context, HttpStatusCode.Forbidden, "Forbidden", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteProblemDetails(context, HttpStatusCode.InternalServerError,
                "Internal server error", "An unexpected error occurred. Please try again later.");
        }
    }
#pragma warning restore CA1848

    private static async Task WriteProblemDetails(HttpContext context, HttpStatusCode statusCode,
        string title, string detail)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type = $"https://httpstatuses.io/{(int)statusCode}",
            title,
            detail,
            status = (int)statusCode,
            traceId = context.TraceIdentifier
        };

        var json = JsonSerializer.Serialize(problem, JsonOptions);
        await context.Response.WriteAsync(json);
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
        => builder.UseMiddleware<ExceptionHandlingMiddleware>();
}