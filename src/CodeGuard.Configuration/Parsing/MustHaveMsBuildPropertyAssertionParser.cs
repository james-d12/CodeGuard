using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustHaveMsBuildPropertyAssertionParser : IAssertionParser
{
    public string Kind => "must_have_msbuild_property";

    public IAssertion Parse(JsonObject parameters) =>
        new MustHaveMsBuildPropertyAssertion(parameters.GetRequiredString("name"), parameters.GetOptionalString("value"));
}
