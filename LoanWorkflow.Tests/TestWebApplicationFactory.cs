using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using LoanWorkflow.Api.Data;
using Microsoft.Extensions.Hosting;
using System.Linq;

namespace LoanWorkflow.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // No manual DbContext override needed now; ProgramExtensions selects InMemory for Testing.
    }
}
