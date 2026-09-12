using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustNotReferencePackageAssertionParser : IAssertionParser
{
    public string Kind => "must_not_reference_package";

    public CapabilityDescriptor Descriptor => new(
        "must_not_reference_package",
        "Project must not reference a matching NuGet package.",
        [
            ParameterDescriptor.RequiredGlob("id", "Package ID that must not be referenced.")
        ])
    { AppliesTo = [CandidateKind.Project] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotReferencePackageAssertion(parameters.GetRequiredString("id"));
}
