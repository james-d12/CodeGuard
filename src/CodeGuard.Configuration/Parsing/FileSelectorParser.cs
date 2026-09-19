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
        "Repository files by path, extension, and/or basename.",
        [
            ParameterDescriptor.OptionalGlob("path", "Repository-relative file path.", @default: "**"),
            new ParameterDescriptor("extension", ParameterType.String, false, "File extension, including the leading dot."),
            ParameterDescriptor.OptionalGlob("name", "File's basename (name + extension), matched independently of 'path' - e.g. path: \"**/Handlers/*\" with name: \"*Handler.cs\".", @default: "(none)")
        ])
    { Produces = CandidateKind.File };

    public ITargetSelector Parse(JsonObject node) =>
        new FileSelector(
            node.GetOptionalString("path") ?? "**",
            node.GetOptionalString("extension"),
            node.GetOptionalString("name"));
}
