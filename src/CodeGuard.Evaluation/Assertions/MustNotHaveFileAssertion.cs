using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Evaluation.Assertions;

public sealed class MustNotHaveFileAssertion(string pathPattern) : IAssertion
{
    public string Kind => "must_not_have_file";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not RepositoryModel repository)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against the repository.");
        }

        var match = repository.Files.FirstOrDefault(f => GlobMatcher.IsMatch(f.RelativePath, pathPattern));
        return match is null
            ? AssertionOutcome.Success()
            : AssertionOutcome.Failure($"Repository must not have a file matching '{pathPattern}' (found '{match.RelativePath}').");
    }
}
