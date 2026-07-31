using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Evaluation.Assertions;

public sealed class MustHavePropertyAssertion(string namePattern) : IAssertion
{
    public string Kind => "must_have_property";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not TypeModel type)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against types.");
        }

        return type.Properties.Any(p => GlobMatcher.IsMatch(p.Name, namePattern))
            ? AssertionOutcome.Success()
            : AssertionOutcome.Failure($"'{type.FullName}' must have a property matching '{namePattern}'.");
    }
}
