using System.Text.RegularExpressions;
using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Evaluation.Assertions;

public sealed class MustMatchArgumentAssertion(int index, string pattern) : IAssertion
{
    public string Kind => "must_match_argument";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not CallSiteModel callSite)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against call sites.");
        }

        var argument = callSite.Arguments.FirstOrDefault(a => a.Index == index);
        if (argument is null || !argument.IsLiteral || argument.LiteralValue is null)
        {
            return AssertionOutcome.Failure($"Call site '{callSite.InvokedMember}' must have a literal argument at index {index}.");
        }

        return Regex.IsMatch(argument.LiteralValue, pattern)
            ? AssertionOutcome.Success()
            : AssertionOutcome.Failure($"Argument {index} of call site '{callSite.InvokedMember}' must match '{pattern}' (found '{argument.LiteralValue}').");
    }
}
