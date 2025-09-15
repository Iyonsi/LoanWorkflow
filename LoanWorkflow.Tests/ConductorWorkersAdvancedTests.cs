using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using LoanWorkflow.Workers.Conductor;

namespace LoanWorkflow.Tests;

class SequenceHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new();
    public void Enqueue(HttpResponseMessage resp) => _responses.Enqueue(resp);
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(_responses.Count>0 ? _responses.Dequeue() : new HttpResponseMessage(HttpStatusCode.NotFound));
}

[TestFixture]
public class ConductorWorkersAdvancedTests
{
    [Test]
    public async Task Token_Ack_Update_Succeeds()
    {
        var handler = new SequenceHandler();
        // Token response
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK){ Content = new StringContent("{\"token\":\"abc123\",\"expiryTime\":9999999999999}")});
        // Ack response
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK));
        // Update response
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK));
        var http = new HttpClient(handler){ BaseAddress = new Uri("http://wrk/") };
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{
            {"Conductor:BaseUrl","http://wrk"},
            {"Conductor:ApiKey","k"},
            {"Conductor:ApiSecret","s"}
        }).Build();
        var client = new ConductorClient(http, cfg);
        await client.AckAsync("t1","w1");
        await client.UpdateTaskAsync(new ConductorTaskResult{ TaskId="t1", WorkflowInstanceId="wf1"});
        Assert.Pass();
    }

    [Test]
    public void Token_Failure_Throws()
    {
        var handler = new SequenceHandler();
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.BadRequest){ Content = new StringContent("bad")});
        var http = new HttpClient(handler){ BaseAddress = new Uri("http://wrk/") };
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{
            {"Conductor:BaseUrl","http://wrk"}, {"Conductor:ApiKey","k"}, {"Conductor:ApiSecret","s"}
        }).Build();
        var client = new ConductorClient(http, cfg);
        Assert.ThrowsAsync<InvalidOperationException>(() => client.AckAsync("t1","w1"));
    }
}
