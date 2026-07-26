using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustNotImplementAssertionParser : IAssertionParser
{
    public string Kind => "must_not_implement";

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotImplementAssertion(parameters.GetRequiredString("interface"));
}
