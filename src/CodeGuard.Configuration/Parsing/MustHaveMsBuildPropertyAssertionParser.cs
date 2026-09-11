using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustHaveMsBuildPropertyAssertionParser : IAssertionParser
{
    public string Kind => "must_have_msbuild_property";

    public CapabilityDescriptor Descriptor => new(
        "must_have_msbuild_property",
        "Project must set an MSBuild property, optionally to a specific value.",
        [
            new ParameterDescriptor("name", ParameterType.String, true, "MSBuild property name."),
            new ParameterDescriptor("value", ParameterType.String, false, "Required value. Any value is accepted when omitted.")
        ])
    { AppliesTo = [CandidateKind.Project] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustHaveMsBuildPropertyAssertion(parameters.GetRequiredString("name"), parameters.GetOptionalString("value"));
}
