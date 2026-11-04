namespace StudentProjectManagement.Application.Common;

/// <summary>
/// Response envelope thống nhất cho tất cả endpoint.
/// </summary>
public sealed class ApiResponse<T>
{
    public bool Success { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public T? Data { get; init; }

    public object? Error { get; init; }

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    public static ApiResponse<T> SuccessResponse(
        T? data,
        string message = "Request completed successfully.",
        string code = "SUCCESS")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Code = code,
            Message = message,
            Data = data,
            Error = null,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    public static ApiResponse<T> FailureResponse(
        string code,
        string message,
        object? error = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Code = code,
            Message = message,
            Data = default,
            Error = error,
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}

/// <summary>Chi tiết lỗi an toàn trả về client; không chứa stack trace.</summary>
public sealed record ApiError(string TraceId, object? Details = null);
