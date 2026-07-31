using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustHaveMethodAssertionParser : IAssertionParser
{
    public string Kind => "must_have_method";

    public IAssertion Parse(JsonObject parameters) =>
        new MustHaveMethodAssertion(parameters.GetRequiredString("name"));
}
