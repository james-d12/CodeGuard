using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Evaluation.Assertions;

public sealed class MustReferencePackageAssertion(string packageIdPattern) : IAssertion
{
    public string Kind => "must_reference_package";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not ProjectModel project)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against projects.");
        }

        return project.PackageReferences.Any(p => GlobMatcher.IsMatch(p.Id, packageIdPattern))
            ? AssertionOutcome.Success()
            : AssertionOutcome.Failure($"Project '{project.Name}' must reference package matching '{packageIdPattern}'.");
    }
}
