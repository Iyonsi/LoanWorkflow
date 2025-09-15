using System.Net.Http.Json;
using NUnit.Framework;
using LoanWorkflow.Shared.DTOs;
using LoanWorkflow.Api.Features.Common;
using LoanWorkflow.Api.Features.Constants;

namespace LoanWorkflow.Tests;

[TestFixture]
public class LoanRequestsControllerIntegrationTests
{
    private TestWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new TestWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void Teardown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task CreateStandard_ReturnsCreated()
    {
        var dto = new CreateLoanRequestDto{ Amount=500, BorrowerId=Guid.NewGuid().ToString(), FullName="Int User", IsEligible=true };
        var resp = await _client.PostAsJsonAsync("/api/loanrequests/standard", dto);
        Assert.That(resp.IsSuccessStatusCode, Is.True);
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.That(body, Is.Not.Null);
        Assert.That(body!.ResponseCode, Is.EqualTo(ResponseCodes.CREATED));
    }

    [Test]
    public async Task CreateStandard_Invalid_ReturnsValidation()
    {
        var dto = new CreateLoanRequestDto{ Amount=0, BorrowerId="", FullName="" };
        var resp = await _client.PostAsJsonAsync("/api/loanrequests/standard", dto);
        Assert.That(resp.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.BadRequest));
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.That(body!.ResponseCode, Is.EqualTo(ResponseCodes.VALIDATION_ERROR));
    }

    [Test]
    public async Task Get_NotFound_Returns404()
    {
        var resp = await _client.GetAsync("/api/loanrequests/does-not-exist");
        Assert.That(resp.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.That(body!.ResponseCode, Is.EqualTo(ResponseCodes.NOT_FOUND));
    }

    [Test]
    public async Task Search_Pagination_MetaPresent()
    {
        // create two
        for(int i=0;i<2;i++)
        {
            var dto = new CreateLoanRequestDto{ Amount=100+i, BorrowerId=Guid.NewGuid().ToString(), FullName=$"User{i}", IsEligible=true };
            await _client.PostAsJsonAsync("/api/loanrequests/standard", dto);
        }
        var resp = await _client.GetAsync("/api/loanrequests?page=1&pageSize=1");
        Assert.That(resp.IsSuccessStatusCode, Is.True);
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<List<object>>>();
        Assert.That(body!.Pagination, Is.Not.Null);
        Assert.That(body.Pagination!.TotalCount, Is.GreaterThanOrEqualTo(2));
        Assert.That(body.Pagination.TotalPages, Is.GreaterThanOrEqualTo(2));
    }
}
