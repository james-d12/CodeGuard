using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Evaluation.Assertions;

public sealed class MustHaveMethodAssertion(string namePattern) : IAssertion
{
    public string Kind => "must_have_method";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not TypeModel type)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against types.");
        }

        return type.Methods.Any(m => GlobMatcher.IsMatch(m.Name, namePattern))
            ? AssertionOutcome.Success()
            : AssertionOutcome.Failure($"'{type.FullName}' must have a method matching '{namePattern}'.");
    }
}
