using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Evaluation.Assertions;

public sealed class MustNotHaveModifierAssertion(string modifier) : IAssertion
{
    public string Kind => "must_not_have_modifier";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        var matches = ModifierMatcher.Matches(candidate, modifier);
        return matches switch
        {
            null => AssertionOutcome.Failure($"Modifier '{modifier}' is not applicable to this candidate."),
            true => AssertionOutcome.Failure($"Candidate must not have modifier '{modifier}'."),
            false => AssertionOutcome.Success()
        };
    }
}
