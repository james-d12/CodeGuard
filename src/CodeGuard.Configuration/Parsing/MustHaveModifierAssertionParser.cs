using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustHaveModifierAssertionParser : IAssertionParser
{
    public string Kind => "must_have_modifier";

    public IAssertion Parse(JsonObject parameters) =>
        new MustHaveModifierAssertion(parameters.GetRequiredString("modifier"));
}
