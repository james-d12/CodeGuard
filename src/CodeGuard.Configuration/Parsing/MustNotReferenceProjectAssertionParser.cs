using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustNotReferenceProjectAssertionParser : IAssertionParser
{
    public string Kind => "must_not_reference_project";

    public CapabilityDescriptor Descriptor => new(
        "must_not_reference_project",
        "Project must not reference a matching project.",
        [
            ParameterDescriptor.RequiredGlob("name", "Project name that must not be referenced.")
        ])
    { AppliesTo = [CandidateKind.Project] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotReferenceProjectAssertion(parameters.GetRequiredString("name"));
}
