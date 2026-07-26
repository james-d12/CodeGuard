using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Evaluation.Assertions;

public sealed class MustNotHavePropertyAssertion(string namePattern) : IAssertion
{
    public string Kind => "must_not_have_property";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not TypeModel type)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against types.");
        }

        var match = type.Properties.FirstOrDefault(p => GlobMatcher.IsMatch(p.Name, namePattern));
        return match is null
            ? AssertionOutcome.Success()
            : AssertionOutcome.Failure($"'{type.FullName}' must not have a property matching '{namePattern}' (found '{match.Name}').");
    }
}
