using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustNotHavePropertyAssertionParser : IAssertionParser
{
    public string Kind => "must_not_have_property";

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotHavePropertyAssertion(parameters.GetRequiredString("name"));
}
