using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustReferenceProjectAssertionParser : IAssertionParser
{
    public string Kind => "must_reference_project";

    public CapabilityDescriptor Descriptor => new(
        "must_reference_project",
        "Project must reference a matching project.",
        [
            ParameterDescriptor.RequiredGlob("name", "Referenced project name.")
        ])
    { AppliesTo = [CandidateKind.Project] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustReferenceProjectAssertion(parameters.GetRequiredString("name"));
}
