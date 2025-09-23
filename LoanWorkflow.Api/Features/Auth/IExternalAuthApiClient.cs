using System.Net.Http.Json;

namespace LoanWorkflow.Api.Features.Auth;

public interface IExternalAuthApiClient
{
    Task<bool> ValidateCredentialsAsync(string email, string encryptedPassword, CancellationToken ct = default);
}

internal sealed class ExternalAuthApiClient : IExternalAuthApiClient
{
    private readonly HttpClient _http;
    private readonly ExternalAuthOptions _options;

    public ExternalAuthApiClient(HttpClient http, ExternalAuthOptions options)
    {
        _http = http;
        _options = options;
        if(!string.IsNullOrWhiteSpace(options.BaseUrl))
            _http.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/'));
    }

    private sealed record ValidateRequest(string Email, string Password);
    private sealed record ValidateResponse(bool IsValid);

    public async Task<bool> ValidateCredentialsAsync(string email, string encryptedPassword, CancellationToken ct = default)
    {
        var path = _options.ValidatePath.StartsWith('/') ? _options.ValidatePath : "/" + _options.ValidatePath;
        var requestObj = new ValidateRequest(email, encryptedPassword);
        using var response = await _http.PostAsJsonAsync(path, requestObj, ct);
        if(!response.IsSuccessStatusCode) return false; // treat non-200 as invalid for now
        var payload = await response.Content.ReadFromJsonAsync<ValidateResponse>(cancellationToken: ct);
        return payload?.IsValid ?? false;
    }
}
