using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using LoanWorkflow.Api.Services;
using LoanWorkflow.Api.Repositories;
using LoanWorkflow.Shared.DTOs;
using LoanWorkflow.Shared.Domain;
using LoanWorkflow.Api.Conductor;
using Microsoft.Extensions.Configuration;

namespace LoanWorkflow.Tests;

[TestFixture]
public class LoanRequestServiceTests
{
    private Mock<IUnitOfWork> _uow = null!;
    private Mock<ILoanRequestRepository> _loanRepo = null!;
    private Mock<ILoanRequestLogRepository> _logRepo = null!;
    private Mock<IWorkflowStarter> _workflow = null!;
    private LoanRequestService _svc = null!;
    private Mock<Microsoft.Extensions.Logging.ILogger<LoanRequestService>> _logger = null!;

    [SetUp]
    public void Setup()
    {
        _uow = new Mock<IUnitOfWork>();
        _loanRepo = new Mock<ILoanRequestRepository>();
        _logRepo = new Mock<ILoanRequestLogRepository>();
    _workflow = new Mock<IWorkflowStarter>();
    _logger = new Mock<Microsoft.Extensions.Logging.ILogger<LoanRequestService>>();
        _uow.Setup(x => x.LoanRequests).Returns(_loanRepo.Object);
        _uow.Setup(x => x.LoanRequestLogs).Returns(_logRepo.Object);
        _uow.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _workflow.Setup(w => w.StartWorkflowAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<object>()))
            .ReturnsAsync("wf123");
        var inMemorySettings = new Dictionary<string,string?> { { "Conductor:Enabled", "false" } };
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings!).Build();
    _svc = new LoanRequestService(_uow.Object, _workflow.Object, _logger.Object, cfg);
    }

    [Test]
    public async Task CreateAsync_SetsInitialStage_AndStartsWorkflow()
    {
    var dto = new CreateLoanRequestDto { Amount = 1000m, BorrowerId = Guid.NewGuid().ToString(), FullName = "Tester", IsEligible = true };
        _loanRepo.Setup(r => r.InsertAsync(It.IsAny<LoanRequest>()))
            .Returns(Task.CompletedTask);
        _logRepo.Setup(l => l.InsertAsync(It.IsAny<LoanRequestLog>())).Returns(Task.CompletedTask);

    var (req, wfId) = await _svc.CreateAsync(dto, "standard");
    Assert.That(req, Is.Not.Null);
    Assert.That(req.LoanType, Is.EqualTo("standard"));
        Assert.That(req.StageIndex, Is.EqualTo(0));
        Assert.That(req.CurrentStage, Is.EqualTo("FT"));
        Assert.That(wfId, Is.EqualTo("wf123"));
        _workflow.Verify(w => w.StartWorkflowAsync("loan_dynamic_workflow", 1, It.IsAny<object>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Test]
    public void CreateAsync_InvalidFlow_Throws()
    {
    var dto = new CreateLoanRequestDto { Amount = 10m, BorrowerId = Guid.NewGuid().ToString(), FullName = "Bad", IsEligible = false };
    Assert.ThrowsAsync<ArgumentException>(() => _svc.CreateAsync(dto, "invalid_type"));
    }

    [Test]
    public async Task ApproveAsync_FirstApproval_WritesLog()
    {
    var req = new LoanRequest { LoanType = "standard", Amount = 10, BorrowerId = Guid.NewGuid().ToString(), StageIndex = 0, CurrentStage = "FT", Status = LoanRequestStatus.InProgress };
        _loanRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(req);
        _logRepo.Setup(l => l.CountStageDecisionAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(0);
        _logRepo.Setup(l => l.InsertAsync(It.IsAny<LoanRequestLog>())).Returns(Task.CompletedTask);
        var result = await _svc.ApproveAsync(req.Id, new DecisionDto{ ActorUserId = Guid.NewGuid().ToString(), Approved = true, Stage = "FT", RequestId = req.Id });
        Assert.That(result.Approved, Is.True);
        Assert.That(result.Stage, Is.EqualTo("FT"));
    }

    [Test]
    public void ApproveAsync_Duplicate_Throws()
    {
    var req = new LoanRequest { LoanType = "standard", Amount = 10, BorrowerId = Guid.NewGuid().ToString(), StageIndex = 0, CurrentStage = "FT", Status = LoanRequestStatus.InProgress };
        _loanRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(req);
        _logRepo.Setup(l => l.CountStageDecisionAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(1);
        Assert.ThrowsAsync<InvalidOperationException>(() => _svc.ApproveAsync(req.Id, new DecisionDto{ ActorUserId = Guid.NewGuid().ToString(), Approved = true, Stage = "FT", RequestId = req.Id }));
    }

    [Test]
    public async Task RejectAsync_Flow3_RecordsLog()
    {
    var req = new LoanRequest { LoanType = "flex_review", Amount = 10, BorrowerId = Guid.NewGuid().ToString(), StageIndex = 1, CurrentStage = "HOP", Status = LoanRequestStatus.InProgress };
        _loanRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(req);
        _logRepo.Setup(l => l.CountStageDecisionAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(0);
        _logRepo.Setup(l => l.InsertAsync(It.IsAny<LoanRequestLog>())).Returns(Task.CompletedTask);
        var result = await _svc.RejectAsync(req.Id, new DecisionDto{ ActorUserId = Guid.NewGuid().ToString(), Approved = false, Stage = "HOP", RequestId = req.Id });
    Assert.That(result.Approved, Is.False);
        Assert.That(result.Stage, Is.EqualTo("HOP"));
    }

    [Test]
    public void RejectAsync_Flow2_Throws()
    {
    var req = new LoanRequest { LoanType = "multi_stage", Amount = 10, BorrowerId = Guid.NewGuid().ToString(), StageIndex = 2, CurrentStage = "BRANCH", Status = LoanRequestStatus.InProgress };
        _loanRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(req);
        Assert.ThrowsAsync<InvalidOperationException>(() => _svc.RejectAsync(req.Id, new DecisionDto{ ActorUserId = Guid.NewGuid().ToString(), Approved = false, Stage = "BRANCH", RequestId = req.Id }));
    }
}

