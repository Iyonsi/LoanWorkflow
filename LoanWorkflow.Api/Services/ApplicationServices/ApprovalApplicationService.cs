using LoanWorkflow.Api.Features.Common;
using LoanWorkflow.Api.Features.Constants;
using LoanWorkflow.Shared.DTOs;

namespace LoanWorkflow.Api.Services.ApplicationServices;

public interface IApprovalApplicationService
{
    Task<ApiResponse<object>> ApproveAsync(string requestId, DecisionDto dto, string traceId);
    Task<ApiResponse<object>> RejectAsync(string requestId, DecisionDto dto, string traceId);
}

public sealed class ApprovalApplicationService : IApprovalApplicationService
{
    private readonly ILoanRequestService _domain;
    public ApprovalApplicationService(ILoanRequestService domain){ _domain = domain; }

    public async Task<ApiResponse<object>> ApproveAsync(string requestId, DecisionDto dto, string traceId)
    {
        try
        {
            var result = await _domain.ApproveAsync(requestId, dto);
            return ApiResponse<object>.Success(new { requestId, stage = result.Stage, approved = result.Approved }, "Approved", ResponseCodes.SUCCESS, traceId);
        }
        catch (KeyNotFoundException)
        {
            return ApiResponse<object>.NotFound("Loan request not found", traceId);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<object>.Failure(ex.Message, traceId: traceId);
        }
    }

    public async Task<ApiResponse<object>> RejectAsync(string requestId, DecisionDto dto, string traceId)
    {
        try
        {
            var result = await _domain.RejectAsync(requestId, dto);
            return ApiResponse<object>.Success(new { requestId, stage = result.Stage, approved = result.Approved }, "Rejected", ResponseCodes.SUCCESS, traceId);
        }
        catch (KeyNotFoundException)
        {
            return ApiResponse<object>.NotFound("Loan request not found", traceId);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<object>.Failure(ex.Message, traceId: traceId);
        }
    }
}
