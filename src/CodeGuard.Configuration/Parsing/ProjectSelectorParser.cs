using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class ProjectSelectorParser : ISelectorParser
{
    public string Kind => "project";

    public CapabilityDescriptor Descriptor => new(
        "project",
        "Projects with a matching name.",
        [
            ParameterDescriptor.RequiredGlob("name", "Project name, without the .csproj extension.")
        ])
    { Produces = CandidateKind.Project };

    public ITargetSelector Parse(JsonObject node) =>
        new ProjectSelector(node.GetRequiredString("name"));
}
