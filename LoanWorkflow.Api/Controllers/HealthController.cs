using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LoanWorkflow.Api.Data;
using LoanWorkflow.Api.Conductor;

namespace LoanWorkflow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly LoanWorkflowDbContext _db;
    private readonly ConductorClient _conductor;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _cfg;
    public HealthController(LoanWorkflowDbContext db, ConductorClient conductor, IHttpClientFactory httpFactory, IConfiguration cfg)
    { _db = db; _conductor = conductor; _httpFactory = httpFactory; _cfg = cfg; }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var dbOk = await _db.Database.CanConnectAsync();
        var baseUrl = _cfg["Conductor:BaseUrl"] ?? string.Empty;
        var conductorOk = false;
        if(!string.IsNullOrWhiteSpace(baseUrl))
        {
            try
            {
                var client = _httpFactory.CreateClient("conductor");
                if(!client.BaseAddress?.ToString().Contains(baseUrl.TrimEnd('/')) ?? true)
                {
                    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
                }
                var resp = await client.GetAsync("/health", HttpContext.RequestAborted);
                conductorOk = resp.IsSuccessStatusCode;
            }
            catch { conductorOk = false; }
        }
        return Ok(new { db = dbOk ? "OK" : "FAIL", conductor = conductorOk ? "OK" : "FAIL" });
    }
}
