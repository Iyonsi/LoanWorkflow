using System.Data;
using Microsoft.Data.SqlClient;

namespace LoanWorkflow.Api.Infrastructure;

public interface IDbConnectionFactory
{
    IDbConnection Create();
}

public sealed class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;
    public SqlConnectionFactory(IConfiguration cfg)
    {
        _connectionString = cfg.GetConnectionString("SqlServer") ?? throw new InvalidOperationException("SqlServer connection string missing");
    }
    public IDbConnection Create() => new SqlConnection(_connectionString);
}
