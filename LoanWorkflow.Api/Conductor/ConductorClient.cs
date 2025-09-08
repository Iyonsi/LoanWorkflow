using System.Text;
using System.Text.Json;

namespace LoanWorkflow.Api.Conductor;

public interface IWorkflowStarter
{
    Task<string?> StartWorkflowAsync(string name, int version, object input);
}

public class ConductorClient : IWorkflowStarter
{
    private readonly HttpClient _http;
    public ConductorClient(IConfiguration cfg, IHttpClientFactory factory)
    {
        _http = factory.CreateClient("conductor");
        var baseUrl = cfg["Conductor:BaseUrl"] ?? string.Empty;
        if(!string.IsNullOrWhiteSpace(baseUrl)) _http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        var apiKey = cfg["Conductor:ApiKey"];
        var apiSecret = cfg["Conductor:ApiSecret"];
        if(!string.IsNullOrEmpty(apiKey))
        {
            _http.DefaultRequestHeaders.Add("X-API-KEY", apiKey);
            _http.DefaultRequestHeaders.Add("X-API-SECRET", apiSecret ?? "");
        }
    }

    public async Task<bool> RegisterWorkflowAsync(string workflowJson)
    {
        var content = new StringContent(workflowJson, Encoding.UTF8, "application/json");
        var resp = await _http.PostAsync("metadata/workflow", content);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> RegisterTasksAsync(string tasksJson)
    {
        var content = new StringContent(tasksJson, Encoding.UTF8, "application/json");
        var resp = await _http.PostAsync("metadata/taskdefs", content);
        return resp.IsSuccessStatusCode;
    }

    public async Task<string?> StartWorkflowAsync(string name, int version, object input)
    {
        var payload = JsonSerializer.Serialize(new { name, version, input });
        var resp = await _http.PostAsync("workflow", new StringContent(payload, Encoding.UTF8, "application/json"));
        if(!resp.IsSuccessStatusCode) return null;
        var txt = await resp.Content.ReadAsStringAsync();
        return txt.Trim('"');
    }
}
