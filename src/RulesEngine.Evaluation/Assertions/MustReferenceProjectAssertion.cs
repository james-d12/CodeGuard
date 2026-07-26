using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Evaluation.Assertions;

public sealed class MustReferenceProjectAssertion(string projectNamePattern) : IAssertion
{
    public string Kind => "must_reference_project";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not ProjectModel project)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against projects.");
        }

        return project.ProjectReferences.Any(reference => GlobMatcher.IsMatch(reference, projectNamePattern))
            ? AssertionOutcome.Success()
            : AssertionOutcome.Failure($"Project '{project.Name}' must reference a project matching '{projectNamePattern}'.");
    }
}
