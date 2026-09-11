using CodeGuard.Configuration.Capabilities;
using CodeGuard.Configuration.Validation;
using CodeGuard.Evaluation.Analyzers;
using CodeGuard.RuleModel.Analyzers;
using System.Text.Json.Nodes;

namespace CodeGuard.Configuration.Parsing;

public sealed class ProjectConventionAnalyzerParser : IAnalyzerParser
{
    public string Kind => "project-convention";

    public CapabilityDescriptor Descriptor => new(
        "project-convention",
        "Flags projects matching a pattern that lack a required call site or content folder.",
        [
            ParameterDescriptor.RequiredGlob("project_pattern", "Projects to inspect."),
            ParameterDescriptor.OptionalGlob("required_call_pattern", "Call site the project must contain.", "*DeployChanges*"),
            ParameterDescriptor.OptionalGlob("required_content_folder", "Folder the project must contain.", "Scripts")
        ]);

    public ICustomAnalyzer Parse(JsonObject node) => new ProjectConventionAnalyzer(
        node.GetOptionalString("project_pattern") ?? throw new RuleParsingException(
                "'project-convention' requires a 'project_pattern'.",
                RuleErrorCodes.InvalidParameter, "/project_pattern"),
        node.GetOptionalString("required_call_pattern") ?? "*DeployChanges*",
        node.GetOptionalString("required_content_folder") ?? "Scripts");
}
