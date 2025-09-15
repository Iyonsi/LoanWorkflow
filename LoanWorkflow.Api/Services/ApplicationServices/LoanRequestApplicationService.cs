using LoanWorkflow.Api.Features.Common;
using LoanWorkflow.Api.Features.Constants;
using LoanWorkflow.Shared.DTOs;
using LoanWorkflow.Shared.Domain;
using LoanWorkflow.Api.Features.LoanRequests;

namespace LoanWorkflow.Api.Services.ApplicationServices;

public interface ILoanRequestApplicationService
{
    Task<ApiResponse<object>> CreateAsync(CreateLoanRequestDto dto, string loanType, string traceId);
    Task<ApiResponse<LoanRequest>> GetAsync(string id, string traceId);
    Task<ApiResponse<IEnumerable<LoanRequestLog>>> GetLogsAsync(string id, string traceId);
    Task<ApiResponse<IEnumerable<LoanRequestSummaryDto>>> SearchAsync(string? loanRequestId, string? stages, string? loanType, DateTime? createdFromUtc, DateTime? createdToUtc, DateTime? date, string? statuses, int page, int pageSize, string traceId, CancellationToken ct = default);
}

public sealed class LoanRequestApplicationService : ILoanRequestApplicationService
{
    private readonly ILoanRequestService _domain;
    public LoanRequestApplicationService(ILoanRequestService domain){ _domain = domain; }

    public async Task<ApiResponse<object>> CreateAsync(CreateLoanRequestDto dto, string loanType, string traceId)
    {
    if(dto is null) return ApiResponse<object>.Validation("Payload required", traceId: traceId);
    var errors = new List<string>();
    if(dto.Amount < 1) errors.Add("Amount must be greater than zero");
    if(string.IsNullOrWhiteSpace(dto.BorrowerId)) errors.Add("BorrowerId required");
    if(string.IsNullOrWhiteSpace(dto.FullName)) errors.Add("FullName required");
        if(errors.Count>0)
        {
            Console.WriteLine($"CreateAsync validation errors: {string.Join(';', errors)}");
        }
    if(errors.Count>0) return ApiResponse<object>.Validation("Invalid payload", errors, traceId);
        var (req, wfId) = await _domain.CreateAsync(dto, loanType);
        return ApiResponse<object>.Created(new { requestId = req.Id, workflowId = wfId, loanType = req.LoanType }, "Created", traceId);
    }

    public async Task<ApiResponse<LoanRequest>> GetAsync(string id, string traceId)
    {
        var req = await _domain.GetAsync(id);
        if(req is null) return ApiResponse<LoanRequest>.NotFound("Loan request not found", traceId);
        return ApiResponse<LoanRequest>.Success(req, traceId: traceId);
    }

    public async Task<ApiResponse<IEnumerable<LoanRequestLog>>> GetLogsAsync(string id, string traceId)
    {
        var logs = await _domain.GetLogsAsync(id);
        return ApiResponse<IEnumerable<LoanRequestLog>>.Success(logs, traceId: traceId);
    }

    public async Task<ApiResponse<IEnumerable<LoanRequestSummaryDto>>> SearchAsync(string? loanRequestId, string? stages, string? loanType, DateTime? createdFromUtc, DateTime? createdToUtc, DateTime? date, string? statuses, int page, int pageSize, string traceId, CancellationToken ct = default)
    {
        string[]? stageArray = string.IsNullOrWhiteSpace(stages) ? null : stages.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        LoanRequestStatus[]? statusArray = null;
        if(!string.IsNullOrWhiteSpace(statuses))
        {
            var parts = statuses.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var list = new List<LoanRequestStatus>();
            foreach(var p in parts)
                if(Enum.TryParse<LoanRequestStatus>(p, true, out var st)) list.Add(st);
            statusArray = list.Count > 0 ? list.ToArray() : null;
        }
        if(date.HasValue)
        {
            createdFromUtc = date.Value.Date;
            createdToUtc = date.Value.Date.AddDays(1).AddTicks(-1);
        }
        var query = new LoanRequestSearchQuery
        {
            LoanRequestId = loanRequestId,
            Stages = stageArray,
            LoanType = loanType,
            CreatedFromUtc = createdFromUtc,
            CreatedToUtc = createdToUtc,
            Statuses = statusArray,
            Page = page,
            PageSize = pageSize
        };
        var (items, total) = await _domain.SearchAsync(query, ct);
        var summaries = items.Select(x => new LoanRequestSummaryDto{ Id=x.Id, LoanType=x.LoanType, Amount=x.Amount, FullName=x.FullName, IsEligible=x.IsEligible, CurrentStage=x.CurrentStage, StageIndex=x.StageIndex, Status=x.Status, CreatedAt=x.CreatedAt }).ToList();
        var resp = ApiResponse<IEnumerable<LoanRequestSummaryDto>>.Success(summaries, traceId: traceId);
        resp.Pagination = new PaginationMeta{ PageNumber=page, PageSize=pageSize, TotalCount=total, TotalPages=(int)Math.Ceiling(total/(double)pageSize), HasNext = page * pageSize < total, HasPrevious = page>1 };
        return resp;
    }
}
