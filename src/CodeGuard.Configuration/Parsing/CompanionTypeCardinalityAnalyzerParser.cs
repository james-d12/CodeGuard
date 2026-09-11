using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Analyzers;
using CodeGuard.RuleModel.Analyzers;

namespace CodeGuard.Configuration.Parsing;

public sealed class CompanionTypeCardinalityAnalyzerParser : IAnalyzerParser
{
    public string Kind => "companion-type-cardinality";

    public CapabilityDescriptor Descriptor => new(
        "companion-type-cardinality",
        "Flags types implementing a marker interface that lack exactly one matching companion type.",
        [
            ParameterDescriptor.RequiredGlob("marker_interface", "Marker interface identifying the primary type."),
            new ParameterDescriptor("companion_suffix", ParameterType.String, true, "Suffix the companion type's name must carry.")
        ]);

    public ICustomAnalyzer Parse(JsonObject node) => new CompanionTypeCardinalityAnalyzer(
        node.GetOptionalString("marker_interface") ?? throw new RuleParsingException(
            "'companion-type-cardinality' requires a 'marker_interface' pattern."),
        node.GetOptionalString("companion_suffix") ?? throw new RuleParsingException(
            "'companion-type-cardinality' requires a 'companion_suffix'."));
}
