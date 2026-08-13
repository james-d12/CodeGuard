using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Evaluation.Assertions;

public sealed class MustNotBeInNamespaceAssertion(string namespacePattern) : IAssertion
{
    public string Kind => "must_not_be_in_namespace";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not TypeModel type)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against types.");
        }

        return GlobMatcher.IsMatch(type.Namespace, namespacePattern)
            ? AssertionOutcome.Failure($"'{type.FullName}' must not be in a namespace matching '{namespacePattern}'.")
            : AssertionOutcome.Success();
    }
}
