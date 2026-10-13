using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Evaluation.Assertions;

public sealed class MustNotDependOnAssertion(string forbiddenTypePattern) : IAssertion
{
    public string Kind => "must_not_depend_on";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not ProjectModel project)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against projects.");
        }

        var offendingTypes = project.Types
            .Where(type => DependencyTraversal.ReferencedTypeNames(type).Any(name => GlobMatcher.IsMatch(name, forbiddenTypePattern)))
            .Select(type => type.FullName)
            .ToList();

        if (offendingTypes.Count == 0)
        {
            return AssertionOutcome.Success();
        }

        var matchedNames = project.Types
            .SelectMany(DependencyTraversal.ReferencedTypeNames)
            .Where(name => GlobMatcher.IsMatch(name, forbiddenTypePattern))
            .Distinct()
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        return AssertionOutcome.Failure(
            $"Project '{project.Name}' must not depend on '{forbiddenTypePattern}' (via: {string.Join(", ", offendingTypes)}).",
            $"no dependency on '{forbiddenTypePattern}'",
            string.Join(", ", CandidateDescriptor.CapEvidence(matchedNames)),
            CandidateDescriptor.CapEvidence(offendingTypes));
    }
}
