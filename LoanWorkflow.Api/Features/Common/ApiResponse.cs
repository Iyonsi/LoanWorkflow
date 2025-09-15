using LoanWorkflow.Api.Features.Constants;

namespace LoanWorkflow.Api.Features.Common;

public class ApiResponse<T>
{
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Code { get; set; } = ResponseCodes.SUCCESS;
    // Alias to support pattern: response.ResponseCode
    public string ResponseCode => Code;
    public PaginationMeta? Pagination { get; set; }
    public string? TraceId { get; set; }
    public List<string>? Errors { get; set; }
    public static ApiResponse<T> Success(T data, string message = "Success", string code = ResponseCodes.SUCCESS, string? traceId=null) => new(){ Data = data, Message = message, Code = code, TraceId = traceId };
    public static ApiResponse<T> Created(T data, string message = "Created", string? traceId=null) => new(){ Data = data, Message = message, Code = ResponseCodes.CREATED, TraceId = traceId };
    public static ApiResponse<T> NotFound(string message = "Not Found", string? traceId=null) => new(){ Data = default, Message = message, Code = ResponseCodes.NOT_FOUND, TraceId = traceId };
    public static ApiResponse<T> Validation(string message, IEnumerable<string>? errors=null, string? traceId=null) => new(){ Code = ResponseCodes.VALIDATION_ERROR, Message = message, Errors = errors?.ToList(), TraceId = traceId };
    public static ApiResponse<T> Failure(string message, IEnumerable<string>? errors=null, string? traceId=null) => new(){ Code = ResponseCodes.FAILURE, Message = message, Errors = errors?.ToList(), TraceId = traceId };
}

public sealed class PaginationMeta
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public long TotalCount { get; set; }
    public bool HasNext { get; set; }
    public bool HasPrevious { get; set; }
}
