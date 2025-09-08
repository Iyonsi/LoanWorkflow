using LoanWorkflow.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using LoanWorkflow.Api.Services;
using LoanWorkflow.Shared.Domain;

namespace LoanWorkflow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoanRequestsController : ControllerBase
{
    private readonly ILoanRequestService _service;
    public LoanRequestsController(ILoanRequestService service){ _service = service; }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLoanRequestDto dto)
    {
        if(!ModelState.IsValid) return ValidationProblem(ModelState);
        var (req, wfId) = await _service.CreateAsync(dto);
        return Ok(new { requestId = req.Id, workflowId = wfId });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var req = await _service.GetAsync(id);
        if (req is null) return NotFound();
        return Ok(req);
    }

    [HttpGet("{id}/logs")]
    public async Task<IActionResult> GetLogs(string id)
    {
        var logs = await _service.GetLogsAsync(id);
        return Ok(logs);
    }
}
