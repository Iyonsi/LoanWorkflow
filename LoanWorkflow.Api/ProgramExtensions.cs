using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LoanWorkflow.Api;

public static class ProgramExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        services.AddHttpClient();
        services.AddHttpClient("conductor");
        services.AddHttpClient("conductor");
        services.AddSingleton<LoanWorkflow.Api.Conductor.ConductorClient>();
        services.AddSingleton<LoanWorkflow.Api.Conductor.IWorkflowStarter>(sp => sp.GetRequiredService<LoanWorkflow.Api.Conductor.ConductorClient>());

        // Conditionally register EF Core provider based on environment to avoid dual provider conflict in tests.
        if (env.IsEnvironment("Testing"))
        {
            services.AddDbContext<LoanWorkflow.Api.Data.LoanWorkflowDbContext>(o => o.UseInMemoryDatabase("test_db"));
        }
        else
        {
            services.AddDbContext<LoanWorkflow.Api.Data.LoanWorkflowDbContext>(o => o.UseSqlServer(config.GetConnectionString("SqlServer")));
        }

        services.AddScoped<LoanWorkflow.Api.Repositories.ILoanRequestRepository, LoanWorkflow.Api.Repositories.LoanRequestRepository>();
        services.AddScoped<LoanWorkflow.Api.Repositories.ILoanRequestLogRepository, LoanWorkflow.Api.Repositories.LoanRequestLogRepository>();
        services.AddScoped<LoanWorkflow.Api.Repositories.IUnitOfWork, LoanWorkflow.Api.Repositories.UnitOfWork>();
        services.AddScoped<LoanWorkflow.Api.Services.ILoanRequestService, LoanWorkflow.Api.Services.LoanRequestService>();
        // Application layer services
        services.AddScoped<LoanWorkflow.Api.Services.ApplicationServices.ILoanRequestApplicationService, LoanWorkflow.Api.Services.ApplicationServices.LoanRequestApplicationService>();
        services.AddScoped<LoanWorkflow.Api.Services.ApplicationServices.IApprovalApplicationService, LoanWorkflow.Api.Services.ApplicationServices.ApprovalApplicationService>();
        services.AddScoped<LoanWorkflow.Api.Services.ApplicationServices.IRegistrationApplicationService, LoanWorkflow.Api.Services.ApplicationServices.RegistrationApplicationService>();
        services.AddScoped<LoanWorkflow.Api.Services.ApplicationServices.IHealthApplicationService, LoanWorkflow.Api.Services.ApplicationServices.HealthApplicationService>();
        return services;
    }
}
