using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Analyzers;
using RulesEngine.RuleModel.Analyzers;

namespace RulesEngine.Configuration.Parsing;

public sealed class DuplicateAttributeArgumentAnalyzerParser : IAnalyzerParser
{
    public string Kind => "duplicate-attribute-argument";

    public ICustomAnalyzer Parse(JsonObject node) => new DuplicateAttributeArgumentAnalyzer(
        node.GetOptionalString("attribute_name") ?? throw new RuleParsingException(
            "'duplicate-attribute-argument' requires an 'attribute_name' pattern."),
        node.GetOptionalInt("argument_index") ?? 0);
}
