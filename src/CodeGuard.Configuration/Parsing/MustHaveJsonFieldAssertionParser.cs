using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustHaveJsonFieldAssertionParser : IAssertionParser
{
    public string Kind => "must_have_json_field";

    public IAssertion Parse(JsonObject parameters) =>
        new MustHaveJsonFieldAssertion(parameters.GetRequiredString("path"), parameters.GetOptionalString("equals"));
}
