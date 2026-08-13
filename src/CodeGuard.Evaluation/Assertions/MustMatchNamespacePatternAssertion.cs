using System.Text.RegularExpressions;
using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Evaluation.Assertions;

/// <summary>
/// Regex complement to <c>must_match_name</c> (which matches a candidate's <c>Name</c>) - this
/// matches a type's <c>Namespace</c>, for prefix/suffix/shape rules a glob can't express (e.g.
/// "must end in .Events or .Commands").
/// </summary>
public sealed class MustMatchNamespacePatternAssertion(string regex) : IAssertion
{
    public string Kind => "must_match_namespace_pattern";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not TypeModel type)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against types.");
        }

        return Regex.IsMatch(type.Namespace, regex)
            ? AssertionOutcome.Success()
            : AssertionOutcome.Failure($"'{type.Namespace}' must match namespace pattern '{regex}'.");
    }
}
