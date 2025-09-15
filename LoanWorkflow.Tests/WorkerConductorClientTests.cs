using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using LoanWorkflow.Workers.Conductor;

namespace LoanWorkflow.Tests;

class FakeHandler2 : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new();
    public void Enqueue(HttpResponseMessage resp) => _responses.Enqueue(resp);
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if(_responses.Count==0) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        return Task.FromResult(_responses.Dequeue());
    }
}

[TestFixture]
public class WorkerConductorClientTests
{
    [Test]
    public async Task PollAsync_DeserializesTask()
    {
        var handler = new FakeHandler2();
        var pollObj = new TaskPollResult("t1","fetch_decision", new Dictionary<string,object>{{"requestId","R1"}}, "wf1", 0);
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK){ Content = new StringContent(JsonSerializer.Serialize(pollObj))});
    var http = new HttpClient(handler){ BaseAddress = new Uri("http://local/") };
    var inMem = new Dictionary<string,string?>{ {"Conductor:BaseUrl", "http://local" } };
    var cfg = new ConfigurationBuilder().AddInMemoryCollection(inMem!).Build();
        var client = new ConductorClient(http, cfg);
        var polled = await client.PollAsync("fetch_decision","w1");
        Assert.That(polled, Is.Not.Null);
        Assert.That(polled!.TaskId, Is.EqualTo("t1"));
    }
}
