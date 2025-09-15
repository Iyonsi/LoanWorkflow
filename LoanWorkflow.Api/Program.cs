using LoanWorkflow.Api;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<LoanWorkflow.Api.Infrastructure.ApiResponseResultFilter>();
});
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(o =>
{
    o.SuppressModelStateInvalidFilter = true; // we'll handle validation manually to produce consistent ApiResponse
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAppServices(builder.Configuration, builder.Environment);

var app = builder.Build();

// Apply pending EF Core migrations automatically on startup except in Testing environment
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<LoanWorkflow.Api.Data.LoanWorkflowDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        logger.LogError(ex, "Database migration failed");
        throw; // rethrow to avoid running with invalid schema
    }
}
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseMiddleware<LoanWorkflow.Api.Infrastructure.ApiResponseExceptionMiddleware>();
app.MapControllers();
app.Run();

public partial class Program { }
