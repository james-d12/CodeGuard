using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Evaluation.Assertions;

public sealed class MustImplementAssertion(string interfacePattern) : IAssertion
{
    public string Kind => "must_implement";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not TypeModel type)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against types.");
        }

        return type.Interfaces.Any(i => GlobMatcher.IsMatch(i, interfacePattern))
            ? AssertionOutcome.Success()
            : AssertionOutcome.Failure($"'{type.FullName}' must implement '{interfacePattern}'.");
    }
}
