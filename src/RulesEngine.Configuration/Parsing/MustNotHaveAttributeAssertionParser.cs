using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustNotHaveAttributeAssertionParser : IAssertionParser
{
    public string Kind => "must_not_have_attribute";

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotHaveAttributeAssertion(parameters.GetRequiredString("type"), parameters.GetOptionalString("argument"));
}
