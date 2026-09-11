using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustReferencePackageAssertionParser : IAssertionParser
{
    public string Kind => "must_reference_package";

    public CapabilityDescriptor Descriptor => new(
        "must_reference_package",
        "Project must reference a matching NuGet package.",
        [
            ParameterDescriptor.RequiredGlob("id", "Package ID.")
        ])
    { AppliesTo = [CandidateKind.Project] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustReferencePackageAssertion(parameters.GetRequiredString("id"));
}
