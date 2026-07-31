using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustNotHaveModifierAssertionParser : IAssertionParser
{
    public string Kind => "must_not_have_modifier";

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotHaveModifierAssertion(parameters.GetRequiredString("modifier"));
}
