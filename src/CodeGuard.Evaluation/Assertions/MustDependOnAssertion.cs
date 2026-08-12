using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Evaluation.Assertions;

/// <summary>
/// Positive counterpart to <see cref="MustNotDependOnAssertion"/>: the project must reference at
/// least one type matching <paramref name="requiredTypePattern"/>, via any of the type-reference
/// sites <see cref="DependencyTraversal"/> walks (base type, interfaces, attributes, member
/// return/parameter/property/field types).
/// </summary>
public sealed class MustDependOnAssertion(string requiredTypePattern) : IAssertion
{
    public string Kind => "must_depend_on";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not ProjectModel project)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against projects.");
        }

        var hasMatch = project.Types
            .SelectMany(DependencyTraversal.ReferencedTypeNames)
            .Any(name => GlobMatcher.IsMatch(name, requiredTypePattern));

        return hasMatch
            ? AssertionOutcome.Success()
            : AssertionOutcome.Failure($"Project '{project.Name}' must depend on a type matching '{requiredTypePattern}'.");
    }
}
