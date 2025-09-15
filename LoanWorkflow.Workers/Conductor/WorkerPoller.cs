using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;
using LoanWorkflow.Shared.Workflow;
using LoanWorkflow.Shared.Domain;

namespace LoanWorkflow.Workers.Conductor;

public class WorkerPoller
{
    private readonly ConductorClient _client;
    private readonly IServiceProvider _sp;
    private readonly IConfiguration _cfg;
    private readonly ILogger<WorkerPoller> _logger;
    private readonly string _connStr;
    private readonly string _workerId = Environment.MachineName + "_loanwf";
    public WorkerPoller(ConductorClient client, IServiceProvider sp, IConfiguration cfg, ILogger<WorkerPoller> logger)
    {
        _client = client; _sp = sp; _cfg = cfg; _logger = logger; _connStr = cfg.GetConnectionString("SqlServer")!;
    }

    public Task StartAsync(CancellationToken token)
    {
        Task.Run(()=>Loop("init_variables", HandleInitVariables, token));
        Task.Run(()=>Loop("fetch_decision", HandleFetchDecision, token));
        Task.Run(()=>Loop("advance_pointer", HandleAdvancePointer, token));
        Task.Run(()=>Loop("handle_rejection", HandleRejection, token));
        Task.Run(()=>Loop("create_loan", HandleCreateLoan, token));
        return Task.CompletedTask;
    }

    private async Task Loop(string taskType, Func<TaskPollResult,Task> handler, CancellationToken token)
    {
        while(!token.IsCancellationRequested)
        {
            try
            {
                var polled = await _client.PollAsync(taskType, _workerId, 10000);
                if (polled == null) { await Task.Delay(1000, token); continue; }
                await _client.AckAsync(polled.TaskId, _workerId);
                await handler(polled);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error in poll loop for {TaskType}", taskType);
                await Task.Delay(2000, token);
            }
        }
    }

    private async Task HandleInitVariables(TaskPollResult task)
    {
        var output = new Dictionary<string,object>();
        var requestId = task.InputData?["requestId"]?.ToString() ?? string.Empty;
    var loanType = task.InputData?["loanType"]?.ToString() ?? string.Empty;
    var stages = FlowConfiguration.Flows[loanType];
        output["stages"] = stages;
        output["stageIndex"] = 0;
        await _client.UpdateTaskAsync(new ConductorTaskResult{TaskId=task.TaskId,WorkflowInstanceId=task.WorkflowInstanceId,OutputData=output});
    }

    private async Task HandleFetchDecision(TaskPollResult task)
    {
        var output = new Dictionary<string,object>();
        var requestId = task.InputData?["requestId"]?.ToString() ?? string.Empty;
    var stageIndex = Convert.ToInt32(task.InputData?["stageIndex"] ?? 0);
        var stagesJson = JsonSerializer.Serialize(task.InputData?["stages"]);
        var stages = JsonSerializer.Deserialize<string[]>(stagesJson!) ?? Array.Empty<string>();
        var currentStage = stages[stageIndex];
        using var con = new SqlConnection(_connStr);
    var decision = await con.QueryFirstOrDefaultAsync<string>($"SELECT TOP 1 Action FROM LoanRequestLog WHERE LoanRequestId=@Id AND Stage=@Stage AND Action IN ('{LoanRequestActions.Approved}','{LoanRequestActions.Rejected}') ORDER BY CreatedAt DESC", new { Id = requestId, Stage = currentStage });
    if (string.IsNullOrEmpty(decision))
        {
            await _client.UpdateTaskAsync(new ConductorTaskResult{TaskId=task.TaskId,WorkflowInstanceId=task.WorkflowInstanceId,Status="IN_PROGRESS", CallbackAfterSeconds=5});
            return;
        }
    output["approved"] = decision == "APPROVED";
        await _client.UpdateTaskAsync(new ConductorTaskResult{TaskId=task.TaskId,WorkflowInstanceId=task.WorkflowInstanceId,OutputData=output});
    }

    private async Task HandleAdvancePointer(TaskPollResult task)
    {
    var stageIndex = Convert.ToInt32(task.InputData?["stageIndex"] ?? 0);
        var stagesJson = JsonSerializer.Serialize(task.InputData?["stages"]);
        var stages = JsonSerializer.Deserialize<string[]>(stagesJson!) ?? Array.Empty<string>();
        var requestId = task.InputData?["requestId"]?.ToString() ?? string.Empty;
        stageIndex++;
        var output = new Dictionary<string,object>{{"stageIndex",stageIndex},{"stages",stages}};
        using var con = new SqlConnection(_connStr);
        if (stageIndex < stages.Length)
        {
            await con.ExecuteAsync("UPDATE LoanRequest SET StageIndex=@Idx, CurrentStage=@Stage, UpdatedAt=@Now WHERE Id=@Id", new { Id=requestId, Idx=stageIndex, Stage=stages[stageIndex], Now=DateTime.UtcNow });
        }
        else
        {
            await con.ExecuteAsync("UPDATE LoanRequest SET Status='Approved', UpdatedAt=@Now WHERE Id=@Id", new { Id=requestId, Now=DateTime.UtcNow });
        }
        await _client.UpdateTaskAsync(new ConductorTaskResult{TaskId=task.TaskId,WorkflowInstanceId=task.WorkflowInstanceId,OutputData=output});
    }

    private async Task HandleRejection(TaskPollResult task)
    {
    var stageIndex = Convert.ToInt32(task.InputData?["stageIndex"] ?? 0);
    var requestId = task.InputData?["requestId"]?.ToString() ?? string.Empty;
    var loanType = task.InputData?["loanType"]?.ToString() ?? string.Empty;
        var stagesJson = System.Text.Json.JsonSerializer.Serialize(task.InputData?["stages"]);
        var stages = System.Text.Json.JsonSerializer.Deserialize<string[]>(stagesJson!) ?? Array.Empty<string>();
        using var con = new SqlConnection(_connStr);
    if (string.Equals(loanType, "standard", StringComparison.OrdinalIgnoreCase))
        {
            stageIndex = 0;
            await con.ExecuteAsync("UPDATE LoanRequest SET StageIndex=0, CurrentStage=@Stage, UpdatedAt=@Now WHERE Id=@Id", new { Id=requestId, Stage=stages[0], Now=DateTime.UtcNow });
        }
    else if (string.Equals(loanType, "flex_review", StringComparison.OrdinalIgnoreCase))
        {
            stageIndex = Math.Max(stageIndex - 1, 0);
            await con.ExecuteAsync("UPDATE LoanRequest SET StageIndex=@Idx, CurrentStage=@Stage, UpdatedAt=@Now WHERE Id=@Id", new { Id=requestId, Idx=stageIndex, Stage=stages[stageIndex], Now=DateTime.UtcNow });
        }
    else if (string.Equals(loanType, "multi_stage", StringComparison.OrdinalIgnoreCase))
        {
            // Should not happen
            await _client.UpdateTaskAsync(new ConductorTaskResult{TaskId=task.TaskId,WorkflowInstanceId=task.WorkflowInstanceId,Status="FAILED",ReasonForIncompletion="Rejection not allowed"});
            return;
        }
        var output = new Dictionary<string,object>{{"stageIndex",stageIndex},{"stages",stages}};
        await _client.UpdateTaskAsync(new ConductorTaskResult{TaskId=task.TaskId,WorkflowInstanceId=task.WorkflowInstanceId,OutputData=output});
    }

    private async Task HandleCreateLoan(TaskPollResult task)
    {
        var requestId = task.InputData?["requestId"]?.ToString() ?? string.Empty;
        using var con = new SqlConnection(_connStr);
        var status = await con.QueryFirstOrDefaultAsync<string>("SELECT Status FROM LoanRequest WHERE Id=@Id", new { Id=requestId });
        if (status != "Approved")
        {
            await _client.UpdateTaskAsync(new ConductorTaskResult{TaskId=task.TaskId,WorkflowInstanceId=task.WorkflowInstanceId,Status="COMPLETED",OutputData=new(){{"status","SKIPPED"}}});
            return;
        }
        var existing = await con.QueryFirstOrDefaultAsync<int>("SELECT COUNT(1) FROM Loan WHERE LoanRequestId=@Id", new { Id=requestId });
        if (existing == 0)
        {
            var now = DateTime.UtcNow;
            await con.ExecuteAsync(@"INSERT INTO Loan (Id, LoanRequestId, LoanNumber, Principal, InterestRate, TermMonths, StartDate, Status, CreatedAt, UpdatedAt)
VALUES (@Id,@LoanRequestId,@LoanNumber,0,0,0,@StartDate,'ACTIVE',@CreatedAt,@UpdatedAt)", new {
                Id = Guid.NewGuid().ToString(), LoanRequestId = requestId, LoanNumber = "LN-" + requestId[..8].ToUpperInvariant(), StartDate = DateTime.UtcNow.Date, CreatedAt = now, UpdatedAt = now
            });
        }
        await _client.UpdateTaskAsync(new ConductorTaskResult{TaskId=task.TaskId,WorkflowInstanceId=task.WorkflowInstanceId,OutputData=new(){{"status","CREATED"}}});
    }
}
