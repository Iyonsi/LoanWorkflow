using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using LoanWorkflow.Api.Services.ApplicationServices;
using LoanWorkflow.Api.Services;
using LoanWorkflow.Shared.DTOs;
using LoanWorkflow.Shared.Domain;
using LoanWorkflow.Api.Features.Common;
using LoanWorkflow.Api.Features.Constants;

namespace LoanWorkflow.Tests;

[TestFixture]
public class ApplicationServiceTests
{
    private Mock<ILoanRequestService> _domain = null!;
    private ILoanRequestApplicationService _app = null!;

    [SetUp]
    public void Setup()
    {
        _domain = new Mock<ILoanRequestService>();
        _app = new LoanRequestApplicationService(_domain.Object);
    }

    [Test]
    public async Task CreateAsync_ReturnsCreatedCode()
    {
        var dto = new CreateLoanRequestDto{ Amount=10, BorrowerId=Guid.NewGuid().ToString(), FullName="User", IsEligible=true };
        var request = new LoanRequest{ Id="R1", LoanType="standard" };
        _domain.Setup(d => d.CreateAsync(dto, "standard")).ReturnsAsync((request, "wf1"));
        var resp = await _app.CreateAsync(dto, "standard", "trace-1");
        Assert.That(resp.ResponseCode, Is.EqualTo(ResponseCodes.CREATED));
        Assert.That(resp.Data, Is.Not.Null);
    }

    [Test]
    public async Task SearchAsync_Paginates()
    {
        var items = new List<LoanRequest>{
            new LoanRequest{ Id="1", LoanType="standard", CreatedAt=DateTime.UtcNow },
            new LoanRequest{ Id="2", LoanType="standard", CreatedAt=DateTime.UtcNow }
        };
        _domain.Setup(d => d.SearchAsync(It.IsAny<LoanRequestSearchQuery>(), default)).ReturnsAsync((items.AsEnumerable(), (long)items.Count));
        var resp = await _app.SearchAsync(null, null, "standard", null, null, null, null, 1, 1, "t1");
        Assert.That(resp.Pagination, Is.Not.Null);
        Assert.That(resp.Pagination!.TotalCount, Is.EqualTo(2));
        Assert.That(resp.Pagination.PageSize, Is.EqualTo(1));
        Assert.That(resp.Pagination.TotalPages, Is.EqualTo(2));
    }
}
