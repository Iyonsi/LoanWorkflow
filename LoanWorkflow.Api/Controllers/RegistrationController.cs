using Microsoft.AspNetCore.Mvc;
using LoanWorkflow.Api.Features.Common;
using LoanWorkflow.Api.Features.Constants;
using LoanWorkflow.Api.Services.ApplicationServices;

namespace LoanWorkflow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegistrationController : ControllerBase
{
    private readonly IRegistrationApplicationService _appService;
    private readonly IWebHostEnvironment _env;
    public RegistrationController(IRegistrationApplicationService appService, IWebHostEnvironment env){ _appService = appService; _env = env; }

    [HttpPost("register-workflow")]
    public async Task<IActionResult> RegisterWorkflow()
    {
    var traceId = HttpContext.TraceIdentifier;
    var response = await _appService.RegisterWorkflowAsync(traceId, _env.ContentRootPath);
    return MapResponse(response);
    }

    [HttpPost("register-tasks")]
    public async Task<IActionResult> RegisterTasks()
    {
        var traceId = HttpContext.TraceIdentifier;
        var response = await _appService.RegisterTasksAsync(traceId, _env.ContentRootPath);
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
