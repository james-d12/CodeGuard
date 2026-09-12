using CodeGuard.Configuration.Capabilities;
using CodeGuard.Configuration.Validation;
using CodeGuard.Evaluation.Analyzers;
using CodeGuard.RuleModel.Analyzers;
using System.Text.Json.Nodes;

namespace CodeGuard.Configuration.Parsing;

public sealed class ConstYamlValueConsistencyAnalyzerParser : IAnalyzerParser
{
    public string Kind => "const-yaml-value-consistency";

    public CapabilityDescriptor Descriptor => new(
        "const-yaml-value-consistency",
        "Cross-checks a C# const against a single field in a YAML file. The only YAML-aware check; there is no generic YAML field assertion.",
        [
            ParameterDescriptor.RequiredGlob("const_type", "Type declaring the const."),
            new ParameterDescriptor("const_name", ParameterType.String, true, "Const field name."),
            ParameterDescriptor.RequiredGlob("yaml_file_pattern", "YAML file to read."),
            new ParameterDescriptor("yaml_field_path", ParameterType.String, true, "Dotted path to the YAML field.")
        ]);

    public ICustomAnalyzer Parse(JsonObject node) => new ConstYamlValueConsistencyAnalyzer(
        node.GetOptionalString("const_type") ?? throw new RuleParsingException(
                "'const-yaml-value-consistency' requires a 'const_type' pattern.",
                RuleErrorCodes.InvalidParameter, "/const_type"),
        node.GetOptionalString("const_name") ?? throw new RuleParsingException(
                "'const-yaml-value-consistency' requires a 'const_name'.",
                RuleErrorCodes.InvalidParameter, "/const_name"),
        node.GetOptionalString("yaml_file_pattern") ?? throw new RuleParsingException(
                "'const-yaml-value-consistency' requires a 'yaml_file_pattern'.",
                RuleErrorCodes.InvalidParameter, "/yaml_file_pattern"),
        node.GetOptionalString("yaml_field_path") ?? throw new RuleParsingException(
                "'const-yaml-value-consistency' requires a 'yaml_field_path'.",
                RuleErrorCodes.InvalidParameter, "/yaml_field_path"));
}
