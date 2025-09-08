using System.Collections.Concurrent;
using LoanWorkflow.Api.Data;
using LoanWorkflow.Shared.Domain;
using Microsoft.EntityFrameworkCore.Storage;

namespace LoanWorkflow.Api.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly LoanWorkflowDbContext _db;
    private readonly IServiceProvider _sp;
    private readonly ConcurrentDictionary<Type, object> _repos = new();
    public UnitOfWork(LoanWorkflowDbContext db, IServiceProvider sp, ILoanRequestRepository lr, ILoanRequestLogRepository logs)
    { _db = db; _sp = sp; LoanRequests = lr; LoanRequestLogs = logs; }
    public ILoanRequestRepository LoanRequests { get; }
    public ILoanRequestLogRepository LoanRequestLogs { get; }
    public IRepository<T> Repository<T>() where T : BaseEntity => (IRepository<T>) _repos.GetOrAdd(typeof(T), _ => new GenericRepository<T>(_db));
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default) => _db.Database.BeginTransactionAsync(ct);
}
