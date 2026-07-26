using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Evaluation.Assertions;

public sealed class MustHaveMsBuildPropertyAssertion(string name, string? value) : IAssertion
{
    public string Kind => "must_have_msbuild_property";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not ProjectModel project)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against projects.");
        }

        if (!project.Properties.TryGetValue(name, out var actualValue))
        {
            return AssertionOutcome.Failure($"Project '{project.Name}' must define MSBuild property '{name}'.");
        }

        if (value is not null && !string.Equals(actualValue, value, StringComparison.OrdinalIgnoreCase))
        {
            return AssertionOutcome.Failure(
                $"Project '{project.Name}' must have MSBuild property '{name}' set to '{value}' (found '{actualValue}').");
        }

        return AssertionOutcome.Success();
    }
}
