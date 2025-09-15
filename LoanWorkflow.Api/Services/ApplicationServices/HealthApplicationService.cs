using LoanWorkflow.Api.Data;
using LoanWorkflow.Api.Conductor;
using LoanWorkflow.Api.Features.Common;
using LoanWorkflow.Api.Features.Constants;
using Microsoft.EntityFrameworkCore;

namespace LoanWorkflow.Api.Services.ApplicationServices;

public interface IHealthApplicationService
{
    Task<ApiResponse<object>> GetAsync(string traceId, IConfiguration cfg, IHttpClientFactory httpFactory, CancellationToken ct = default);
}

public sealed class HealthApplicationService : IHealthApplicationService
{
    private readonly LoanWorkflowDbContext _db;
    public HealthApplicationService(LoanWorkflowDbContext db){ _db = db; }

    public async Task<ApiResponse<object>> GetAsync(string traceId, IConfiguration cfg, IHttpClientFactory httpFactory, CancellationToken ct = default)
    {
        var dbOk = await _db.Database.CanConnectAsync(ct);
        var baseUrl = cfg["Conductor:BaseUrl"] ?? string.Empty;
        var conductorOk = false;
        if(!string.IsNullOrWhiteSpace(baseUrl))
        {
            try
            {
                var client = httpFactory.CreateClient("conductor");
                if(!client.BaseAddress?.ToString().Contains(baseUrl.TrimEnd('/')) ?? true)
                {
                    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
                }
                var resp = await client.GetAsync("/health", ct);
                conductorOk = resp.IsSuccessStatusCode;
            }
            catch { conductorOk = false; }
        }
        var data = new { db = dbOk ? "OK" : "FAIL", conductor = conductorOk ? "OK" : "FAIL" };
        return ApiResponse<object>.Success(data, "Health status", ResponseCodes.SUCCESS, traceId);
    }
}
