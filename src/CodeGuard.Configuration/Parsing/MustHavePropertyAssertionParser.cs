using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustHavePropertyAssertionParser : IAssertionParser
{
    public string Kind => "must_have_property";

    public IAssertion Parse(JsonObject parameters) =>
        new MustHavePropertyAssertion(parameters.GetRequiredString("name"));
}
