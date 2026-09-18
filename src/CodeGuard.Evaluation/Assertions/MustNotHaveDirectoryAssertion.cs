using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Evaluation.Assertions;

public sealed class MustNotHaveDirectoryAssertion(string path) : IAssertion
{
    public string Kind => "must_not_have_directory";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not RepositoryModel repository)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against the repository.");
        }

        var match = repository.Directories.FirstOrDefault(d => GlobMatcher.IsMatch(d.RelativePath, path));
        return match is null
            ? AssertionOutcome.Success()
            : AssertionOutcome.Failure($"Repository must not have a directory matching '{path}' (found '{match.RelativePath}').");
    }
}
