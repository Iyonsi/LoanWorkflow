using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using LoanWorkflow.Api.Data;
using LoanWorkflow.Api.Repositories;
using LoanWorkflow.Shared.Domain;

namespace LoanWorkflow.Tests;

[TestFixture]
public class RepositoryTests
{
    private LoanWorkflowDbContext _db = null!;

    [SetUp]
    public void Setup()
    {
        var opts = new DbContextOptionsBuilder<LoanWorkflowDbContext>()
            .UseInMemoryDatabase(databaseName: "repo_tests")
            .Options;
        _db = new LoanWorkflowDbContext(opts);
    }

    [TearDown]
    public void Teardown()
    {
        _db.Dispose();
    }

    [Test]
    public async Task GenericRepository_InsertAndGet_Works()
    {
        var repo = new GenericRepository<LoanRequest>(_db);
        var entity = new LoanRequest { BorrowerId = "B1", Amount = 50, FlowType = 1, CurrentStage = "FT", StageIndex = 0, Status = LoanRequestStatus.InProgress };
        await repo.InsertAsync(entity);
        await _db.SaveChangesAsync();
        var fetched = await repo.GetByIdAsync(entity.Id);
        Assert.That(fetched, Is.Not.Null);
        Assert.That(fetched!.BorrowerId, Is.EqualTo("B1"));
    }
}
