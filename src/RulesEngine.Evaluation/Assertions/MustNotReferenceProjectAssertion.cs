using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Evaluation.Assertions;

public sealed class MustNotReferenceProjectAssertion(string projectNamePattern) : IAssertion
{
    public string Kind => "must_not_reference_project";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not ProjectModel project)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against projects.");
        }

        var match = project.ProjectReferences.FirstOrDefault(reference => GlobMatcher.IsMatch(reference, projectNamePattern));
        return match is null
            ? AssertionOutcome.Success()
            : AssertionOutcome.Failure($"Project '{project.Name}' must not reference project '{match}'.");
    }
}
