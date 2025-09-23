using System.Net.Http.Headers;
using System.Net.Http.Json;
using LoanWorkflow.Api.Features.Auth;
using LoanWorkflow.Api.Features.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace LoanWorkflow.Tests;

public class LoanRequestInitiationNegativeAuthorizationTests
{
    private TestWebApplicationFactory _factory = null!;

    [SetUp]
    public void SetUp() => _factory = new TestWebApplicationFactory();

    [TearDown]
    public void TearDown() => _factory.Dispose();

    [Test]
    public async Task NonFtUser_Cannot_Initiate_Loan()
    {
        var client = _factory.CreateClient();
        var nonFt = LoanWorkflow.Shared.Workflow.MockUsers.Users.First(u=>u.Role!="FT");
        var loginResp = await client.PostAsJsonAsync("/api/auth/login", new { email = nonFt.Email, password = nonFt.Password});
        loginResp.EnsureSuccessStatusCode();
        var payload = await loginResp.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
    NUnit.Framework.Assert.That(payload?.Data, Is.Not.Null);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload!.Data!.AccessToken);
        var createResp = await client.PostAsJsonAsync("/api/loanrequests/standard", new { customerEmail = "cust2@example.com", amount = 500m, loanType = "standard", tenorMonths = 6, currency = "USD", fullName = "Customer Two" });
        NUnit.Framework.Assert.That(createResp.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Forbidden));
    }
}
