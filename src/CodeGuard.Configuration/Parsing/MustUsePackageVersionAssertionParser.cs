using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustUsePackageVersionAssertionParser : IAssertionParser
{
    public string Kind => "must_use_package_version";

    public IAssertion Parse(JsonObject parameters) => new MustUsePackageVersionAssertion(
        parameters.GetRequiredString("package"),
        parameters.GetRequiredString("constraint"));
}
