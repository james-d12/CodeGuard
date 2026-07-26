using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustHaveParameterCountAssertionParser : IAssertionParser
{
    public string Kind => "must_have_parameter_count";

    public IAssertion Parse(JsonObject parameters) =>
        new MustHaveParameterCountAssertion(parameters.GetOptionalInt("min"), parameters.GetOptionalInt("max"));
}
