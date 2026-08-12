using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Evaluation.Assertions;

/// <summary>
/// Allow-list form of the dependency assertion family: every type the project references (via any
/// of the sites <see cref="DependencyTraversal"/> walks) must match at least one of
/// <paramref name="allowedTypePatterns"/>. Unlike <see cref="MustNotDependOnAssertion"/>'s
/// blacklist, this has no implicit BCL/framework exemption - Roslyn's default display format
/// renders primitives with their C# keyword alias (`string`, `int`, `void`, ...) rather than a
/// `System.*`-prefixed name, so a hardcoded "exclude System.*" default would silently fail to
/// exempt them. Allow-lists must therefore explicitly include the primitive/framework types the
/// project actually uses alongside its own application namespaces (see the example rule for a
/// starter list).
/// </summary>
public sealed class MustOnlyDependOnAssertion(IReadOnlyList<string> allowedTypePatterns) : IAssertion
{
    public string Kind => "must_only_depend_on";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not ProjectModel project)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against projects.");
        }

        var offendingTypeNames = project.Types
            .SelectMany(DependencyTraversal.ReferencedTypeNames)
            .Where(name => allowedTypePatterns.All(pattern => !GlobMatcher.IsMatch(name, pattern)))
            .Distinct()
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        return offendingTypeNames.Count == 0
            ? AssertionOutcome.Success()
            : AssertionOutcome.Failure(
                $"Project '{project.Name}' must only depend on types matching [{string.Join(", ", allowedTypePatterns)}] " +
                $"(disallowed: {string.Join(", ", offendingTypeNames)}).");
    }
}
