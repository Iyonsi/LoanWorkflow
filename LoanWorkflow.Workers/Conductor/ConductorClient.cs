using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace LoanWorkflow.Workers.Conductor;

public class ConductorClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _apiSecret;
    private readonly string _baseUrl;
    private readonly ILogger<ConductorClient>? _logger;
    private string? _bearerToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1,1);
    private bool IsConfigured => !string.IsNullOrWhiteSpace(_baseUrl) && !string.IsNullOrEmpty(_apiKey) && !string.IsNullOrEmpty(_apiSecret);

    public ConductorClient(HttpClient http, IConfiguration cfg, ILogger<ConductorClient>? logger = null)
    {
        _http = http;
        _logger = logger;
        _apiKey = cfg["Conductor:ApiKey"] ?? "";
        _apiSecret = cfg["Conductor:ApiSecret"] ?? "";
        _baseUrl = (cfg["Conductor:BaseUrl"] ?? string.Empty).TrimEnd('/') + "/";
        if (!string.IsNullOrWhiteSpace(_baseUrl))
        {
            _http.BaseAddress = new Uri(_baseUrl);
        }
        if (!IsConfigured)
        {
            _logger?.LogInformation("Conductor client disabled: missing BaseUrl or API credentials.");
        }
    }

    public async Task<TaskPollResult?> PollAsync(string taskType, string workerId, int timeoutMs = 10000)
    {
        if (!IsConfigured) return null; // Skip silently when not configured
        await EnsureTokenAsync();
        if (string.IsNullOrEmpty(_bearerToken))
        {
            _logger?.LogWarning("Skipping poll {TaskType} because token not acquired", taskType);
            return null;
        }
        var req = new HttpRequestMessage(HttpMethod.Get, $"tasks/poll/{taskType}?workerid={workerId}&domain=");
        if (!string.IsNullOrEmpty(_bearerToken)) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
        var resp = await _http.SendAsync(req);
        if(!resp.IsSuccessStatusCode)
        {
            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var body = await resp.Content.ReadAsStringAsync();
                _logger?.LogWarning("Conductor poll unauthorized (will attempt token refresh): {Body}", body);
                // Force refresh and retry once
                InvalidateToken();
                await EnsureTokenAsync(force:true);
                if (string.IsNullOrEmpty(_bearerToken))
                {
                    _logger?.LogWarning("Token still null after forced refresh, aborting poll {TaskType}", taskType);
                    return null;
                }
                req = new HttpRequestMessage(HttpMethod.Get, $"tasks/poll/{taskType}?workerid={workerId}&domain=");
                if (!string.IsNullOrEmpty(_bearerToken)) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
                resp = await _http.SendAsync(req);
                if(!resp.IsSuccessStatusCode)
                {
                    var retryBody = await resp.Content.ReadAsStringAsync();
                    _logger?.LogWarning("Conductor poll still failing after refresh: {Status} {Body}", (int)resp.StatusCode, retryBody);
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
        var json = await resp.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(json)) return null;
        return JsonSerializer.Deserialize<TaskPollResult>(json, new JsonSerializerOptions{PropertyNameCaseInsensitive=true});
    }

    public async Task AckAsync(string taskId, string workerId)
    {
        if (!IsConfigured) return;
        await EnsureTokenAsync();
        var content = new StringContent(JsonSerializer.Serialize(new { workerId }), Encoding.UTF8, "application/json");
        var req = new HttpRequestMessage(HttpMethod.Post, $"tasks/{taskId}/ack") { Content = content };
        if (!string.IsNullOrEmpty(_bearerToken)) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
        await _http.SendAsync(req);
    }

    public async Task UpdateTaskAsync(ConductorTaskResult result)
    {
        if (!IsConfigured) return;
        await EnsureTokenAsync();
        var content = new StringContent(JsonSerializer.Serialize(result), Encoding.UTF8, "application/json");
        var req = new HttpRequestMessage(HttpMethod.Post, "tasks") { Content = content };
        if (!string.IsNullOrEmpty(_bearerToken)) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
        await _http.SendAsync(req);
    }

    private async Task EnsureTokenAsync(bool force = false)
    {
        if(!IsConfigured) return;
        if(!force && !string.IsNullOrEmpty(_bearerToken) && _tokenExpiry > DateTime.UtcNow.AddMinutes(1)) return;
        await _tokenLock.WaitAsync();
        try
        {
            if(!force && !string.IsNullOrEmpty(_bearerToken) && _tokenExpiry > DateTime.UtcNow.AddMinutes(1)) return; // double-check inside lock
            var tokenUrl = _baseUrl + "token";
            var req = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
            {
                // Build JSON manually to avoid extra serializer dependency for this tiny payload
                Content = new StringContent($"{{\"keyId\":\"{_apiKey}\",\"keySecret\":\"{_apiSecret}\"}}", Encoding.UTF8, "application/json")
            };
            var resp = await _http.SendAsync(req);
            if(!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                _logger?.LogWarning("Failed to get Orkes token: {Status} {Body}", resp.StatusCode, body);
                return; // fail soft
            }
            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            _bearerToken = doc.RootElement.TryGetProperty("token", out var tokenEl) ? tokenEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(_bearerToken))
            {
                _logger?.LogWarning("Token response missing 'token' field: {Json}", json);
                return;
            }
            var exp = doc.RootElement.TryGetProperty("expiryTime", out var expiry) ? expiry.GetInt64() : 0;
            if(exp > 0)
            {
                _tokenExpiry = DateTimeOffset.FromUnixTimeMilliseconds(exp).UtcDateTime;
            }
            else
            {
                // Default to 10 minutes if no expiry provided
                _tokenExpiry = DateTime.UtcNow.AddMinutes(10);
            }
            _logger?.LogInformation("Obtained Orkes token expiring at {Expiry} (force={Force})", _tokenExpiry, force);
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private void InvalidateToken()
    {
        _bearerToken = null;
        _tokenExpiry = DateTime.MinValue;
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
