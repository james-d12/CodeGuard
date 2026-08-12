using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustHaveFieldAssertionParser : IAssertionParser
{
    public string Kind => "must_have_field";

    public IAssertion Parse(JsonObject parameters) =>
        new MustHaveFieldAssertion(parameters.GetRequiredString("name"));
}
