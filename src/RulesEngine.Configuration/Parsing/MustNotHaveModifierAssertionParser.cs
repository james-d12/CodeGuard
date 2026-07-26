using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustNotHaveModifierAssertionParser : IAssertionParser
{
    public string Kind => "must_not_have_modifier";

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotHaveModifierAssertion(parameters.GetRequiredString("modifier"));
}
