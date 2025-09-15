using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using LoanWorkflow.Api.Data;
using LoanWorkflow.Api.Repositories;
using LoanWorkflow.Shared.Domain;
using Moq;

namespace LoanWorkflow.Tests;

[TestFixture]
public class UnitOfWorkTests
{
    private LoanWorkflowDbContext _db = null!;

    [SetUp]
    public void Setup()
    {
        var opts = new DbContextOptionsBuilder<LoanWorkflowDbContext>()
            .UseInMemoryDatabase("uow_tests")
            .Options;
        _db = new LoanWorkflowDbContext(opts);
    }

    [TearDown]
    public void Teardown()
    {
        _db.Dispose();
    }

    [Test]
    public async Task UnitOfWork_SaveChanges_Persists()
    {
        var lrRepo = new LoanRequestRepository(_db);
        var logRepo = new LoanRequestLogRepository(_db);
        var sp = new Mock<IServiceProvider>().Object;
        var uow = new UnitOfWork(_db, sp, lrRepo, logRepo);
    var req = new LoanRequest { BorrowerId = "B2", Amount = 10, LoanType = "standard", FullName = "User Two", IsEligible = false, CurrentStage = "FT", StageIndex = 0, Status = LoanRequestStatus.InProgress };
        await uow.LoanRequests.InsertAsync(req);
        var changes = await uow.SaveChangesAsync();
        Assert.That(changes, Is.GreaterThanOrEqualTo(1));
        var fetched = await uow.LoanRequests.GetByIdAsync(req.Id);
        Assert.That(fetched, Is.Not.Null);
    }

    [Test]
    public void Repository_Generic_Cache_DifferentInstancesPerType()
    {
        var lrRepo = new LoanRequestRepository(_db);
        var logRepo = new LoanRequestLogRepository(_db);
        var sp = new Mock<IServiceProvider>().Object;
        var uow = new UnitOfWork(_db, sp, lrRepo, logRepo);
        var r1 = uow.Repository<LoanRequest>();
        var r2 = uow.Repository<LoanRequest>();
        Assert.That(r1, Is.SameAs(r2)); // cached
    }
}
