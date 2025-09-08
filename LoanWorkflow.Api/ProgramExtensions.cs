using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LoanWorkflow.Api;

public static class ProgramExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration config)
    {
    services.AddHttpClient();
    services.AddSingleton<LoanWorkflow.Api.Conductor.ConductorClient>();
    services.AddSingleton<LoanWorkflow.Api.Conductor.IWorkflowStarter>(sp => sp.GetRequiredService<LoanWorkflow.Api.Conductor.ConductorClient>());
    services.AddDbContext<LoanWorkflow.Api.Data.LoanWorkflowDbContext>(o => o.UseSqlServer(config.GetConnectionString("SqlServer")));
    // Remove old factory (Dapper) once fully migrated
    services.AddScoped<LoanWorkflow.Api.Repositories.ILoanRequestRepository, LoanWorkflow.Api.Repositories.LoanRequestRepository>();
    services.AddScoped<LoanWorkflow.Api.Repositories.ILoanRequestLogRepository, LoanWorkflow.Api.Repositories.LoanRequestLogRepository>();
    services.AddScoped<LoanWorkflow.Api.Repositories.IUnitOfWork, LoanWorkflow.Api.Repositories.UnitOfWork>();
    services.AddScoped<LoanWorkflow.Api.Services.ILoanRequestService, LoanWorkflow.Api.Services.LoanRequestService>();
        return services;
    }
}
