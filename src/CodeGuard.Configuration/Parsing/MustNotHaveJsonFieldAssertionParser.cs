using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustNotHaveJsonFieldAssertionParser : IAssertionParser
{
    public string Kind => "must_not_have_json_field";

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotHaveJsonFieldAssertion(parameters.GetRequiredString("path"), parameters.GetOptionalString("equals"));
}
