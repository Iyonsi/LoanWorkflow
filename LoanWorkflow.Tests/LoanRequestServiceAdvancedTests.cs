using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using LoanWorkflow.Api.Services;
using LoanWorkflow.Api.Repositories;
using LoanWorkflow.Shared.DTOs;
using LoanWorkflow.Shared.Domain;
using LoanWorkflow.Api.Conductor;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace LoanWorkflow.Tests;

[TestFixture]
public class LoanRequestServiceAdvancedTests
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
        _workflow.Setup(w => w.StartWorkflowAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<object>()));
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{ {"Conductor:Enabled","false"} }).Build();
        _svc = new LoanRequestService(_uow.Object, _workflow.Object, _logger.Object, cfg);
    }

    [Test]
    public async Task Approve_FinalStage_SetsApprovedStatus()
    {
        var req = new LoanRequest { LoanType = "standard", StageIndex = 3, CurrentStage = "ED", Status = LoanRequestStatus.InProgress };
        _loanRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(req);
        _logRepo.Setup(l => l.CountStageDecisionAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(0);
        _logRepo.Setup(l => l.InsertAsync(It.IsAny<LoanRequestLog>())).Returns(Task.CompletedTask);
        var result = await _svc.ApproveAsync(req.Id, new DecisionDto{ ActorUserId = Guid.NewGuid().ToString(), Approved = true, Stage = "ED", RequestId = req.Id });
        Assert.That(result.Approved, Is.True);
        Assert.That(req.Status, Is.EqualTo(LoanRequestStatus.Approved));
    }

    [Test]
    public async Task Reject_Standard_ResetsToFirstStage()
    {
        var req = new LoanRequest { LoanType = "standard", StageIndex = 2, CurrentStage = "ZONAL_HEAD", Status = LoanRequestStatus.InProgress };
        _loanRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(req);
        _logRepo.Setup(l => l.CountStageDecisionAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(0);
        _logRepo.Setup(l => l.InsertAsync(It.IsAny<LoanRequestLog>())).Returns(Task.CompletedTask);
        var result = await _svc.RejectAsync(req.Id, new DecisionDto{ ActorUserId = Guid.NewGuid().ToString(), Approved = false, Stage = "ZONAL_HEAD", RequestId = req.Id });
        Assert.That(result.Approved, Is.False);
        Assert.That(req.StageIndex, Is.EqualTo(0));
        Assert.That(req.CurrentStage, Is.EqualTo("FT"));
    }

    [Test]
    public async Task Reject_FlexReview_StepsBackOne()
    {
        var req = new LoanRequest { LoanType = "flex_review", StageIndex = 2, CurrentStage = "FIN_INT_CTRL", Status = LoanRequestStatus.InProgress };
        _loanRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(req);
        _logRepo.Setup(l => l.CountStageDecisionAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(0);
        _logRepo.Setup(l => l.InsertAsync(It.IsAny<LoanRequestLog>())).Returns(Task.CompletedTask);
        var result = await _svc.RejectAsync(req.Id, new DecisionDto{ ActorUserId = Guid.NewGuid().ToString(), Approved = false, Stage = "FIN_INT_CTRL", RequestId = req.Id });
        Assert.That(result.Approved, Is.False);
        Assert.That(req.StageIndex, Is.EqualTo(1));
    }

    [Test]
    public void Reject_MultiStage_Throws()
    {
        var req = new LoanRequest { LoanType = "multi_stage", StageIndex = 1, CurrentStage = "HOP", Status = LoanRequestStatus.InProgress };
        _loanRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(req);
        Assert.ThrowsAsync<InvalidOperationException>(() => _svc.RejectAsync(req.Id, new DecisionDto{ ActorUserId = Guid.NewGuid().ToString(), Approved = false, Stage = "HOP", RequestId = req.Id }));
    }

    [Test]
    public async Task Search_FilterByLoanType_ReturnsOnlyMatches()
    {
        var data = new List<LoanRequest>{
            new LoanRequest{ LoanType = "standard", CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
            new LoanRequest{ LoanType = "flex_review", CreatedAt = DateTime.UtcNow.AddMinutes(-5) }
        };
        _loanRepo.Setup(r => r.Query()).Returns(data.AsQueryable());
        var (items,total) = await _svc.SearchAsync(new LoanRequestSearchQuery{ LoanType = "standard", Page=1, PageSize=10 });
        Assert.That(total, Is.EqualTo(1));
        Assert.That(items.First().LoanType, Is.EqualTo("standard"));
    }
}
