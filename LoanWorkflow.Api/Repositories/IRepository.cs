using LoanWorkflow.Shared.Domain;

namespace LoanWorkflow.Api.Repositories;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(string id);
    Task InsertAsync(T entity);
    IQueryable<T> Query();
}
