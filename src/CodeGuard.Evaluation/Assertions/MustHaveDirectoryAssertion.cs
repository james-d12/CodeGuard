using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Evaluation.Assertions;

public sealed class MustHaveDirectoryAssertion(string path) : IAssertion
{
    public string Kind => "must_have_directory";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not RepositoryModel repository)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against the repository.");
        }

        return repository.Directories.Any(d => GlobMatcher.IsMatch(d, path))
            ? AssertionOutcome.Success()
            : AssertionOutcome.Failure($"Repository must have a directory at '{path}'.");
    }
}
