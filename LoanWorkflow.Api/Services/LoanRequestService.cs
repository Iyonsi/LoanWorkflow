using LoanWorkflow.Api.Conductor;
using LoanWorkflow.Api.Repositories;
using Microsoft.EntityFrameworkCore.Storage;
using LoanWorkflow.Shared.Domain;
using LoanWorkflow.Shared.DTOs;
using LoanWorkflow.Shared.Workflow;

namespace LoanWorkflow.Api.Services;

public interface ILoanRequestService
{
    Task<(LoanRequest request, string? workflowId)> CreateAsync(CreateLoanRequestDto dto);
    Task<DecisionResult> ApproveAsync(string requestId, DecisionDto dto);
    Task<DecisionResult> RejectAsync(string requestId, DecisionDto dto);
    Task<LoanRequest?> GetAsync(string id);
    Task<IEnumerable<LoanRequestLog>> GetLogsAsync(string id);
}

public sealed class LoanRequestService : ILoanRequestService
{
    private readonly IUnitOfWork _uow;
    private readonly IWorkflowStarter _workflowStarter;
    private readonly ILogger<LoanRequestService> _logger;
    public LoanRequestService(IUnitOfWork uow, IWorkflowStarter workflowStarter, ILogger<LoanRequestService> logger)
    { _uow = uow; _workflowStarter = workflowStarter; _logger = logger; }

    public async Task<(LoanRequest request, string? workflowId)> CreateAsync(CreateLoanRequestDto dto)
    {
        if(!FlowConfiguration.Flows.ContainsKey(dto.FlowType)) throw new ArgumentException("Invalid flowType");
        var stages = FlowConfiguration.Flows[dto.FlowType];
        var now = DateTime.UtcNow;
        var request = new LoanRequest
        {
            FlowType = dto.FlowType,
            Amount = dto.Amount,
            BorrowerId = dto.BorrowerId,
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
    var wfId = await _workflowStarter.StartWorkflowAsync("loan_dynamic_workflow", 1, new { requestId = request.Id, flowType = request.FlowType });
    return (request, wfId);
    }

    public async Task<DecisionResult> ApproveAsync(string requestId, DecisionDto dto)
    {
    var req = await _uow.LoanRequests.GetByIdAsync(requestId) ?? throw new KeyNotFoundException("Request not found");
        var stages = FlowConfiguration.Flows[req.FlowType];
        var currentStage = stages[req.StageIndex];
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
    await _uow.SaveChangesAsync();
        return new DecisionResult(true, currentStage);
    }

    public async Task<DecisionResult> RejectAsync(string requestId, DecisionDto dto)
    {
        var req = await _uow.LoanRequests.GetByIdAsync(requestId) ?? throw new KeyNotFoundException("Request not found");
        if (req.FlowType == 2) throw new InvalidOperationException("Rejections not allowed for flow 2");
        var stages = FlowConfiguration.Flows[req.FlowType];
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
        await _uow.SaveChangesAsync();
        return new DecisionResult(false, currentStage);
    }

    public Task<LoanRequest?> GetAsync(string id) => _uow.LoanRequests.GetByIdAsync(id);
    public Task<IEnumerable<LoanRequestLog>> GetLogsAsync(string id) => _uow.LoanRequestLogs.GetLogsAsync(id);
}

public sealed record DecisionResult(bool Approved, string Stage);
