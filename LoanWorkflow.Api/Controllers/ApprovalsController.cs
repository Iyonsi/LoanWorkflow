using LoanWorkflow.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using LoanWorkflow.Api.Services;
using LoanWorkflow.Api.Features.Common;
using LoanWorkflow.Api.Features.Constants;
using LoanWorkflow.Api.Services.ApplicationServices;

namespace LoanWorkflow.Api.Controllers;

[ApiController]
[Route("api/loan-requests/{requestId}/[controller]")]
public class ApprovalsController : ControllerBase
{
    private readonly IApprovalApplicationService _appService;
    public ApprovalsController(IApprovalApplicationService appService){ _appService = appService; }

    [HttpPost("approve")] 
    public async Task<IActionResult> Approve(string requestId, [FromBody] DecisionDto dto)
    {
        var traceId = HttpContext.TraceIdentifier;
        if(!ModelState.IsValid)
        {
            var respInvalid = ApiResponse<object>.Validation("Invalid payload", ModelState.Values.SelectMany(v=>v.Errors).Select(e=>e.ErrorMessage), traceId);
            return BadRequest(respInvalid);
        }
        var response = await _appService.ApproveAsync(requestId, dto, traceId);
        return MapResponse(response);
    }

    [HttpPost("reject")] 
    public async Task<IActionResult> Reject(string requestId, [FromBody] DecisionDto dto)
    {
        var traceId = HttpContext.TraceIdentifier;
        if(!ModelState.IsValid)
        {
            var respInvalid = ApiResponse<object>.Validation("Invalid payload", ModelState.Values.SelectMany(v=>v.Errors).Select(e=>e.ErrorMessage), traceId);
            return BadRequest(respInvalid);
        }
        var response = await _appService.RejectAsync(requestId, dto, traceId);
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
