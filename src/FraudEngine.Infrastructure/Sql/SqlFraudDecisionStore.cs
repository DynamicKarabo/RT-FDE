using FraudEngine.Domain;
using FraudEngine.Domain.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace FraudEngine.Infrastructure.Sql;

/// <summary>
/// SQL Server implementation of IFraudDecisionStore.
/// Append-only — no UPDATE, no DELETE permissions granted to the application service account.
/// </summary>
public sealed class SqlFraudDecisionStore : IFraudDecisionStore
{
    private readonly SqlConnectionFactory _connectionFactory;
    private readonly ILogger<SqlFraudDecisionStore> _logger;

    public SqlFraudDecisionStore(SqlConnectionFactory connectionFactory, ILogger<SqlFraudDecisionStore> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<FraudDecision?> GetExistingDecisionAsync(Guid transactionId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT TOP 1 RiskScore, Decision, Reasons, Timestamp
            FROM FraudDecisions
            WHERE TransactionId = @TransactionId
            ORDER BY Timestamp DESC";

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TransactionId", transactionId);

        await using var reader = await command.ExecuteReaderAsync(ct);

        if (!await reader.ReadAsync(ct))
            return null;

        var riskScore = reader.GetInt32(0);
        var decisionStr = reader.GetString(1);
        var reasonsStr = reader.GetString(2);
        var timestamp = reader.GetDateTimeOffset(3);

        var outcome = decisionStr.ToUpperInvariant() switch
        {
            "APPROVE" => DecisionOutcome.Approve,
            "REVIEW" => DecisionOutcome.Review,
            "REJECT" => DecisionOutcome.Reject,
            _ => throw new InvalidOperationException($"Unknown decision value: {decisionStr}")
        };

        var reasons = reasonsStr
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        return new FraudDecision(outcome, riskScore, reasons, timestamp);
    }

    public async Task PersistDecisionAsync(Guid transactionId, FraudDecision decision, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO FraudDecisions (TransactionId, RiskScore, Decision, Reasons, Timestamp)
            VALUES (@TransactionId, @RiskScore, @Decision, @Reasons, @Timestamp)";

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TransactionId", transactionId);
        command.Parameters.AddWithValue("@RiskScore", decision.RiskScore);
        command.Parameters.AddWithValue("@Decision", decision.Outcome.ToString().ToUpperInvariant());
        command.Parameters.AddWithValue("@Reasons", string.Join(", ", decision.Reasons));
        command.Parameters.AddWithValue("@Timestamp", decision.Timestamp);

        var rows = await command.ExecuteNonQueryAsync(ct);

        if (rows != 1)
        {
            _logger.LogError("Expected 1 row inserted for transaction {TransactionId}, but got {Rows}.",
                transactionId, rows);
            throw new InvalidOperationException("Failed to persist fraud decision.");
        }

        _logger.LogInformation(
            "Persisted decision {Decision} for transaction {TransactionId} with score {RiskScore}.",
            decision.Outcome, transactionId, decision.RiskScore);
    }
}
