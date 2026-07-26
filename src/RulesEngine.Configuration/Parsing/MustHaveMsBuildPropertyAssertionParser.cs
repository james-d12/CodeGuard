using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustHaveMsBuildPropertyAssertionParser : IAssertionParser
{
    public string Kind => "must_have_msbuild_property";

    public IAssertion Parse(JsonObject parameters) =>
        new MustHaveMsBuildPropertyAssertion(parameters.GetRequiredString("name"), parameters.GetOptionalString("value"));
}
