using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LoanWorkflow.Api.Features.Auth;
using LoanWorkflow.Api.Features.Common;
using NUnit.Framework;

namespace LoanWorkflow.Tests;

public class ApprovalAuthorizationNegativeTests
{
    private TestWebApplicationFactory _factory = null!;

    [SetUp]
    public void SetUp() => _factory = new TestWebApplicationFactory();

    [TearDown]
    public void TearDown() => _factory.Dispose();

    [Test]
    public async Task FtRole_Cannot_Approve()
    {
        var client = _factory.CreateClient();
        var ft = LoanWorkflow.Shared.Workflow.MockUsers.Users.First(u=>u.Role == "FT");
        var loginResp = await client.PostAsJsonAsync("/api/auth/login", new { email = ft.Email, password = ft.Password});
        loginResp.EnsureSuccessStatusCode();
        var login = await loginResp.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Data!.AccessToken);
        var approveResp = await client.PostAsJsonAsync($"/api/loan-requests/{Guid.NewGuid()}/approvals/approve", new { stageDecision = "APPROVE", comments = "no" });
        NUnit.Framework.Assert.That(approveResp.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }
}
