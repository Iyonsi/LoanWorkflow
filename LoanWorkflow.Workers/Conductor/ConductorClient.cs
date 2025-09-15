using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace LoanWorkflow.Workers.Conductor;

public class ConductorClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _apiSecret;
    private readonly string _baseUrl;
    private string? _bearerToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    public ConductorClient(HttpClient http, IConfiguration cfg)
    {
        _http = http;
        _apiKey = cfg["Conductor:ApiKey"] ?? "";
        _apiSecret = cfg["Conductor:ApiSecret"] ?? "";
        _baseUrl = (cfg["Conductor:BaseUrl"] ?? string.Empty).TrimEnd('/') + "/";
        if (!string.IsNullOrWhiteSpace(_baseUrl))
        {
            _http.BaseAddress = new Uri(_baseUrl);
        }
    }

    public async Task<TaskPollResult?> PollAsync(string taskType, string workerId, int timeoutMs = 10000)
    {
        await EnsureTokenAsync();
        var req = new HttpRequestMessage(HttpMethod.Get, $"tasks/poll/{taskType}?workerid={workerId}&domain=");
        if (!string.IsNullOrEmpty(_bearerToken)) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
        var resp = await _http.SendAsync(req);
        if(!resp.IsSuccessStatusCode) return null;
        var json = await resp.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(json)) return null;
        return JsonSerializer.Deserialize<TaskPollResult>(json, new JsonSerializerOptions{PropertyNameCaseInsensitive=true});
    }

    public async Task AckAsync(string taskId, string workerId)
    {
        await EnsureTokenAsync();
        var content = new StringContent(JsonSerializer.Serialize(new { workerId }), Encoding.UTF8, "application/json");
        var req = new HttpRequestMessage(HttpMethod.Post, $"tasks/{taskId}/ack") { Content = content };
        if (!string.IsNullOrEmpty(_bearerToken)) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
        await _http.SendAsync(req);
    }

    public async Task UpdateTaskAsync(ConductorTaskResult result)
    {
        await EnsureTokenAsync();
        var content = new StringContent(JsonSerializer.Serialize(result), Encoding.UTF8, "application/json");
        var req = new HttpRequestMessage(HttpMethod.Post, "tasks") { Content = content };
        if (!string.IsNullOrEmpty(_bearerToken)) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
        await _http.SendAsync(req);
    }

    private async Task EnsureTokenAsync()
    {
        if(string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_apiSecret)) return;
        if(!string.IsNullOrEmpty(_bearerToken) && _tokenExpiry > DateTime.UtcNow.AddMinutes(1)) return;
        var tokenUrl = _baseUrl + "token";
        var req = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
        {
            Content = new StringContent($"{{\"keyId\":\"{_apiKey}\",\"keySecret\":\"{_apiSecret}\"}}", Encoding.UTF8, "application/json")
        };
        var resp = await _http.SendAsync(req);
        if(!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to get Orkes token: {resp.StatusCode} {body}");
        }
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        _bearerToken = doc.RootElement.GetProperty("token").GetString();
        var exp = doc.RootElement.TryGetProperty("expiryTime", out var expiry) ? expiry.GetInt64() : 0;
        if(exp > 0) _tokenExpiry = DateTimeOffset.FromUnixTimeMilliseconds(exp).UtcDateTime;
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
