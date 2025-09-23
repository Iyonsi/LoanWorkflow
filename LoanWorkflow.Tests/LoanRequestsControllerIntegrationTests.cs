using System.Net.Http.Json;
using NUnit.Framework;
using LoanWorkflow.Shared.DTOs;
using LoanWorkflow.Api.Features.Common;
using LoanWorkflow.Api.Features.Constants;
using System.Net.Http.Headers;
using LoanWorkflow.Api.Features.Auth;
using System.Threading.Tasks;

namespace LoanWorkflow.Tests;

[TestFixture]
public partial class LoanRequestsControllerIntegrationTests
{
    private TestWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new TestWebApplicationFactory();
        _client = _factory.CreateClient();
        TestAuthHelper.AuthenticateAsFtAsync(_client).GetAwaiter().GetResult();
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
        var bodyText = await resp.Content.ReadAsStringAsync();
        if(!resp.IsSuccessStatusCode)
        {
            Console.WriteLine("CreateStandard_ReturnsCreated failure status: " + resp.StatusCode);
            Console.WriteLine("Authorization header: " + (_client.DefaultRequestHeaders.Authorization?.Parameter?.Substring(0,20) + "..."));
            Console.WriteLine("Body: " + bodyText);
        }
        Assert.That(resp.IsSuccessStatusCode, Is.True, $"Expected success but got {(int)resp.StatusCode} Body: {bodyText}");
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.That(body, Is.Not.Null);
        Assert.That(body!.ResponseCode, Is.EqualTo(ResponseCodes.CREATED));
    }

    [Test]
    public async Task CreateStandard_Invalid_ReturnsValidation()
    {
        var dto = new CreateLoanRequestDto{ Amount=0, BorrowerId="", FullName="" };
        var resp = await _client.PostAsJsonAsync("/api/loanrequests/standard", dto);
        var bodyText = await resp.Content.ReadAsStringAsync();
        if(resp.StatusCode != System.Net.HttpStatusCode.BadRequest)
        {
            Console.WriteLine("CreateStandard_Invalid status: " + resp.StatusCode);
            Console.WriteLine("Authorization header: " + (_client.DefaultRequestHeaders.Authorization?.Parameter?.Substring(0,20) + "..."));
            Console.WriteLine("Body: " + bodyText);
        }
        Assert.That(resp.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.BadRequest), $"Expected 400 validation but got {(int)resp.StatusCode} Body: {bodyText}");
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.That(body!.ResponseCode, Is.EqualTo(ResponseCodes.VALIDATION_ERROR));
    }

    [Test]
    public async Task Get_NotFound_Returns404()
    {
        var resp = await _client.GetAsync("/api/loanrequests/does-not-exist");
        var bodyText = await resp.Content.ReadAsStringAsync();
        if(resp.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            Console.WriteLine("Get_NotFound status: " + resp.StatusCode);
            Console.WriteLine("Authorization header: " + (_client.DefaultRequestHeaders.Authorization?.Parameter?.Substring(0,20) + "..."));
            Console.WriteLine("Body: " + bodyText);
        }
        Assert.That(resp.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound), $"Expected 404 but got {(int)resp.StatusCode} Body: {bodyText}");
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
        var bodyText = await resp.Content.ReadAsStringAsync();
        if(!resp.IsSuccessStatusCode)
        {
            Console.WriteLine("Search_Pagination status: " + resp.StatusCode);
            Console.WriteLine("Authorization header: " + (_client.DefaultRequestHeaders.Authorization?.Parameter?.Substring(0,20) + "..."));
            Console.WriteLine("Body: " + bodyText);
        }
        Assert.That(resp.IsSuccessStatusCode, Is.True, $"Expected success but got {(int)resp.StatusCode} Body: {bodyText}");
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<List<object>>>();
        Assert.That(body!.Pagination, Is.Not.Null);
        Assert.That(body.Pagination!.TotalCount, Is.GreaterThanOrEqualTo(2));
        Assert.That(body.Pagination.TotalPages, Is.GreaterThanOrEqualTo(2));
    }
}

// helper methods
public partial class LoanRequestsControllerIntegrationTests { }
