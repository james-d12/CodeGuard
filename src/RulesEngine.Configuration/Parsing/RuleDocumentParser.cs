using System.Text.Json.Nodes;
using RulesEngine.RuleModel.Rules;

namespace RulesEngine.Configuration.Parsing;

public static class RuleDocumentParser
{
    public static RuleDefinition Parse(
        JsonObject document,
        SelectorParserRegistry selectorParsers,
        AssertionParserRegistry assertionParsers,
        ConditionParserRegistry conditionParsers)
    {
        var targetNode = document["target"]?.AsObject()
            ?? throw new RuleParsingException("Rule is missing required 'target'.");
        var assertionsNode = document["assertions"]?.AsArray()
            ?? throw new RuleParsingException("Rule is missing required 'assertions'.");

        return new RuleDefinition
        {
            Id = document.GetRequiredString("id"),
            Name = document.GetRequiredString("name"),
            Description = document.GetOptionalString("description"),
            Standard = document.GetOptionalString("standard"),
            Severity = ParseSeverity(document),
            Enforcement = new EnforcementMetadata { Classification = ParseClassification(document) },
            Tags = document.GetStringArray("tags"),
            Remediation = document.GetOptionalString("remediation"),
            Documentation = document.GetStringArray("documentation"),
            Enabled = document.GetOptionalBool("enabled", true),
            Illustrative = document.GetOptionalBool("illustrative", false),
            Target = selectorParsers.Parse(targetNode),
            When = document["when"]?.AsObject() is { } whenNode ? conditionParsers.Parse(whenNode) : null,
            Assertions = assertionsNode.Select(node => assertionParsers.Parse(node!.AsObject())).ToList()
        };
    }

    private static Severity ParseSeverity(JsonObject document) =>
        document.GetOptionalString("severity") is { } value
            ? EnumParsing.ParseSnakeCase<Severity>(value)
            : Severity.Warning;

    private static EnforcementClassification ParseClassification(JsonObject document) =>
        document["enforcement"]?.AsObject().GetOptionalString("classification") is { } value
            ? EnumParsing.ParseSnakeCase<EnforcementClassification>(value)
            : EnforcementClassification.Deterministic;
}
