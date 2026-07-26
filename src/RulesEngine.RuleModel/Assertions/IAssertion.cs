using RulesEngine.Analysis.AnalysisModel;

namespace RulesEngine.RuleModel.Assertions;

public interface IAssertion
{
    string Kind { get; }

    AssertionOutcome Evaluate(object candidate, RepositoryModel model);
}

public sealed record AssertionOutcome(bool Passed, string? Message)
{
    public static AssertionOutcome Success() => new(true, null);

    public static AssertionOutcome Failure(string message) => new(false, message);
}
