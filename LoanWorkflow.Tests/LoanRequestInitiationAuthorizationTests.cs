using System.Net.Http.Json;
using LoanWorkflow.Api.Features.Auth;
using LoanWorkflow.Api.Features.Common;
using LoanWorkflow.Shared.DTOs;
using NUnit.Framework;

namespace LoanWorkflow.Tests;

public class LoanRequestInitiationAuthorizationTests
{
    private TestWebApplicationFactory _factory = null!;

    [SetUp]
    public void SetUp() => _factory = new TestWebApplicationFactory();

    [TearDown]
    public void TearDown() => _factory.Dispose();

    [Test]
    public async Task FtUser_Can_Initiate_Loan()
    {
        var client = _factory.CreateClient();
        await TestAuthHelper.AuthenticateAsFtAsync(client);
        var dto = new CreateLoanRequestDto
        {
            Amount = 1000m,
            BorrowerId = Guid.NewGuid().ToString(),
            FullName = "Customer One",
            IsEligible = true
        };
        var resp = await client.PostAsJsonAsync("/api/loanrequests/standard", dto);
        if(!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            Assert.Fail($"Expected success but got {(int)resp.StatusCode}. Body: {body}");
        }
        Assert.Pass();
    }
}
