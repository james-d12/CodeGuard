using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Evaluation.Assertions;

public sealed class MustHaveDirectoryAssertion(string path) : IAssertion
{
    public string Kind => "must_have_directory";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not RepositoryModel repository)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against the repository.");
        }

        return Directory.Exists(Path.Combine(repository.RootPath, path))
            ? AssertionOutcome.Success()
            : AssertionOutcome.Failure($"Repository must have a directory at '{path}'.");
    }
}
