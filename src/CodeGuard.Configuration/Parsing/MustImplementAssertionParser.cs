using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustImplementAssertionParser : IAssertionParser
{
    public string Kind => "must_implement";

    public IAssertion Parse(JsonObject parameters) =>
        new MustImplementAssertion(parameters.GetRequiredString("interface"));
}
