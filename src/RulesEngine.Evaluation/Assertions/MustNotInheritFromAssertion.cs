using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Evaluation.Assertions;

public sealed class MustNotInheritFromAssertion(string baseTypePattern) : IAssertion
{
    public string Kind => "must_not_inherit_from";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not TypeModel type)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against types.");
        }

        return type.BaseType is not null && GlobMatcher.IsMatch(type.BaseType, baseTypePattern)
            ? AssertionOutcome.Failure($"'{type.FullName}' must not inherit from '{baseTypePattern}'.")
            : AssertionOutcome.Success();
    }
}
