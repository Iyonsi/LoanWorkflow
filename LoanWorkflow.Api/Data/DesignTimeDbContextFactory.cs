using LoanWorkflow.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LoanWorkflow.Api.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LoanWorkflowDbContext>
{
    public LoanWorkflowDbContext CreateDbContext(string[] args)
    {
        // Build minimal configuration (fallback to default connection if none found)
        var cfg = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Development.json", optional:true)
            .AddJsonFile("appsettings.json", optional:true)
            .AddEnvironmentVariables()
            .Build();
        var cs = cfg.GetConnectionString("SqlServer") ?? "Server=localhost;Database=LoanWorkflow;Trusted_Connection=True;TrustServerCertificate=True;";
        var options = new DbContextOptionsBuilder<LoanWorkflowDbContext>()
            .UseSqlServer(cs)
            .Options;
        return new LoanWorkflowDbContext(options);
    }
}
