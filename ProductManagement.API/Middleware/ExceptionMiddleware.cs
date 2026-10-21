using System.Net;
using System.Text.Json;
using ProductManagement.Application.Common;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
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
        catch (AppException appEx)
        {
            _logger.LogWarning("AppException: {Message} (Code: {Code})", appEx.Message, appEx.ErrorCode);
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)appEx.StatusCode;

            var errorResponse = new
            {
                success = false,
                message = appEx.Message,
                error = new
                {
                    code = appEx.ErrorCode,
                    message = appEx.Message
                }
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Beklenmeyen hata: {Message}", ex.Message);
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var details = _env.IsDevelopment() ? (ex.InnerException?.Message ?? ex.StackTrace ?? "") : null;
            var message = _env.IsDevelopment() ? ex.Message : "Sunucu tarafında bir hata oluştu.";

            var errorResponse = new
            {
                success = false,
                message = message,
                error = new
                {
                    code = "INTERNAL_SERVER_ERROR",
                    message = message,
                    details = details
                }
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }
    }
}