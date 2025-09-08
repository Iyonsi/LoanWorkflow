using LoanWorkflow.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using LoanWorkflow.Api.Data;

namespace LoanWorkflow.Api.Repositories;

public interface ILoanRequestLogRepository
{
    Task<int> CountStageDecisionAsync(string requestId, string stage);
    Task InsertAsync(LoanRequestLog log);
    Task<IEnumerable<LoanRequestLog>> GetLogsAsync(string requestId);
}

public sealed class LoanRequestLogRepository : ILoanRequestLogRepository
{
    private readonly LoanWorkflowDbContext _db;
    public LoanRequestLogRepository(LoanWorkflowDbContext db){ _db = db; }
    public Task<int> CountStageDecisionAsync(string requestId, string stage) => _db.LoanRequestLogs.CountAsync(l => l.LoanRequestId == requestId && l.Stage == stage && (l.Action == "APPROVED" || l.Action == "REJECTED"));
    public async Task InsertAsync(LoanRequestLog log){ await _db.LoanRequestLogs.AddAsync(log); /* Save deferred to UnitOfWork */ }
    public async Task<IEnumerable<LoanRequestLog>> GetLogsAsync(string requestId) => await _db.LoanRequestLogs.Where(l => l.LoanRequestId == requestId).OrderBy(l => l.CreatedAt).ToListAsync();
}
