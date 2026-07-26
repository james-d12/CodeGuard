using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustNotHaveJsonFieldAssertionParser : IAssertionParser
{
    public string Kind => "must_not_have_json_field";

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotHaveJsonFieldAssertion(parameters.GetRequiredString("path"), parameters.GetOptionalString("equals"));
}
