using LoanWorkflow.Workers.Conductor;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ConductorClient>();
builder.Services.AddSingleton<WorkerPoller>();
builder.Services.AddHostedService<WorkerHost>();
var host = builder.Build();
await host.RunAsync();

public class WorkerHost : IHostedService
{
	private readonly WorkerPoller _poller; private readonly ILogger<WorkerHost> _logger; private CancellationTokenSource? _cts;
	public WorkerHost(WorkerPoller poller, ILogger<WorkerHost> logger){ _poller = poller; _logger = logger; }
	public Task StartAsync(CancellationToken cancellationToken){ _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken); _logger.LogInformation("WorkerHost starting"); return _poller.StartAsync(_cts.Token); }
	public Task StopAsync(CancellationToken cancellationToken){ _logger.LogInformation("WorkerHost stopping"); _cts?.Cancel(); return Task.CompletedTask; }
}
