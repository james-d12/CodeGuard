using System.Text.RegularExpressions;
using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Evaluation.Assertions;

public sealed class MustNotMatchContentAssertion(string pattern) : IAssertion
{
    public string Kind => "must_not_match_content";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not FileModel file)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against files.");
        }

        var content = File.ReadAllText(file.Path);
        return Regex.IsMatch(content, pattern)
            ? AssertionOutcome.Failure($"File '{file.RelativePath}' must not contain content matching '{pattern}'.")
            : AssertionOutcome.Success();
    }
}
