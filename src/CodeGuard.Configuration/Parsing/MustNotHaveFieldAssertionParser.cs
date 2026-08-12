using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustNotHaveFieldAssertionParser : IAssertionParser
{
    public string Kind => "must_not_have_field";

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotHaveFieldAssertion(parameters.GetRequiredString("name"));
}
