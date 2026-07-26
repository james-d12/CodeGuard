using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustHaveJsonFieldAssertionParser : IAssertionParser
{
    public string Kind => "must_have_json_field";

    public IAssertion Parse(JsonObject parameters) =>
        new MustHaveJsonFieldAssertion(parameters.GetRequiredString("path"), parameters.GetOptionalString("equals"));
}
