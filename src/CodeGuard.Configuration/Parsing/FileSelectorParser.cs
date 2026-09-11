using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class FileSelectorParser : ISelectorParser
{
    public string Kind => "file";

    public CapabilityDescriptor Descriptor => new(
        "file",
        "Repository files by path and/or extension.",
        [
            ParameterDescriptor.OptionalGlob("path", "Repository-relative file path."),
            new ParameterDescriptor("extension", ParameterType.String, false, "File extension, including the leading dot.")
        ])
    { Produces = CandidateKind.File };

    public ITargetSelector Parse(JsonObject node) =>
        new FileSelector(node.GetOptionalString("path") ?? "*", node.GetOptionalString("extension"));
}
