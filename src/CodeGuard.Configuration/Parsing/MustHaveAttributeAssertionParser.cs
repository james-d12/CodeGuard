using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustHaveAttributeAssertionParser : IAssertionParser
{
    public string Kind => "must_have_attribute";

    public IAssertion Parse(JsonObject parameters) =>
        new MustHaveAttributeAssertion(parameters.GetRequiredString("type"), parameters.GetOptionalString("argument"));
}
