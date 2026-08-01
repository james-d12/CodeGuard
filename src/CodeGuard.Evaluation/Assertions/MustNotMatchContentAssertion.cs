using System.Text.RegularExpressions;
using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Evaluation.Assertions;

public sealed class MustNotMatchContentAssertion(string pattern) : IAssertion
{
    public string Kind => "must_not_match_content";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not FileModel file)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against files.");
        }

        var content = file.Content ?? File.ReadAllText(file.Path);
        return Regex.IsMatch(content, pattern)
            ? AssertionOutcome.Failure($"File '{file.RelativePath}' must not contain content matching '{pattern}'.")
            : AssertionOutcome.Success();
    }
}
