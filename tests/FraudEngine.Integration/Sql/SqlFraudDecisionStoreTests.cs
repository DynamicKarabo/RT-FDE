using System.Data;
using FraudEngine.Domain;
using FraudEngine.Infrastructure.Sql;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace FraudEngine.Integration.Sql;

public class SqlFraudDecisionStoreTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Str0ngP@ssw0rd!")
        .WithPortBinding(1433, assignRandomHostPort: true)
        .Build();

    private SqlConnectionFactory _factory = null!;

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();

        var connectionString = _sqlContainer.GetConnectionString()
            .Replace("Database=master;", "Database=master;TrustServerCertificate=true;");

        _factory = new SqlConnectionFactory(connectionString);

        // Create the FraudDecisions table
        await using var connection = _factory.CreateConnection();
        await connection.OpenAsync();

        await using var command = new SqlCommand(@"
            CREATE TABLE FraudDecisions (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                TransactionId UNIQUEIDENTIFIER NOT NULL,
                RiskScore INT NOT NULL,
                Decision NVARCHAR(20) NOT NULL,
                Reasons NVARCHAR(500) NOT NULL,
                Timestamp DATETIMEOFFSET NOT NULL
            );

            CREATE INDEX IX_FraudDecisions_TransactionId ON FraudDecisions(TransactionId);", connection);

        await command.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        if (_sqlContainer is not null)
            await _sqlContainer.DisposeAsync();
    }

    [Fact]
    public async Task PersistDecision_AndGetExisting_RoundTripsCorrectly()
    {
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<SqlFraudDecisionStore>.Instance;
        var store = new SqlFraudDecisionStore(_factory, logger);

        var txnId = Guid.NewGuid();
        var decision = new FraudDecision(
            DecisionOutcome.Reject,
            87,
            new[] { "HIGH_VELOCITY", "NEW_DEVICE" },
            DateTimeOffset.UtcNow);

        await store.PersistDecisionAsync(txnId, decision);

        var retrieved = await store.GetExistingDecisionAsync(txnId);

        Assert.NotNull(retrieved);
        Assert.Equal(DecisionOutcome.Reject, retrieved!.Outcome);
        Assert.Equal(87, retrieved.RiskScore);
        Assert.Equal(2, retrieved.Reasons.Count);
        Assert.Contains("HIGH_VELOCITY", retrieved.Reasons);
        Assert.Contains("NEW_DEVICE", retrieved.Reasons);
    }

    [Fact]
    public async Task GetExistingDecision_ReturnsNull_ForUnknownTransaction()
    {
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<SqlFraudDecisionStore>.Instance;
        var store = new SqlFraudDecisionStore(_factory, logger);

        var result = await store.GetExistingDecisionAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task PersistDecision_Appends_WithoutOverwriting()
    {
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<SqlFraudDecisionStore>.Instance;
        var store = new SqlFraudDecisionStore(_factory, logger);

        var txnId = Guid.NewGuid();

        var decision1 = new FraudDecision(DecisionOutcome.Approve, 10, Array.Empty<string>(), DateTimeOffset.UtcNow);
        var decision2 = new FraudDecision(DecisionOutcome.Reject, 90, new[] { "HIGH_AMOUNT_ANOMALY" }, DateTimeOffset.UtcNow.AddSeconds(1));

        await store.PersistDecisionAsync(txnId, decision1);
        await store.PersistDecisionAsync(txnId, decision2);

        // Should return the most recent (ordered by timestamp DESC)
        var retrieved = await store.GetExistingDecisionAsync(txnId);

        Assert.NotNull(retrieved);
        Assert.Equal(90, retrieved!.RiskScore);
    }
}
