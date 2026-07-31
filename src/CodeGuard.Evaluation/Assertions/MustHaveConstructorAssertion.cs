using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Evaluation.Assertions;

public sealed class MustHaveConstructorAssertion(IReadOnlyList<Accessibility> allowedAccessibilities) : IAssertion
{
    public string Kind => "must_have_constructor";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not TypeModel type)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against types.");
        }

        return type.Constructors.Any(c => allowedAccessibilities.Contains(c.Accessibility))
            ? AssertionOutcome.Success()
            : AssertionOutcome.Failure(
                $"'{type.FullName}' must have a constructor with accessibility {string.Join(" or ", allowedAccessibilities)}.");
    }
}
