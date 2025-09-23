using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using LoanWorkflow.Api.Conductor;

namespace LoanWorkflow.Tests;

class FakeHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage,HttpResponseMessage> _handler;
    public FakeHandler(Func<HttpRequestMessage,HttpResponseMessage> handler){ _handler = handler; }
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(_handler(request));
}

[TestFixture]
public class ConductorClientTests
{
    [Test]
    public async Task StartWorkflowAsync_ReturnsId()
    {
        // Respond with token first then workflow id
        var handler = new FakeHandler(req =>
        {
            if (req.RequestUri!.AbsolutePath.EndsWith("/token"))
                return new HttpResponseMessage(HttpStatusCode.OK){ Content = new StringContent("{\"token\":\"t123\",\"expiryTime\":9999999999999}")};
            return new HttpResponseMessage(HttpStatusCode.OK){ Content = new StringContent("\"wf-abc\"")};
        });
        var http = new HttpClient(handler){ BaseAddress = new Uri("http://localhost/") };
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{
            {"Conductor:BaseUrl","http://localhost"},
            {"Conductor:ApiKey","k"},
            {"Conductor:ApiSecret","s"}
        }).Build();
        var client = new ConductorClient(cfg, new TestFactory(http));
        var id = await client.StartWorkflowAsync("loan_dynamic_workflow",1,new { x=1 });
        Assert.That(id, Is.EqualTo("wf-abc"));
    }

    private class TestFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;
        public TestFactory(HttpClient c){ _client = c; }
        public HttpClient CreateClient(string name) => _client;
    }
}
