using Microsoft.AspNetCore.Mvc;
using LoanWorkflow.Api.Conductor;

namespace LoanWorkflow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegistrationController : ControllerBase
{
    private readonly ConductorClient _client;
    private readonly IWebHostEnvironment _env;
    public RegistrationController(ConductorClient client, IWebHostEnvironment env){ _client = client; _env = env; }

    [HttpPost("register-workflow")]
    public async Task<IActionResult> RegisterWorkflow()
    {
    var path = Path.Combine(_env.ContentRootPath, "WorkflowDefinitions", "loan_dynamic_workflow.json");
        if(!System.IO.File.Exists(path)) return NotFound("workflow json missing");
        var json = await System.IO.File.ReadAllTextAsync(path);
        var ok = await _client.RegisterWorkflowAsync(json);
        return Ok(new { success = ok });
    }

    [HttpPost("register-tasks")]
    public async Task<IActionResult> RegisterTasks()
    {
    var path = Path.Combine(_env.ContentRootPath, "WorkflowDefinitions", "task_definitions.json");
        if(!System.IO.File.Exists(path)) return NotFound("tasks json missing");
        var json = await System.IO.File.ReadAllTextAsync(path);
        var ok = await _client.RegisterTasksAsync(json);
        return Ok(new { success = ok });
    }
}
