using FraudEngine.Domain;
using FraudEngine.Domain.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FraudEngine.Infrastructure.Sql;

/// <summary>
/// SQL Server implementation of IRuleRepository.
/// Loads active rule definitions from the RuleDefinitions table.
/// </summary>
public sealed class SqlRuleRepository : IRuleRepository
{
    private readonly SqlConnectionFactory _connectionFactory;
    private readonly ILogger<SqlRuleRepository> _logger;

    public SqlRuleRepository(SqlConnectionFactory connectionFactory, ILogger<SqlRuleRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RuleDefinition>> LoadActiveRulesAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT RuleId, RuleName, RuleReason, ScoreDelta, IsActive
            FROM RuleDefinitions
            WHERE IsActive = 1";

        var rules = new List<RuleDefinition>();

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            rules.Add(new RuleDefinition(
                RuleId: reader.GetGuid(0),
                RuleName: reader.GetString(1),
                RuleReason: reader.GetString(2),
                ScoreDelta: reader.GetInt32(3),
                IsActive: reader.GetBoolean(4)));
        }

        _logger.LogInformation("Loaded {RuleCount} active rules from RuleDefinitions.", rules.Count);

        return rules;
    }
}
