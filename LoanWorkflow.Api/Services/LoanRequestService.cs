using LoanWorkflow.Api.Conductor;
using LoanWorkflow.Api.Repositories;
using Microsoft.EntityFrameworkCore.Storage;
using LoanWorkflow.Shared.Domain;
using LoanWorkflow.Shared.DTOs;
using LoanWorkflow.Shared.Workflow;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace LoanWorkflow.Api.Services;

public interface ILoanRequestService
{
    Task<(LoanRequest request, string? workflowId)> CreateAsync(CreateLoanRequestDto dto, string loanType);
    Task<DecisionResult> ApproveAsync(string requestId, DecisionDto dto, string userRole);
    Task<DecisionResult> RejectAsync(string requestId, DecisionDto dto);
    Task<LoanRequest?> GetAsync(string id);
    Task<IEnumerable<LoanRequestLog>> GetLogsAsync(string id);
    Task<(IEnumerable<LoanRequest> Items, long TotalCount)> SearchAsync(LoanRequestSearchQuery query, CancellationToken ct = default);
}

public sealed class LoanRequestService : ILoanRequestService
{
    private readonly IUnitOfWork _uow;
    private readonly IWorkflowStarter _workflowStarter;
    private readonly ILogger<LoanRequestService> _logger;
    private readonly bool _conductorEnabled;
    public LoanRequestService(IUnitOfWork uow, IWorkflowStarter workflowStarter, ILogger<LoanRequestService> logger, IConfiguration cfg)
    { _uow = uow; _workflowStarter = workflowStarter; _logger = logger; _conductorEnabled = !(bool.TryParse(cfg["Conductor:Enabled"], out var en) && en == false); }

    public async Task<(LoanRequest request, string? workflowId)> CreateAsync(CreateLoanRequestDto dto, string loanType)
    {
        if(!FlowConfiguration.Flows.ContainsKey(loanType)) throw new ArgumentException("Invalid loanType");
        var stages = FlowConfiguration.Flows[loanType];
        var now = DateTime.UtcNow;
        var request = new LoanRequest
        {
            LoanType = loanType,
            Amount = dto.Amount,
            BorrowerId = dto.BorrowerId,
            FullName = dto.FullName,
            IsEligible = dto.IsEligible,
            Status = LoanRequestStatus.InProgress,
            CurrentStage = stages[0],
            StageIndex = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
        await _uow.LoanRequests.InsertAsync(request);
        var log = new LoanRequestLog
        {
            LoanRequestId = request.Id,
            Stage = stages[0],
            Action = LoanRequestActions.Submitted,
            ActorUserId = string.Empty,
            CreatedAt = now,
            UpdatedAt = now
        };
        await _uow.LoanRequestLogs.InsertAsync(log);
        await _uow.SaveChangesAsync();
        var wfId = await _workflowStarter.StartWorkflowAsync("loan_dynamic_workflow", 1, new { requestId = request.Id, loanType = request.LoanType });
        return (request, wfId);
    }

    public async Task<DecisionResult> ApproveAsync(string requestId, DecisionDto dto, string userRole)
    {
    var req = await _uow.LoanRequests.GetByIdAsync(requestId) ?? throw new KeyNotFoundException("Request not found");
        var stages = FlowConfiguration.Flows[req.LoanType];
        // Determine the stage being approved. We trust the persisted CurrentStage/StageIndex pairing.
        var currentStage = stages[req.StageIndex];
        // Enforce that the logged-in user's role matches the current stage.
        if(!string.Equals(userRole, currentStage, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Approve role mismatch: request {RequestId} expected stage {Expected} but user role {Role}", requestId, currentStage, userRole);
            throw new InvalidOperationException("User not authorized for current stage");
        }
        var existing = await _uow.LoanRequestLogs.CountStageDecisionAsync(requestId, currentStage);
        if (existing > 0)
        {
            _logger.LogWarning("Duplicate approval attempt for request {RequestId} stage {Stage}", requestId, currentStage);
            throw new InvalidOperationException("Stage already decided");
        }
        var now = DateTime.UtcNow;
    await _uow.LoanRequestLogs.InsertAsync(new LoanRequestLog
        {
            LoanRequestId = requestId,
            Stage = currentStage,
            Action = LoanRequestActions.Approved,
            ActorUserId = dto.ActorUserId,
            Comments = dto.Comments,
            CreatedAt = now,
            UpdatedAt = now
        });
        // Progress to next stage (was previously skipped when Conductor enabled causing stage to remain FT).
        if(req.StageIndex < stages.Length - 1)
        {
            req.StageIndex += 1;
            req.CurrentStage = stages[req.StageIndex];
            req.UpdatedAt = now;
        }
        else
        {
            // Final stage approved -> mark request approved
            req.Status = LoanRequestStatus.Approved;
            req.UpdatedAt = now;
            // Create Loan record if not already created
            try
            {
                var loanRepo = _uow.Repository<Loan>();
                // Use AnyAsync if EF provider present, otherwise fallback to in-memory evaluation
                var loanQuery = loanRepo.Query().Where(l => l.LoanRequestId == requestId);
                bool exists;
                if (loanQuery.Provider is Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider)
                {
                    exists = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync((IQueryable<Loan>)loanQuery);
                }
                else
                {
                    exists = loanQuery.Any();
                }
                if(!exists)
                {
                    var loanNumber = "LN-" + req.Id[..8].ToUpperInvariant();
                    await loanRepo.InsertAsync(new Loan
                    {
                        LoanRequestId = requestId,
                        LoanNumber = loanNumber,
                        FullName = req.FullName,
                        Principal = req.Amount,
                        InterestRate = 0m,
                        TermMonths = 0,
                        StartDate = DateTime.UtcNow.Date,
                        Status = "ACTIVE",
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                    _logger.LogInformation("Created Loan record {LoanNumber} for request {RequestId}", loanNumber, requestId);
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Failed to create Loan record for request {RequestId}", requestId);
                // Intentionally swallow so approval flow still succeeds; alternatively rethrow if strict consistency required
            }
        }
    await _uow.SaveChangesAsync();
        return new DecisionResult(true, currentStage, req.FullName);
    }

    public async Task<DecisionResult> RejectAsync(string requestId, DecisionDto dto)
    {
    var req = await _uow.LoanRequests.GetByIdAsync(requestId) ?? throw new KeyNotFoundException("Request not found");
    if (string.Equals(req.LoanType, "multi_stage", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Rejections not allowed for multi_stage");
    var stages = FlowConfiguration.Flows[req.LoanType];
        var currentStage = stages[req.StageIndex];
        var existing = await _uow.LoanRequestLogs.CountStageDecisionAsync(requestId, currentStage);
        if (existing > 0)
        {
            _logger.LogWarning("Duplicate rejection attempt for request {RequestId} stage {Stage}", requestId, currentStage);
            throw new InvalidOperationException("Stage already decided");
        }
        var now = DateTime.UtcNow;
        await _uow.LoanRequestLogs.InsertAsync(new LoanRequestLog
        {
            LoanRequestId = requestId,
            Stage = currentStage,
            Action = LoanRequestActions.Rejected,
            ActorUserId = dto.ActorUserId,
            Comments = dto.Comments,
            CreatedAt = now,
            UpdatedAt = now
        });
        if(!_conductorEnabled)
        {
            // Flow 1: reset to first stage
            if(string.Equals(req.LoanType, "standard", StringComparison.OrdinalIgnoreCase))
            {
                req.StageIndex = 0;
                req.CurrentStage = stages[0];
                req.UpdatedAt = now;
            }
            // Flow 3: step back one stage if possible
            else if(string.Equals(req.LoanType, "flex_review", StringComparison.OrdinalIgnoreCase))
            {
                if(req.StageIndex > 0)
                {
                    req.StageIndex -= 1;
                    req.CurrentStage = stages[req.StageIndex];
                    req.UpdatedAt = now;
                }
            }
        }
    await _uow.SaveChangesAsync();
    return new DecisionResult(false, currentStage, req.FullName);
    }

    public Task<LoanRequest?> GetAsync(string id) => _uow.LoanRequests.GetByIdAsync(id);
    public Task<IEnumerable<LoanRequestLog>> GetLogsAsync(string id) => _uow.LoanRequestLogs.GetLogsAsync(id);

    public async Task<(IEnumerable<LoanRequest> Items, long TotalCount)> SearchAsync(LoanRequestSearchQuery query, CancellationToken ct = default)
    {
        var q = _uow.LoanRequests.Query();
        if(!string.IsNullOrWhiteSpace(query.LoanRequestId)) q = q.Where(x => x.Id == query.LoanRequestId);
        if(query.Stages?.Length > 0) q = q.Where(x => query.Stages.Contains(x.CurrentStage));
        if(!string.IsNullOrWhiteSpace(query.LoanType)) q = q.Where(x => x.LoanType == query.LoanType);
        if(query.CreatedFromUtc.HasValue) q = q.Where(x => x.CreatedAt >= query.CreatedFromUtc.Value);
        if(query.CreatedToUtc.HasValue) q = q.Where(x => x.CreatedAt <= query.CreatedToUtc.Value);
        if(query.Statuses?.Length > 0) q = q.Where(x => query.Statuses.Contains(x.Status));
        q = q.OrderByDescending(x => x.CreatedAt);
        // Fallback to synchronous enumeration if provider does not support async (e.g., unit tests with plain LINQ)
        if(q.Provider is not Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider)
        {
            var listAll = q.ToList();
            var totalSync = listAll.LongCount();
            var segment = listAll.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList();
            return (segment, totalSync);
        }
        var total = await q.LongCountAsync(ct);
        var items = await q.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);
        return (items, total);
    }
}

public sealed record DecisionResult(bool Approved, string Stage, string FullName);

public sealed class LoanRequestSearchQuery
{
    public string? LoanRequestId { get; set; }
    public string? LoanType { get; set; }
    public string[]? Stages { get; set; }
    public DateTime? CreatedFromUtc { get; set; }
    public DateTime? CreatedToUtc { get; set; }
    public LoanRequestStatus[]? Statuses { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
