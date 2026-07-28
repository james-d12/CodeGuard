using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Analyzers;
using RulesEngine.RuleModel.Analyzers;

namespace RulesEngine.Configuration.Parsing;

public sealed class CompanionTypeCardinalityAnalyzerParser : IAnalyzerParser
{
    public string Kind => "companion-type-cardinality";

    public ICustomAnalyzer Parse(JsonObject node) => new CompanionTypeCardinalityAnalyzer(
        node.GetOptionalString("marker_interface") ?? throw new RuleParsingException(
            "'companion-type-cardinality' requires a 'marker_interface' pattern."),
        node.GetOptionalString("companion_suffix") ?? throw new RuleParsingException(
            "'companion-type-cardinality' requires a 'companion_suffix'."));
}
