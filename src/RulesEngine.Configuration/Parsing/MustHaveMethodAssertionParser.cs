using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustHaveMethodAssertionParser : IAssertionParser
{
    public string Kind => "must_have_method";

    public IAssertion Parse(JsonObject parameters) =>
        new MustHaveMethodAssertion(parameters.GetRequiredString("name"));
}
