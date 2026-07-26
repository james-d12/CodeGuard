using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustImplementAssertionParser : IAssertionParser
{
    public string Kind => "must_implement";

    public IAssertion Parse(JsonObject parameters) =>
        new MustImplementAssertion(parameters.GetRequiredString("interface"));
}
