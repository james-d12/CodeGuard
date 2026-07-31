using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Conditions;

namespace CodeGuard.Evaluation.Tests.Conditions;

public class ConditionCompositionTests
{
    private static readonly RepositoryModel EmptyModel = new(".", [], [], [], [], [], [], [], [], []);

    private sealed class FixedCondition(bool result) : IConditionNode
    {
        public bool Evaluate(object candidate, RepositoryModel model) => result;
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, false, false)]
    public void AndCondition_RequiresAllChildrenToPass(bool a, bool b, bool expected)
    {
        var condition = new AndCondition([new FixedCondition(a), new FixedCondition(b)]);
        Assert.Equal(expected, condition.Evaluate(new object(), EmptyModel));
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, false, false)]
    [InlineData(false, true, true)]
    public void OrCondition_PassesWhenAnyChildPasses(bool a, bool b, bool expected)
    {
        var condition = new OrCondition([new FixedCondition(a), new FixedCondition(b)]);
        Assert.Equal(expected, condition.Evaluate(new object(), EmptyModel));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void NotCondition_NegatesChild(bool input, bool expected)
    {
        var condition = new NotCondition(new FixedCondition(input));
        Assert.Equal(expected, condition.Evaluate(new object(), EmptyModel));
    }

    [Fact]
    public void Conditions_CanBeNestedArbitrarily()
    {
        // (true AND NOT false) OR false => true
        var condition = new OrCondition(
        [
            new AndCondition([new FixedCondition(true), new NotCondition(new FixedCondition(false))]),
            new FixedCondition(false)
        ]);

        Assert.True(condition.Evaluate(new object(), EmptyModel));
    }
}
