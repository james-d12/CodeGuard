using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustHaveParameterCountAssertionParser : IAssertionParser
{
    public string Kind => "must_have_parameter_count";

    public IAssertion Parse(JsonObject parameters) =>
        new MustHaveParameterCountAssertion(parameters.GetOptionalInt("min"), parameters.GetOptionalInt("max"));
}
