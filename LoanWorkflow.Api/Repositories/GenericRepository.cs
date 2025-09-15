using LoanWorkflow.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using LoanWorkflow.Api.Data;

namespace LoanWorkflow.Api.Repositories;

public class GenericRepository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly LoanWorkflowDbContext _db;
    protected readonly DbSet<T> _set;
    public GenericRepository(LoanWorkflowDbContext db){ _db = db; _set = db.Set<T>(); }

    public Task<T?> GetByIdAsync(string id) => _set.FirstOrDefaultAsync(x => x.Id == id)!;

    public async Task InsertAsync(T entity)
    {
        await _set.AddAsync(entity);
        // Defer SaveChanges to UnitOfWork
    }

    public IQueryable<T> Query() => _set.AsQueryable();
}

