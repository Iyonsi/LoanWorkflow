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

        // Options binding
        var jwtOptions = new LoanWorkflow.Api.Features.Auth.JwtOptions();
        config.GetSection(LoanWorkflow.Api.Features.Auth.JwtOptions.SectionName).Bind(jwtOptions);
        services.AddSingleton(jwtOptions);
        var externalOptions = new LoanWorkflow.Api.Features.Auth.ExternalAuthOptions();
        config.GetSection(LoanWorkflow.Api.Features.Auth.ExternalAuthOptions.SectionName).Bind(externalOptions);
        services.AddSingleton(externalOptions);

        // Auth related services
        services.AddSingleton<LoanWorkflow.Api.Features.Auth.IAesEncryptionService, LoanWorkflow.Api.Features.Auth.AesEncryptionService>();
        services.AddHttpClient<LoanWorkflow.Api.Features.Auth.IExternalAuthApiClient, LoanWorkflow.Api.Features.Auth.ExternalAuthApiClient>();
        services.AddScoped<LoanWorkflow.Api.Services.ApplicationServices.IAuthApplicationService, LoanWorkflow.Api.Services.ApplicationServices.AuthApplicationService>();

        // Authentication / Authorization
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtOptions.SigningKey))
            };
        });
        services.AddAuthorization(options =>
        {
            options.AddPolicy("CanInitiateLoan", policy =>
                policy.RequireRole("FT"));
            // Approval roles: all roles except FT
            options.AddPolicy("CanApproveLoan", policy =>
                policy.RequireRole(LoanWorkflow.Shared.Workflow.MockUsers.Roles.Where(r => !string.Equals(r, "FT", StringComparison.OrdinalIgnoreCase)).ToArray()));
        });

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
