using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class DirectorySelectorParser : ISelectorParser
{
    public string Kind => "directory";

    public CapabilityDescriptor Descriptor => new(
        "directory",
        "Repository directories by path.",
        [
            ParameterDescriptor.OptionalGlob("path", "Repository-relative directory path.", @default: "**")
        ])
    { Produces = CandidateKind.Directory };

    public ITargetSelector Parse(JsonObject node) => new DirectorySelector(
        node.GetOptionalString("path") ?? "**");
}
