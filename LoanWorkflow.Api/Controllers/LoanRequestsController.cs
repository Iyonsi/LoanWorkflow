using LoanWorkflow.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using LoanWorkflow.Api.Services;
using LoanWorkflow.Shared.Domain;
using LoanWorkflow.Api.Features.Common;
using LoanWorkflow.Api.Features.Constants;
using LoanWorkflow.Api.Features.LoanRequests;
using LoanWorkflow.Api.Services.ApplicationServices;

namespace LoanWorkflow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Microsoft.AspNetCore.Authorization.Authorize]
public class LoanRequestsController : ControllerBase
{
    private readonly ILoanRequestApplicationService _appService;
    public LoanRequestsController(ILoanRequestApplicationService appService){ _appService = appService; }

    [HttpPost("standard")]
    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "CanInitiateLoan")]
    public async Task<IActionResult> CreateStandard([FromBody] CreateLoanRequestDto dto)
    {
        var traceId = HttpContext.TraceIdentifier;
        if(!ModelState.IsValid)
        {
            var respInvalid = ApiResponse<object>.Validation("Invalid payload", ModelState.Values.SelectMany(v=>v.Errors).Select(e=>e.ErrorMessage), traceId);
            return BadRequest(respInvalid);
        }
        var response = await _appService.CreateAsync(dto, "standard", traceId);
        return MapResponse(response);
    }

    [HttpPost("multi-stage")]
    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "CanInitiateLoan")]
    public async Task<IActionResult> CreateMultiStage([FromBody] CreateLoanRequestDto dto)
    {
        var traceId = HttpContext.TraceIdentifier;
        if(!ModelState.IsValid)
        {
            var respInvalid = ApiResponse<object>.Validation("Invalid payload", ModelState.Values.SelectMany(v=>v.Errors).Select(e=>e.ErrorMessage), traceId);
            return BadRequest(respInvalid);
        }
        var response = await _appService.CreateAsync(dto, "multi_stage", traceId);
        return MapResponse(response);
    }

    [HttpPost("flex-review")]
    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "CanInitiateLoan")]
    public async Task<IActionResult> CreateFlexReview([FromBody] CreateLoanRequestDto dto)
    {
        var traceId = HttpContext.TraceIdentifier;
        if(!ModelState.IsValid)
        {
            var respInvalid = ApiResponse<object>.Validation("Invalid payload", ModelState.Values.SelectMany(v=>v.Errors).Select(e=>e.ErrorMessage), traceId);
            return BadRequest(respInvalid);
        }
        var response = await _appService.CreateAsync(dto, "flex_review", traceId);
        return MapResponse(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
    var traceId = HttpContext.TraceIdentifier;
    var response = await _appService.GetAsync(id, traceId);
    return MapResponse(response);
    }

    [HttpGet("{id}/logs")]
    public async Task<IActionResult> GetLogs(string id)
    {
    var traceId = HttpContext.TraceIdentifier;
    var response = await _appService.GetLogsAsync(id, traceId);
    return MapResponse(response);
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? loanRequestId, [FromQuery] string? stages, [FromQuery] string? loanType,
        [FromQuery] DateTime? createdFromUtc, [FromQuery] DateTime? createdToUtc, [FromQuery] DateTime? date,
        [FromQuery] string? statuses, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var traceId = HttpContext.TraceIdentifier;
        var response = await _appService.SearchAsync(loanRequestId, stages, loanType, createdFromUtc, createdToUtc, date, statuses, page, pageSize, traceId);
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
