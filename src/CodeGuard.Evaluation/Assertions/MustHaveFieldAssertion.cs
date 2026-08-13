using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Evaluation.Assertions;

public sealed class MustHaveFieldAssertion(string namePattern) : IAssertion
{
    public string Kind => "must_have_field";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not TypeModel type)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against types.");
        }

        return type.Fields.Any(f => GlobMatcher.IsMatch(f.Name, namePattern))
            ? AssertionOutcome.Success()
            : AssertionOutcome.Failure($"'{type.FullName}' must have a field matching '{namePattern}'.");
    }
}
