using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustNotHaveAttributeAssertionParser : IAssertionParser
{
    public string Kind => "must_not_have_attribute";

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotHaveAttributeAssertion(parameters.GetRequiredString("type"), parameters.GetOptionalString("argument"));
}
