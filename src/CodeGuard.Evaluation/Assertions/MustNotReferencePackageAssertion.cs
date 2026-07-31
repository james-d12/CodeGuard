using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Evaluation.Assertions;

public sealed class MustNotReferencePackageAssertion(string packageIdPattern) : IAssertion
{
    public string Kind => "must_not_reference_package";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not ProjectModel project)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against projects.");
        }

        var match = project.PackageReferences.FirstOrDefault(p => GlobMatcher.IsMatch(p.Id, packageIdPattern));
        return match is null
            ? AssertionOutcome.Success()
            : AssertionOutcome.Failure($"Project '{project.Name}' must not reference package '{match.Id}'.");
    }
}
