using Microsoft.AspNetCore.Mvc;
using LoanWorkflow.Api.Features.Common;
using LoanWorkflow.Api.Features.Constants;
using LoanWorkflow.Api.Services.ApplicationServices;

namespace LoanWorkflow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IHealthApplicationService _appService;
    private readonly IConfiguration _cfg;
    private readonly IHttpClientFactory _httpFactory;
    public HealthController(IHealthApplicationService appService, IConfiguration cfg, IHttpClientFactory httpFactory)
    { _appService = appService; _cfg = cfg; _httpFactory = httpFactory; }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var traceId = HttpContext.TraceIdentifier;
        var response = await _appService.GetAsync(traceId, _cfg, _httpFactory, HttpContext.RequestAborted);
        return MapResponse(response);
    }

    private IActionResult MapResponse<T>(ApiResponse<T> response)
    {
        if(response.ResponseCode == ResponseCodes.SUCCESS || response.ResponseCode == ResponseCodes.CREATED)
            return Ok(response);
        if(response.ResponseCode == ResponseCodes.NOT_FOUND)
            return NotFound(response);
        if(response.ResponseCode == ResponseCodes.VALIDATION_ERROR)
            return BadRequest(response);
        if(response.ResponseCode == ResponseCodes.FAILURE)
            return BadRequest(response);
        return StatusCode(500, response);
    }
}
