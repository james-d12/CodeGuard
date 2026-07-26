using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Evaluation.Assertions;

public sealed class MustNotImplementAssertion(string interfacePattern) : IAssertion
{
    public string Kind => "must_not_implement";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not TypeModel type)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against types.");
        }

        var match = type.Interfaces.FirstOrDefault(i => GlobMatcher.IsMatch(i, interfacePattern));
        return match is null
            ? AssertionOutcome.Success()
            : AssertionOutcome.Failure($"'{type.FullName}' must not implement '{match}'.");
    }
}
