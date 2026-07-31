using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Evaluation.Assertions;

public sealed class MustBeInProjectAssertion(string projectNamePattern) : IAssertion
{
    public string Kind => "must_be_in_project";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not TypeModel type)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against types.");
        }

        return GlobMatcher.IsMatch(type.ProjectName, projectNamePattern)
            ? AssertionOutcome.Success()
            : AssertionOutcome.Failure($"'{type.FullName}' must be in a project matching '{projectNamePattern}'.");
    }
}
