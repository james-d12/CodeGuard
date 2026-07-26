using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustNotHaveMethodAssertionParser : IAssertionParser
{
    public string Kind => "must_not_have_method";

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotHaveMethodAssertion(parameters.GetRequiredString("name"));
}
