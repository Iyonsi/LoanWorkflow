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
    private readonly bool _enabled;
    private readonly ILogger<ConductorClient>? _logger;
    private readonly string? _apiKey;
    private readonly string? _apiSecret;
    private string? _bearerToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private readonly string _baseUrl;
    public ConductorClient(IConfiguration cfg, IHttpClientFactory factory, ILogger<ConductorClient>? logger = null)
    {
        _http = factory.CreateClient("conductor");
        _logger = logger;
        var rawBase = (cfg["Conductor:BaseUrl"] ?? string.Empty).Trim();
        _enabled = bool.TryParse(cfg["Conductor:Enabled"], out var en) ? en : true;
        if(string.IsNullOrWhiteSpace(rawBase))
        {
            _baseUrl = string.Empty;
            _enabled = false; // force offline if no base url
        }
        else
        {
            _baseUrl = rawBase.TrimEnd('/') + "/";
        }
        _apiKey = cfg["Conductor:ApiKey"];
        _apiSecret = cfg["Conductor:ApiSecret"];
    if(!string.IsNullOrWhiteSpace(_baseUrl)) _http.BaseAddress = new Uri(_baseUrl);
    }

    public async Task<bool> RegisterWorkflowAsync(string workflowJson)
    {
        if(!_enabled || _http.BaseAddress is null) return true; // no-op offline
        await EnsureTokenAsync();
        var content = new StringContent(workflowJson, Encoding.UTF8, "application/json");
        var req = new HttpRequestMessage(HttpMethod.Post, "metadata/workflow") { Content = content };
        if (!string.IsNullOrEmpty(_bearerToken)) req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _bearerToken);
        var resp = await _http.SendAsync(req);
        if(!resp.IsSuccessStatusCode)
        {
            var body = await SafeBody(resp);
            _logger?.LogWarning("RegisterWorkflow failed {Status} {Body}", (int)resp.StatusCode, body);
        }
        else
        {
            _logger?.LogInformation("RegisterWorkflow success {Status}", (int)resp.StatusCode);
        }
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> RegisterTasksAsync(string tasksJson)
    {
        if(!_enabled || _http.BaseAddress is null) return true; // no-op offline
        await EnsureTokenAsync();
        var content = new StringContent(tasksJson, Encoding.UTF8, "application/json");
        var req = new HttpRequestMessage(HttpMethod.Post, "metadata/taskdefs") { Content = content };
        if (!string.IsNullOrEmpty(_bearerToken)) req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _bearerToken);
        var resp = await _http.SendAsync(req);
        if(!resp.IsSuccessStatusCode)
        {
            var body = await SafeBody(resp);
            _logger?.LogWarning("RegisterTasks failed {Status} {Body}", (int)resp.StatusCode, body);
        }
        else
        {
            _logger?.LogInformation("RegisterTasks success {Status}", (int)resp.StatusCode);
        }
        return resp.IsSuccessStatusCode;
    }

    public async Task<string?> StartWorkflowAsync(string name, int version, object input)
    {
        if(!_enabled || _http.BaseAddress is null) return null; // offline mode
        await EnsureTokenAsync();
        var payload = JsonSerializer.Serialize(new { name, version, input });
        var req = new HttpRequestMessage(HttpMethod.Post, "workflow") { Content = new StringContent(payload, Encoding.UTF8, "application/json") };
        if (!string.IsNullOrEmpty(_bearerToken)) req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _bearerToken);
        var resp = await _http.SendAsync(req);
        if(!resp.IsSuccessStatusCode)
        {
            var body = await SafeBody(resp);
            _logger?.LogWarning("StartWorkflow failed {Status} {Body}", (int)resp.StatusCode, body);
            return null;
        }
        var txt = await resp.Content.ReadAsStringAsync();
        _logger?.LogInformation("StartWorkflow success {Status}", (int)resp.StatusCode);
        return txt.Trim('"');
    }

    private async Task EnsureTokenAsync()
    {
        if(!_enabled || string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_apiSecret)) return;
        if(!string.IsNullOrEmpty(_bearerToken) && _tokenExpiry > DateTime.UtcNow.AddMinutes(1)) return;
        var tokenUrl = _baseUrl + "token";
        var req = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
        {
            Content = new StringContent($"{{\"keyId\":\"{_apiKey}\",\"keySecret\":\"{_apiSecret}\"}}", Encoding.UTF8, "application/json")
        };
        var resp = await _http.SendAsync(req);
        if(!resp.IsSuccessStatusCode)
        {
            var body = await SafeBody(resp);
            _logger?.LogError("Token request failed {Status} {Body}", (int)resp.StatusCode, body);
            throw new InvalidOperationException($"Failed to get Orkes token: {resp.StatusCode} {body}");
        }
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        _bearerToken = doc.RootElement.GetProperty("token").GetString();
        var exp = doc.RootElement.TryGetProperty("expiryTime", out var expiry) ? expiry.GetInt64() : 0;
        exp = 1;
        if(exp > 0) _tokenExpiry = DateTimeOffset.FromUnixTimeMilliseconds(exp).UtcDateTime;
    }

    private static async Task<string> SafeBody(HttpResponseMessage resp)
    {
        try { return (await resp.Content.ReadAsStringAsync())[..Math.Min(500, (await resp.Content.ReadAsStringAsync()).Length)]; }
        catch { return "<no body>"; }
    }
}
