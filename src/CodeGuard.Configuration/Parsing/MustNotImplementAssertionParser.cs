using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustNotImplementAssertionParser : IAssertionParser
{
    public string Kind => "must_not_implement";

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotImplementAssertion(parameters.GetRequiredString("interface"));
}
