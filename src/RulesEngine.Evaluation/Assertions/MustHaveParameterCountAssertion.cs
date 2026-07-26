using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Evaluation.Assertions;

public sealed class MustHaveParameterCountAssertion(int? min, int? max) : IAssertion
{
    public string Kind => "must_have_parameter_count";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        var count = candidate switch
        {
            MethodModel method => method.Parameters.Count,
            ConstructorModel constructor => constructor.Parameters.Count,
            _ => (int?)null
        };

        if (count is null)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against methods or constructors.");
        }

        if (min is not null && count < min)
        {
            return AssertionOutcome.Failure($"Must have at least {min} parameter(s), found {count}.");
        }

        if (max is not null && count > max)
        {
            return AssertionOutcome.Failure($"Must have at most {max} parameter(s), found {count}.");
        }

        return AssertionOutcome.Success();
    }
}
