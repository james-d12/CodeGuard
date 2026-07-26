using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Evaluation.Assertions;

public sealed class MustHaveFileAssertion(string pathPattern) : IAssertion
{
    public string Kind => "must_have_file";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not RepositoryModel repository)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against the repository.");
        }

        return repository.Files.Any(f => GlobMatcher.IsMatch(f.RelativePath, pathPattern))
            ? AssertionOutcome.Success()
            : AssertionOutcome.Failure($"Repository must have a file matching '{pathPattern}'.");
    }
}
