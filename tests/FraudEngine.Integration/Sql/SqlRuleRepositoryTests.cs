using FraudEngine.Domain;
using FraudEngine.Infrastructure.Sql;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace FraudEngine.Integration.Sql;

public class SqlRuleRepositoryTests : IAsyncLifetime
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

        // Create the RuleDefinitions table and seed test data
        await using var connection = _factory.CreateConnection();
        await connection.OpenAsync();

        await using var command = new SqlCommand(@"
            CREATE TABLE RuleDefinitions (
                RuleId UNIQUEIDENTIFIER PRIMARY KEY,
                RuleName NVARCHAR(100) NOT NULL,
                RuleReason NVARCHAR(50) NOT NULL,
                ScoreDelta INT NOT NULL,
                IsActive BIT NOT NULL DEFAULT 1
            );

            INSERT INTO RuleDefinitions (RuleId, RuleName, RuleReason, ScoreDelta, IsActive)
            VALUES
                (NEWID(), 'High Amount', 'HIGH_AMOUNT_ANOMALY', 30, 1),
                (NEWID(), 'Velocity', 'HIGH_VELOCITY', 25, 1),
                (NEWID(), 'Inactive Rule', 'NEW_DEVICE', 20, 0);", connection);

        await command.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        if (_sqlContainer is not null)
            await _sqlContainer.DisposeAsync();
    }

    [Fact]
    public async Task LoadActiveRulesAsync_ReturnsOnlyActiveRules()
    {
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<SqlRuleRepository>.Instance;
        var repo = new SqlRuleRepository(_factory, logger);

        var rules = await repo.LoadActiveRulesAsync();

        Assert.Equal(2, rules.Count);
        Assert.All(rules, r => Assert.True(r.IsActive));
        Assert.Contains(rules, r => r.RuleReason == RuleReasons.HighAmountAnomaly);
        Assert.Contains(rules, r => r.RuleReason == RuleReasons.HighVelocity);
    }

    [Fact]
    public async Task LoadActiveRulesAsync_ReturnsCorrectScoreDeltas()
    {
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<SqlRuleRepository>.Instance;
        var repo = new SqlRuleRepository(_factory, logger);

        var rules = await repo.LoadActiveRulesAsync();

        var amountRule = rules.First(r => r.RuleReason == RuleReasons.HighAmountAnomaly);
        Assert.Equal(30, amountRule.ScoreDelta);

        var velocityRule = rules.First(r => r.RuleReason == RuleReasons.HighVelocity);
        Assert.Equal(25, velocityRule.ScoreDelta);
    }
}
