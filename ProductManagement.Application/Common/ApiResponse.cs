using System.Net;

namespace ProductManagement.Application.Common
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public ApiError? Error { get; set; }

        public static ApiResponse<T> Ok(T data) => new() { Success = true, Data = data };
        public static ApiResponse<T> Fail(string code, string message, object? details = null) =>
            new() { Success = false, Error = new ApiError { Code = code, Message = message, Details = details } };
    }

    public class ApiResponse : ApiResponse<object>
    {
        public static ApiResponse Ok() => new() { Success = true, Data = null };
        public static new ApiResponse Fail(string code, string message, object? details = null) =>
            new() { Success = false, Error = new ApiError { Code = code, Message = message, Details = details } };
    }

    public class ApiError
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public object? Details { get; set; }
    }

    public class AppException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string ErrorCode { get; }

        public AppException(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest, string errorCode = "BAD_REQUEST")
            : base(message)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
        }
    }
}

