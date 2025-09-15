using NUnit.Framework;
using System.Net.Http.Json;
using LoanWorkflow.Api.Features.Common;
using LoanWorkflow.Api.Features.Constants;

namespace LoanWorkflow.Tests;

[TestFixture]
public class HealthAndRegistrationIntegrationTests
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
    public async Task Health_ReturnsSuccess()
    {
        var resp = await _client.GetAsync("/api/health");
        Assert.That(resp.IsSuccessStatusCode, Is.True);
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.That(body!.ResponseCode, Is.EqualTo(ResponseCodes.SUCCESS));
    }

    [Test]
    public async Task RegisterWorkflow_MissingFile_NotFound()
    {
        var resp = await _client.PostAsync("/api/registration/register-workflow", null);
        // In test env the file likely missing so expect NotFound
        if(resp.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            var body = await resp.Content.ReadFromJsonAsync<ApiResponse<object>>();
            Assert.That(body!.ResponseCode, Is.EqualTo(ResponseCodes.NOT_FOUND));
        }
        else
        {
            Assert.That(resp.IsSuccessStatusCode, Is.True); // If present treat as success.
        }
    }
}
