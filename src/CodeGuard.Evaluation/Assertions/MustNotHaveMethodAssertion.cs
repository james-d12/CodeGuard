using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Evaluation.Assertions;

public sealed class MustNotHaveMethodAssertion(string namePattern) : IAssertion
{
    public string Kind => "must_not_have_method";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not TypeModel type)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against types.");
        }

        var match = type.Methods.FirstOrDefault(m => GlobMatcher.IsMatch(m.Name, namePattern));
        return match is null
            ? AssertionOutcome.Success()
            : AssertionOutcome.Failure($"'{type.FullName}' must not have a method matching '{namePattern}' (found '{match.Name}').");
    }
}
