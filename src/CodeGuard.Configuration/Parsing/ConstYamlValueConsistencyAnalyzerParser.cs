using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Analyzers;
using CodeGuard.RuleModel.Analyzers;

namespace CodeGuard.Configuration.Parsing;

public sealed class ConstYamlValueConsistencyAnalyzerParser : IAnalyzerParser
{
    public string Kind => "const-yaml-value-consistency";

    public ICustomAnalyzer Parse(JsonObject node) => new ConstYamlValueConsistencyAnalyzer(
        node.GetOptionalString("const_type") ?? throw new RuleParsingException(
            "'const-yaml-value-consistency' requires a 'const_type' pattern."),
        node.GetOptionalString("const_name") ?? throw new RuleParsingException(
            "'const-yaml-value-consistency' requires a 'const_name'."),
        node.GetOptionalString("yaml_file_pattern") ?? throw new RuleParsingException(
            "'const-yaml-value-consistency' requires a 'yaml_file_pattern'."),
        node.GetOptionalString("yaml_field_path") ?? throw new RuleParsingException(
            "'const-yaml-value-consistency' requires a 'yaml_field_path'."));
}
