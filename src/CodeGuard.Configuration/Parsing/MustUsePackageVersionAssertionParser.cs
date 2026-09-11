using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustUsePackageVersionAssertionParser : IAssertionParser
{
    public string Kind => "must_use_package_version";

    public CapabilityDescriptor Descriptor => new(
        "must_use_package_version",
        "Referenced package's version must satisfy a constraint. Numeric segments only; any -prerelease suffix is ignored.",
        [
            ParameterDescriptor.RequiredGlob("package", "Package ID."),
            new ParameterDescriptor("constraint", ParameterType.String, true, "Comparator (>=, <=, >, <, ==, !=; bare version means ==) plus a dotted version, e.g. >=8.0.0.")
        ])
    { AppliesTo = [CandidateKind.Project] };

    public IAssertion Parse(JsonObject parameters) => new MustUsePackageVersionAssertion(
        parameters.GetRequiredString("package"),
        parameters.GetRequiredString("constraint"));
}
