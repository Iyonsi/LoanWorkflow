using LoanWorkflow.Shared.Domain;
using Microsoft.EntityFrameworkCore.Storage;

namespace LoanWorkflow.Api.Repositories;

public interface IUnitOfWork
{
    ILoanRequestRepository LoanRequests { get; }
    ILoanRequestLogRepository LoanRequestLogs { get; }
    IRepository<T> Repository<T>() where T : BaseEntity;
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
}
