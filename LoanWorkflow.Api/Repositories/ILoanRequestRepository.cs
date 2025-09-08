using LoanWorkflow.Shared.Domain;

namespace LoanWorkflow.Api.Repositories;

public interface ILoanRequestRepository : IRepository<LoanRequest>
{
}

public sealed class LoanRequestRepository : GenericRepository<LoanRequest>, ILoanRequestRepository
{
    public LoanRequestRepository(LoanWorkflow.Api.Data.LoanWorkflowDbContext db) : base(db) {}
}
