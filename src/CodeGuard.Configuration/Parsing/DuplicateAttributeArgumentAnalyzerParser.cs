using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Analyzers;
using CodeGuard.RuleModel.Analyzers;

namespace CodeGuard.Configuration.Parsing;

public sealed class DuplicateAttributeArgumentAnalyzerParser : IAnalyzerParser
{
    public string Kind => "duplicate-attribute-argument";

    public CapabilityDescriptor Descriptor => new(
        "duplicate-attribute-argument",
        "Flags a repeated argument value across all uses of an attribute.",
        [
            ParameterDescriptor.RequiredGlob("attribute_name", "Attribute type to inspect."),
            new ParameterDescriptor("argument_index", ParameterType.Int, false, "Zero-based index of the argument compared for duplicates.", Default: "0")
        ]);

    public ICustomAnalyzer Parse(JsonObject node) => new DuplicateAttributeArgumentAnalyzer(
        node.GetOptionalString("attribute_name") ?? throw new RuleParsingException(
            "'duplicate-attribute-argument' requires an 'attribute_name' pattern."),
        node.GetOptionalInt("argument_index") ?? 0);
}
