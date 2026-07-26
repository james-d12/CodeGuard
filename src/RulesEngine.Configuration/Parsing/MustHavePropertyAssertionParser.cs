using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustHavePropertyAssertionParser : IAssertionParser
{
    public string Kind => "must_have_property";

    public IAssertion Parse(JsonObject parameters) =>
        new MustHavePropertyAssertion(parameters.GetRequiredString("name"));
}
