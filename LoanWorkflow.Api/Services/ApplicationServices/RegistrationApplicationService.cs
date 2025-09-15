using LoanWorkflow.Api.Conductor;
using LoanWorkflow.Api.Features.Common;
using LoanWorkflow.Api.Features.Constants;

namespace LoanWorkflow.Api.Services.ApplicationServices;

public interface IRegistrationApplicationService
{
    Task<ApiResponse<object>> RegisterWorkflowAsync(string traceId, string contentRootPath);
    Task<ApiResponse<object>> RegisterTasksAsync(string traceId, string contentRootPath);
}

public sealed class RegistrationApplicationService : IRegistrationApplicationService
{
    private readonly ConductorClient _client;
    public RegistrationApplicationService(ConductorClient client){ _client = client; }

    public async Task<ApiResponse<object>> RegisterWorkflowAsync(string traceId, string contentRootPath)
    {
        var path = Path.Combine(contentRootPath, "WorkflowDefinitions", "loan_dynamic_workflow.json");
        if(!File.Exists(path)) return ApiResponse<object>.NotFound("workflow json missing", traceId);
        var json = await File.ReadAllTextAsync(path);
        var ok = await _client.RegisterWorkflowAsync(json);
        return ApiResponse<object>.Success(new { success = ok }, ok ? "Workflow registered" : "Workflow registration failed", ok ? ResponseCodes.SUCCESS : ResponseCodes.FAILURE, traceId);
    }

    public async Task<ApiResponse<object>> RegisterTasksAsync(string traceId, string contentRootPath)
    {
        var path = Path.Combine(contentRootPath, "WorkflowDefinitions", "task_definitions.json");
        if(!File.Exists(path)) return ApiResponse<object>.NotFound("tasks json missing", traceId);
        var json = await File.ReadAllTextAsync(path);
        var ok = await _client.RegisterTasksAsync(json);
        return ApiResponse<object>.Success(new { success = ok }, ok ? "Tasks registered" : "Task registration failed", ok ? ResponseCodes.SUCCESS : ResponseCodes.FAILURE, traceId);
    }
}
