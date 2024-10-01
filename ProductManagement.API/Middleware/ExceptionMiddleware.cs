using System.Text.Json;
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
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 500;
            var InnerExceptionmessage = ex.InnerException?.Message ?? "";
            var StackTrace = ex.InnerException?.StackTrace ?? "";
            var response = _env.IsDevelopment()
                ? new ApiException(context.Response.StatusCode, ex.Message, InnerExceptionmessage, StackTrace)
                : new ApiException(context.Response.StatusCode, "Internal Server Error", "", "");

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(response, options);

            await context.Response.WriteAsync(json);
        }
    }

    private class ApiException : Exception
    {
        public ApiException(int statusCode, string message, string details, string stackTrace) : base(message)
        {
            StatusCode = statusCode;
            Details = details ?? throw new ArgumentNullException(nameof(details));
            StackTrace = stackTrace ?? throw new ArgumentNullException(nameof(stackTrace));
        }
        public int StatusCode { get; set; }
        public string Details { get; set; }
        public override string StackTrace { get; }
    }
}