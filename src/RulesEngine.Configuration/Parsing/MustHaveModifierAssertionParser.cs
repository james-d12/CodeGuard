using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustHaveModifierAssertionParser : IAssertionParser
{
    public string Kind => "must_have_modifier";

    public IAssertion Parse(JsonObject parameters) =>
        new MustHaveModifierAssertion(parameters.GetRequiredString("modifier"));
}
