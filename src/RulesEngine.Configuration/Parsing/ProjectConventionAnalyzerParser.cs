using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Analyzers;
using RulesEngine.RuleModel.Analyzers;

namespace RulesEngine.Configuration.Parsing;

public sealed class ProjectConventionAnalyzerParser : IAnalyzerParser
{
    public string Kind => "project-convention";

    public ICustomAnalyzer Parse(JsonObject node) => new ProjectConventionAnalyzer(
        node.GetOptionalString("project_pattern") ?? throw new RuleParsingException(
            "'project-convention' requires a 'project_pattern'."),
        node.GetOptionalString("required_call_pattern") ?? "*DeployChanges*",
        node.GetOptionalString("required_content_folder") ?? "Scripts");
}
