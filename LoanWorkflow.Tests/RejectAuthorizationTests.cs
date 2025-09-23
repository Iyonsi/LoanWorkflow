using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LoanWorkflow.Api.Features.Auth;
using LoanWorkflow.Api.Features.Common;
using NUnit.Framework;

namespace LoanWorkflow.Tests;

public class RejectAuthorizationTests
{
    private TestWebApplicationFactory _factory = null!;

    [SetUp]
    public void SetUp() => _factory = new TestWebApplicationFactory();

    [TearDown]
    public void TearDown() => _factory.Dispose();

    [Test]
    public async Task NonFtRole_Can_Attempt_Reject()
    {
        var client = _factory.CreateClient();
        var approver = LoanWorkflow.Shared.Workflow.MockUsers.Users.First(u=>u.Role != "FT");
        var loginResp = await client.PostAsJsonAsync("/api/auth/login", new { email = approver.Email, password = approver.Password});
        loginResp.EnsureSuccessStatusCode();
        var login = await loginResp.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Data!.AccessToken);
        var rejectResp = await client.PostAsJsonAsync($"/api/loan-requests/{Guid.NewGuid()}/approvals/reject", new { stageDecision = "REJECT", comments = "no" });
    NUnit.Framework.Assert.That(rejectResp.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task FtRole_Cannot_Reject()
    {
        var client = _factory.CreateClient();
        var ft = LoanWorkflow.Shared.Workflow.MockUsers.Users.First(u=>u.Role == "FT");
        var loginResp = await client.PostAsJsonAsync("/api/auth/login", new { email = ft.Email, password = ft.Password});
        loginResp.EnsureSuccessStatusCode();
        var login = await loginResp.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Data!.AccessToken);
        var rejectResp = await client.PostAsJsonAsync($"/api/loan-requests/{Guid.NewGuid()}/approvals/reject", new { stageDecision = "REJECT", comments = "bad" });
        NUnit.Framework.Assert.That(rejectResp.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }
}
