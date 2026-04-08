namespace FraudEngine.Application;

// Intentionally empty — FraudThresholds and RuleEvaluationThresholds are bound
// via IConfiguration.GetSection().Get<T>() in the DI registration (Program.cs).
// No custom IConfigureOptions implementation needed.
