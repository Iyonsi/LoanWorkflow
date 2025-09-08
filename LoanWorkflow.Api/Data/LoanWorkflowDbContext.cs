using LoanWorkflow.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace LoanWorkflow.Api.Data;

public class LoanWorkflowDbContext : DbContext
{
    public LoanWorkflowDbContext(DbContextOptions<LoanWorkflowDbContext> options) : base(options) {}

    public DbSet<LoanRequest> LoanRequests => Set<LoanRequest>();
    public DbSet<LoanRequestLog> LoanRequestLogs => Set<LoanRequestLog>();
    public DbSet<Loan> Loans => Set<Loan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LoanRequest>(e =>
        {
            e.ToTable("LoanRequest");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(36);
            e.Property(x => x.BorrowerId).HasMaxLength(36);
            e.Property(x => x.Status).HasConversion<string>();
        });
        modelBuilder.Entity<LoanRequestLog>(e =>
        {
            e.ToTable("LoanRequestLog");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(36);
            e.Property(x => x.LoanRequestId).HasMaxLength(36);
            e.Property(x => x.ActorUserId).HasMaxLength(36);
        });
        modelBuilder.Entity<Loan>(e =>
        {
            e.ToTable("Loan");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(36);
            e.Property(x => x.LoanRequestId).HasMaxLength(36);
        });
        base.OnModelCreating(modelBuilder);
    }
}
