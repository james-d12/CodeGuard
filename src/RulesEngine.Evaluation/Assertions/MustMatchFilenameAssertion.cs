using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Evaluation.Assertions;

public sealed class MustMatchFilenameAssertion : IAssertion
{
    public string Kind => "must_match_filename";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not TypeModel type)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against types.");
        }

        var expectedName = Path.GetFileNameWithoutExtension(type.FilePath);
        return type.Name == expectedName
            ? AssertionOutcome.Success()
            : AssertionOutcome.Failure($"'{type.FullName}' must be declared in a file named '{type.Name}.cs' (found '{Path.GetFileName(type.FilePath)}').");
    }
}
