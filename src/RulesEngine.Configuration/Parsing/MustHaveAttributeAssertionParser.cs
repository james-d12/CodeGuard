using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustHaveAttributeAssertionParser : IAssertionParser
{
    public string Kind => "must_have_attribute";

    public IAssertion Parse(JsonObject parameters) =>
        new MustHaveAttributeAssertion(parameters.GetRequiredString("type"), parameters.GetOptionalString("argument"));
}
