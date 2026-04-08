using Microsoft.Data.SqlClient;

namespace FraudEngine.Infrastructure.Sql;

/// <summary>
/// Factory for creating SqlConnection instances with pooled connections.
/// </summary>
public sealed class SqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public SqlConnection CreateConnection() => new(_connectionString);
}
