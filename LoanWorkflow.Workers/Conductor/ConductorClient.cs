using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace LoanWorkflow.Workers.Conductor;

public class ConductorClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _apiSecret;
    public ConductorClient(HttpClient http, IConfiguration cfg)
    {
        _http = http;
        _apiKey = cfg["Conductor:ApiKey"] ?? "";
        _apiSecret = cfg["Conductor:ApiSecret"] ?? "";
        var baseUrl = cfg["Conductor:BaseUrl"] ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            _http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        }
        if(!string.IsNullOrEmpty(_apiKey))
        {
            _http.DefaultRequestHeaders.Add("X-API-KEY", _apiKey);
            _http.DefaultRequestHeaders.Add("X-API-SECRET", _apiSecret);
        }
    }

    public async Task<TaskPollResult?> PollAsync(string taskType, string workerId, int timeoutMs = 10000)
    {
        var resp = await _http.GetAsync($"tasks/poll/{taskType}?workerid={workerId}&domain=");
        if(!resp.IsSuccessStatusCode) return null;
        var json = await resp.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(json)) return null;
        return JsonSerializer.Deserialize<TaskPollResult>(json, new JsonSerializerOptions{PropertyNameCaseInsensitive=true});
    }

    public async Task AckAsync(string taskId, string workerId)
    {
        var content = new StringContent(JsonSerializer.Serialize(new { workerId }), Encoding.UTF8, "application/json");
        await _http.PostAsync($"tasks/{taskId}/ack", content);
    }

    public async Task UpdateTaskAsync(ConductorTaskResult result)
    {
        var content = new StringContent(JsonSerializer.Serialize(result), Encoding.UTF8, "application/json");
        await _http.PostAsync("tasks", content);
    }
}

public sealed record TaskPollResult(string TaskId, string TaskType, Dictionary<string,object>? InputData, string WorkflowInstanceId, int CallbackAfterSeconds);

public sealed class ConductorTaskResult
{
    public string TaskId { get; set; } = string.Empty;
    public string WorkflowInstanceId { get; set; } = string.Empty;
    public string ReasonForIncompletion { get; set; } = string.Empty;
    public string Status { get; set; } = "COMPLETED"; // or FAILED, IN_PROGRESS
    public Dictionary<string,object> OutputData { get; set; } = new();
    public int CallbackAfterSeconds { get; set; } = 0;
}
