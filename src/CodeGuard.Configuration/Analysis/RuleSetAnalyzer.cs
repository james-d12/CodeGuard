using System.Text.Json;
using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Configuration.Loading;
using CodeGuard.Configuration.Validation;
using CodeGuard.RuleModel.Rules;

namespace CodeGuard.Configuration.Analysis;

/// <summary>One rule whose only known assertion can never fire against its target's candidate kind.</summary>
public sealed record UnreachableAssertionIssue(string RuleId, string SourceFile, string TargetKind, string AssertionKind);

/// <summary>A set of rules whose <c>target</c>+<c>assertions</c> (or <c>analyzer</c>) body is structurally identical.</summary>
public sealed record ExactDuplicateGroup(IReadOnlyList<string> RuleIds, IReadOnlyList<string> SourceFiles);

/// <summary>
/// Mechanically detectable problems across a whole rule set - docs/HIGH_LEVEL_AI_ASSISTING.md §14's
/// Tier 1 (cheap, no prerequisites) and Tier 2 (needs <see cref="CapabilityCatalog"/>) checks. Tier 3
/// (overlapping selectors, conflicting assertions, mutation-tested coverage) is explicitly out of
/// scope - see that section for why.
/// </summary>
public sealed record RuleAnalysisReport(
    int RuleCount,
    IReadOnlyList<RuleFileIssue> InvalidRules,
    IReadOnlyList<RuleFileIssue> DuplicateIds,
    IReadOnlyList<string> RulesWithoutTests,
    IReadOnlyList<string> OneSidedTestRules,
    IReadOnlyList<string> DisabledRules,
    IReadOnlyList<string> IllustrativeRules,
    IReadOnlyList<string> RulesMissingProvenance,
    IReadOnlyList<UnreachableAssertionIssue> UnreachableRules,
    IReadOnlyList<ExactDuplicateGroup> ExactDuplicateRules)
{
    /// <summary>
    /// Whether anything worth a human's attention was found. Illustrative/disabled/no-provenance
    /// rules are reported as counts for visibility but don't affect this - a rule set legitimately
    /// containing them (as this repo's own `examples/rules/` does, for all three) isn't itself a
    /// problem: `metadata.source` is optional, additive documentation, not a requirement.
    /// </summary>
    public bool HasFindings =>
        InvalidRules.Count > 0 || DuplicateIds.Count > 0 || RulesWithoutTests.Count > 0
        || OneSidedTestRules.Count > 0 || UnreachableRules.Count > 0 || ExactDuplicateRules.Count > 0;
}

public static class RuleSetAnalyzer
{
    /// <param name="validation">
    /// The result of <see cref="RuleFileLoader.ValidateDirectories"/> - reused rather than
    /// re-validating, since it already separates loadable rules from every structural issue
    /// (invalid files and duplicate ids alike).
    /// </param>
    public static RuleAnalysisReport Analyze(RuleSetValidationReport validation, CapabilityCatalog catalog)
    {
        var duplicateIds = validation.Issues
            .Where(issue => issue.Errors.Any(error => error.Code == RuleErrorCodes.DuplicateRuleId))
            .OrderBy(issue => issue.SourceFile, StringComparer.Ordinal)
            .ToList();
        var invalidRules = validation.Issues
            .Except(duplicateIds)
            .OrderBy(issue => issue.SourceFile, StringComparer.Ordinal)
            .ToList();

        var rulesWithoutTests = validation.Rules
            .Where(entry => entry.Rule.Tests.Count == 0)
            .Select(entry => entry.Rule.Id)
            .Order(StringComparer.Ordinal)
            .ToList();

        var oneSidedTestRules = validation.Rules
            .Where(entry => entry.Rule.Tests.Count > 0 && !CoversBothOutcomes(entry.Rule))
            .Select(entry => entry.Rule.Id)
            .Order(StringComparer.Ordinal)
            .ToList();

        var disabledRules = validation.Rules
            .Where(entry => !entry.Rule.Enabled)
            .Select(entry => entry.Rule.Id)
            .Order(StringComparer.Ordinal)
            .ToList();

        var illustrativeRules = validation.Rules
            .Where(entry => entry.Rule.Illustrative)
            .Select(entry => entry.Rule.Id)
            .Order(StringComparer.Ordinal)
            .ToList();

        var rulesMissingProvenance = validation.Rules
            .Where(entry => entry.Rule.Metadata?.Source is null)
            .Select(entry => entry.Rule.Id)
            .Order(StringComparer.Ordinal)
            .ToList();

        return new RuleAnalysisReport(
            validation.Rules.Count,
            invalidRules,
            duplicateIds,
            rulesWithoutTests,
            oneSidedTestRules,
            disabledRules,
            illustrativeRules,
            rulesMissingProvenance,
            FindUnreachableAssertions(validation.Rules, catalog),
            FindExactDuplicates(validation.Rules));
    }

    private static bool CoversBothOutcomes(RuleDefinition rule) =>
        rule.Tests.Any(t => t.Expect == TestExpectation.Pass) && rule.Tests.Any(t => t.Expect == TestExpectation.Fail);

    /// <summary>
    /// Flags a top-level assertion that the target selector's candidate kind can never satisfy, using
    /// the same <see cref="CapabilityDescriptor.Produces"/>/<see cref="CapabilityDescriptor.AppliesTo"/>
    /// metadata `rules discover` reports. Deliberately doesn't recurse into nested selectors/
    /// assertions (inside `must_all_match`/`must_any_match`/`must_none_match`/`must_have_count`) -
    /// those aren't exposed by the parsed model (only `IAssertion.Kind` survives parsing; nested
    /// selector/assertion objects stay in private constructor fields, the same limitation
    /// `rules explain --format json` works around by reading the source document instead). An
    /// assertion with an empty `AppliesTo` (the quantifier/existence kinds, which evaluate a nested
    /// selector rather than the outer candidate) is never flagged - see that field's own doc comment.
    /// </summary>
    private static IReadOnlyList<UnreachableAssertionIssue> FindUnreachableAssertions(
        IReadOnlyList<(RuleDefinition Rule, string SourceFile)> rules, CapabilityCatalog catalog)
    {
        var selectorsByKind = catalog.Selectors.ToDictionary(d => d.Kind, StringComparer.Ordinal);
        var assertionsByKind = catalog.Assertions.ToDictionary(d => d.Kind, StringComparer.Ordinal);

        var issues = new List<UnreachableAssertionIssue>();
        foreach (var (rule, sourceFile) in rules)
        {
            if (rule.Target is null || rule.Assertions is null)
            {
                continue; // analyzer-shaped rule - has no selector-driven target to check against.
            }

            if (!selectorsByKind.TryGetValue(rule.Target.Kind, out var selectorDescriptor)
                || selectorDescriptor.Produces is not { } produces)
            {
                continue;
            }

            foreach (var assertion in rule.Assertions)
            {
                if (!assertionsByKind.TryGetValue(assertion.Kind, out var assertionDescriptor)
                    || assertionDescriptor.AppliesTo.Count == 0)
                {
                    continue;
                }

                if (!assertionDescriptor.AppliesTo.Contains(produces))
                {
                    issues.Add(new UnreachableAssertionIssue(rule.Id, sourceFile, rule.Target.Kind, assertion.Kind));
                }
            }
        }

        return issues;
    }

    /// <summary>
    /// Groups rules whose <c>target</c>+<c>assertions</c> (or <c>analyzer</c>) body is structurally
    /// identical. Needs the raw source document, not the parsed model - `IAssertion`/`ITargetSelector`
    /// expose only `Kind`, not the parameter values that would distinguish e.g. two `must_inherit_from`
    /// rules checking different base types (the same reason `rules explain --format json` reads the
    /// source document rather than introspecting the model).
    /// </summary>
    private static IReadOnlyList<ExactDuplicateGroup> FindExactDuplicates(
        IReadOnlyList<(RuleDefinition Rule, string SourceFile)> rules)
    {
        var groups = new Dictionary<string, List<(string Id, string SourceFile)>>(StringComparer.Ordinal);
        foreach (var (rule, sourceFile) in rules)
        {
            var document = RuleFileLoader.ReadDocument(sourceFile).AsObject();
            var shape = document.TryGetPropertyValue("analyzer", out var analyzer) && analyzer is not null
                ? new JsonObject { ["analyzer"] = analyzer.DeepClone() }
                : new JsonObject
                {
                    ["target"] = document["target"]?.DeepClone(),
                    ["assertions"] = document["assertions"]?.DeepClone()
                };

            var key = Canonicalize(shape);
            if (!groups.TryGetValue(key, out var group))
            {
                groups[key] = group = [];
            }

            group.Add((rule.Id, sourceFile));
        }

        return groups.Values
            .Where(group => group.Count > 1)
            .Select(group => new ExactDuplicateGroup(
                group.Select(entry => entry.Id).Order(StringComparer.Ordinal).ToList(),
                group.Select(entry => entry.SourceFile).Order(StringComparer.Ordinal).ToList()))
            .OrderBy(group => group.RuleIds[0], StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Renders a node to JSON text with object keys sorted, so two documents that differ only in
    /// param order compare equal. <see cref="JsonNode"/> has no order-independent equality/hash of
    /// its own, so grouping needs a canonical string key rather than a dictionary keyed by the node.
    /// </summary>
    private static string Canonicalize(JsonNode? node) => node switch
    {
        null => "null",
        JsonObject obj => "{" + string.Join(
            ",",
            obj.OrderBy(property => property.Key, StringComparer.Ordinal)
                .Select(property => $"{JsonSerializer.Serialize(property.Key)}:{Canonicalize(property.Value)}")) + "}",
        JsonArray array => "[" + string.Join(",", array.Select(Canonicalize)) + "]",
        _ => node.ToJsonString()
    };
}
