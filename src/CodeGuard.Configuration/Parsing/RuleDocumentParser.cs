using System.Text.Json.Nodes;
using CodeGuard.RuleModel.Analyzers;
using CodeGuard.RuleModel.Assertions;
using CodeGuard.RuleModel.Conditions;
using CodeGuard.RuleModel.Rules;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public static class RuleDocumentParser
{
    public static RuleDefinition Parse(
        JsonObject document,
        SelectorParserRegistry selectorParsers,
        AssertionParserRegistry assertionParsers,
        ConditionParserRegistry conditionParsers,
        AnalyzerParserRegistry analyzerParsers)
    {
        ICustomAnalyzer? analyzer = null;
        ITargetSelector? target = null;
        IConditionNode? when = null;
        IReadOnlyList<IAssertion>? assertions = null;

        if (document["analyzer"]?.AsObject() is { } analyzerNode)
        {
            if (document["target"] is not null || document["assertions"] is not null)
            {
                throw new RuleParsingException("A rule cannot specify both 'analyzer' and 'target'/'assertions'.");
            }

            analyzer = analyzerParsers.Parse(analyzerNode);
        }
        else
        {
            var targetNode = document["target"]?.AsObject()
                ?? throw new RuleParsingException("Rule is missing required 'target'.");
            var assertionsNode = document["assertions"]?.AsArray()
                ?? throw new RuleParsingException("Rule is missing required 'assertions'.");

            target = selectorParsers.Parse(targetNode);
            when = document["when"]?.AsObject() is { } whenNode ? conditionParsers.Parse(whenNode) : null;
            assertions = assertionsNode.Select(node => assertionParsers.Parse(node!.AsObject())).ToList();
        }

        return new RuleDefinition
        {
            Id = document.GetRequiredString("id"),
            Name = document.GetRequiredString("name"),
            Description = document.GetOptionalString("description"),
            Severity = ParseSeverity(document),
            Enforcement = new EnforcementMetadata { Classification = ParseClassification(document) },
            Tags = document.GetStringArray("tags"),
            Remediation = document.GetOptionalString("remediation"),
            Documentation = document.GetStringArray("documentation"),
            Enabled = document.GetOptionalBool("enabled", true),
            Illustrative = document.GetOptionalBool("illustrative", false),
            Tests = ParseTests(document),
            Target = target,
            When = when,
            Assertions = assertions,
            Analyzer = analyzer
        };
    }

    private static IReadOnlyList<RuleTestCase> ParseTests(JsonObject document) =>
        document["tests"]?.AsArray()
            .Select(node => ParseTestCase(node!.AsObject()))
            .ToList()
        ?? [];

    private static RuleTestCase ParseTestCase(JsonObject node) =>
        new(
            node.GetRequiredString("name"),
            node["setup"]?.AsObject() ?? throw new RuleParsingException("Test case is missing required 'setup'."),
            EnumParsing.ParseSnakeCase<TestExpectation>(node.GetRequiredString("expect")));

    private static Severity ParseSeverity(JsonObject document) =>
        document.GetOptionalString("severity") is { } value
            ? EnumParsing.ParseSnakeCase<Severity>(value)
            : Severity.Warning;

    private static EnforcementClassification ParseClassification(JsonObject document) =>
        document["enforcement"]?.AsObject().GetOptionalString("classification") is { } value
            ? EnumParsing.ParseSnakeCase<EnforcementClassification>(value)
            : EnforcementClassification.Deterministic;
}
