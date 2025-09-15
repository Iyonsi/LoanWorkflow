using System.Net;
using System.Text.Json;
using LoanWorkflow.Api.Features.Common;
using LoanWorkflow.Api.Features.Constants;

namespace LoanWorkflow.Api.Infrastructure;

public class ApiResponseExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiResponseExceptionMiddleware> _logger;
    public ApiResponseExceptionMiddleware(RequestDelegate next, ILogger<ApiResponseExceptionMiddleware> logger)
    { _next = next; _logger = logger; }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch(Exception ex)
        {
            var traceId = context.TraceIdentifier;
            _logger.LogError(ex, "Unhandled exception {TraceId}", traceId);
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            var resp = ApiResponse<object>.Failure("Internal server error", new[]{ ex.Message }, traceId);
            var json = JsonSerializer.Serialize(resp);
            await context.Response.WriteAsync(json);
        }
    }
}
