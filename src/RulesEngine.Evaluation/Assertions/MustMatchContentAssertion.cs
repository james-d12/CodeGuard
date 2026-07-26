using System.Text.RegularExpressions;
using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Evaluation.Assertions;

public sealed class MustMatchContentAssertion(string pattern) : IAssertion
{
    public string Kind => "must_match_content";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not FileModel file)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against files.");
        }

        var content = File.ReadAllText(file.Path);
        return Regex.IsMatch(content, pattern)
            ? AssertionOutcome.Success()
            : AssertionOutcome.Failure($"File '{file.RelativePath}' must contain content matching '{pattern}'.");
    }
}
