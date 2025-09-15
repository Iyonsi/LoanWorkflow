using LoanWorkflow.Api.Features.Common;
using LoanWorkflow.Api.Features.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LoanWorkflow.Api.Infrastructure;

public class ApiResponseResultFilter : IAsyncResultFilter
{
    public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if(context.Result is ObjectResult obj && obj.Value is not null && obj.Value is not ApiResponse<object> && obj.Value.GetType().IsGenericType == false)
        {
            // Wrap plain objects (avoid double wrapping, allow controllers to control generics elsewhere)
            var traceId = context.HttpContext.TraceIdentifier;
            var wrapped = ApiResponse<object>.Success(obj.Value, "Success", ResponseCodes.SUCCESS, traceId);
            context.Result = new ObjectResult(wrapped){ StatusCode = obj.StatusCode };
        }
        return next();
    }
}
