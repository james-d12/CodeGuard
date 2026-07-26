using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Evaluation.Assertions;

public sealed class MustHaveAttributeAssertion(string typePattern, string? argument) : IAssertion
{
    public string Kind => "must_have_attribute";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        var attributes = AttributeAccessor.GetAttributes(candidate);
        if (attributes is null)
        {
            return AssertionOutcome.Failure($"'{Kind}' cannot be evaluated against this candidate type.");
        }

        var match = attributes.FirstOrDefault(a =>
            GlobMatcher.IsMatch(a.TypeName, typePattern) && (argument is null || AttributeAccessor.HasArgument(a, argument)));

        return match is not null
            ? AssertionOutcome.Success()
            : AssertionOutcome.Failure($"Candidate must have attribute matching '{typePattern}'{(argument is null ? "" : $" with argument '{argument}'")}.");
    }
}
