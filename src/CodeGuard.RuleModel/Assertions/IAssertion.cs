using CodeGuard.Analysis.AnalysisModel;

namespace CodeGuard.RuleModel.Assertions;

public interface IAssertion
{
    string Kind { get; }

    AssertionOutcome Evaluate(object candidate, RepositoryModel model);
}

/// <summary>
/// The result of evaluating one assertion against one candidate. <see cref="Message"/> is the
/// human-readable explanation; <see cref="Expected"/>, <see cref="Actual"/> and <see cref="Evidence"/>
/// are optional structured data for agents and tooling (what the rule wanted, what was found, and the
/// offending references such as type, namespace or project names). Each is null when the assertion has
/// no natural structured value for the outcome.
/// </summary>
public sealed record AssertionOutcome(
    bool Passed,
    string? Message,
    string? Expected = null,
    string? Actual = null,
    IReadOnlyList<string>? Evidence = null)
{
    public static AssertionOutcome Success() => new(true, null);

    public static AssertionOutcome Failure(string message) => new(false, message);

    public static AssertionOutcome Failure(string message, string? expected, string? actual, IReadOnlyList<string>? evidence) =>
        new(false, message, expected, actual, evidence);
}
