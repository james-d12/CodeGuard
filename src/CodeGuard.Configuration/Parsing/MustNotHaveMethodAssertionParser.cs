using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustNotHaveMethodAssertionParser : IAssertionParser
{
    public string Kind => "must_not_have_method";

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotHaveMethodAssertion(parameters.GetRequiredString("name"));
}
