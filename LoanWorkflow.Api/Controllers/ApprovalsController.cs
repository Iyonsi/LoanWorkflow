using LoanWorkflow.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using LoanWorkflow.Api.Services;

namespace LoanWorkflow.Api.Controllers;

[ApiController]
[Route("api/loan-requests/{requestId}/[controller]")]
public class ApprovalsController : ControllerBase
{
    private readonly ILoanRequestService _service;
    public ApprovalsController(ILoanRequestService service){ _service = service; }

    [HttpPost("approve")] 
    public async Task<IActionResult> Approve(string requestId, [FromBody] DecisionDto dto)
    {
        if(!ModelState.IsValid) return ValidationProblem(ModelState);
        try
        {
            var result = await _service.ApproveAsync(requestId, dto);
            return Ok(new { requestId, stage = result.Stage, approved = result.Approved });
        }
        catch (KeyNotFoundException){ return NotFound(); }
        catch (InvalidOperationException ex){ return Conflict(ex.Message); }
    }

    [HttpPost("reject")] 
    public async Task<IActionResult> Reject(string requestId, [FromBody] DecisionDto dto)
    {
        if(!ModelState.IsValid) return ValidationProblem(ModelState);
        try
        {
            var result = await _service.RejectAsync(requestId, dto);
            return Ok(new { requestId, stage = result.Stage, approved = result.Approved });
        }
        catch (KeyNotFoundException){ return NotFound(); }
        catch (InvalidOperationException ex){ return BadRequest(ex.Message); }
    }
}
