using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LoanWorkflow.Api.Features.Auth;
using LoanWorkflow.Api.Features.Common;
using NUnit.Framework;

namespace LoanWorkflow.Tests;

public class ApprovalAuthorizationTests
{
    private TestWebApplicationFactory _factory = null!;

    [SetUp]
    public void SetUp() => _factory = new TestWebApplicationFactory();

    [TearDown]
    public void TearDown() => _factory.Dispose();

    [Test]
    public async Task NonFtRole_Can_Attempt_Approve()
    {
        var client = _factory.CreateClient();
        var approver = LoanWorkflow.Shared.Workflow.MockUsers.Users.First(u=>u.Role != "FT");
        var loginResp = await client.PostAsJsonAsync("/api/auth/login", new { email = approver.Email, password = approver.Password});
        loginResp.EnsureSuccessStatusCode();
        var login = await loginResp.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Data!.AccessToken);
        // Using a random id will likely yield NotFound, which is fine; just ensure not Forbidden
        var approveResp = await client.PostAsJsonAsync($"/api/loan-requests/{Guid.NewGuid()}/approvals/approve", new { stageDecision = "APPROVE", comments = "ok" });
        NUnit.Framework.Assert.That(approveResp.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden));
    }
}
