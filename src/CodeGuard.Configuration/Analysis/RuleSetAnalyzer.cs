using CodeGuard.Configuration.Capabilities;
using CodeGuard.Configuration.Loading;
using CodeGuard.Configuration.Validation;
using CodeGuard.RuleModel.Rules;
using System.Text.Json.Nodes;

namespace CodeGuard.Configuration.Analysis;

/// <summary>
/// One assertion that can never fire against the candidate kind it is checked on. <paramref name="TargetKind"/>
/// is the selector kind whose candidates it was checked against (the rule's `target`, or the `selector` of
/// the quantifier it is nested in). <paramref name="Location"/> is the path to the assertion inside the rule
/// document, e.g. <c>assertions[0]</c>, <c>assertions[0].assertions[1]</c> or <c>when.and[1]</c>.
/// </summary>
public sealed record UnreachableAssertionIssue(
    string RuleId,
    string SourceFile,
    string TargetKind,
    string AssertionKind,
    string Location);

/// <summary>A set of rules whose <c>target</c>+<c>assertions</c> (or <c>analyzer</c>) body is structurally identical.</summary>
public sealed record ExactDuplicateGroup(IReadOnlyList<string> RuleIds, IReadOnlyList<string> SourceFiles);

/// <summary>
/// Mechanically detectable problems across a whole rule set - docs/roadmap/HIGH_LEVEL_AI_ASSISTING.md §14's
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
    /// Flags an assertion that the candidate kind it is checked against can never satisfy, using the
    /// same <see cref="CapabilityDescriptor.Produces"/>/<see cref="CapabilityDescriptor.AppliesTo"/>
    /// metadata `rules discover` reports. Three places are checked:
    /// <list type="bullet">
    /// <item>top-level assertions, against the target selector (from the parsed model);</item>
    /// <item>assertions nested in a quantifier's `assertions:`, against that quantifier's `selector:`;</item>
    /// <item>`when:` leaves, against the target selector.</item>
    /// </list>
    /// The last two need the raw document (<see cref="NestedAssertionScanner"/>): only `IAssertion.Kind`
    /// survives parsing, so nested selector/assertion objects are otherwise unrecoverable - the same
    /// reason `rules explain --format json` and the exact-duplicate check read the source document. An
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

            var document = RuleFileLoader.ReadDocument(sourceFile).AsObject();
            var produces = selectorsByKind.TryGetValue(rule.Target.Kind, out var selectorDescriptor)
                ? selectorDescriptor.Produces
                : null;
            var scanner = new NestedAssertionScanner(
                rule.Id, sourceFile, selectorsByKind, assertionsByKind, issues);

            for (var index = 0; index < rule.Assertions.Count; index++)
            {
                var assertion = rule.Assertions[index];
                var location = $"assertions[{index}]";
                if (produces is { } candidate
                    && assertionsByKind.TryGetValue(assertion.Kind, out var assertionDescriptor)
                    && assertionDescriptor.AppliesTo.Count > 0
                    && !assertionDescriptor.AppliesTo.Contains(candidate))
                {
                    issues.Add(new UnreachableAssertionIssue(
                        rule.Id, sourceFile, rule.Target.Kind, assertion.Kind, location));
                }

                scanner.ScanNested(document["assertions"]?[index], location);
            }

            scanner.ScanCondition(document["when"], "when", rule.Target.Kind, produces);
        }

        return issues;
    }

    /// <summary>
    /// Walks one rule's raw document for the assertions the parsed model can't expose: those nested in a
    /// quantifier's `assertions:` list, and `when:` leaves. Kinds missing from the catalog are skipped
    /// silently - unknown kinds are rejected by schema validation and `rules validate`, not by this check.
    /// </summary>
    private sealed class NestedAssertionScanner(
        string ruleId,
        string sourceFile,
        IReadOnlyDictionary<string, CapabilityDescriptor> selectorsByKind,
        IReadOnlyDictionary<string, CapabilityDescriptor> assertionsByKind,
        List<UnreachableAssertionIssue> issues)
    {
        /// <summary>Descends into a top-level assertion's nested selector/assertions, without re-checking the assertion itself.</summary>
        public void ScanNested(JsonNode? assertionNode, string location) =>
            ScanAssertion(assertionNode, location, candidateKind: null, produces: null);

        /// <summary>Walks a `when:` tree - `and`/`or`/`not` combinators down to assertion leaves, each checked against the target.</summary>
        public void ScanCondition(JsonNode? conditionNode, string location, string targetKind, CandidateKind? produces)
        {
            if (conditionNode is not JsonObject { Count: 1 } node)
            {
                return;
            }

            var (key, value) = node.Single();
            switch (key)
            {
                case "and" or "or":
                    var children = value?.AsArray();
                    for (var index = 0; children is not null && index < children.Count; index++)
                    {
                        ScanCondition(children[index], $"{location}.{key}[{index}]", targetKind, produces);
                    }

                    break;
                case "not":
                    ScanCondition(value, $"{location}.not", targetKind, produces);
                    break;
                default:
                    ScanAssertion(node, location, targetKind, produces);
                    break;
            }
        }

        /// <summary>
        /// Checks one `{ kind: params }` assertion against <paramref name="produces"/> when given (a `when:`
        /// leaf, or an assertion nested in a quantifier), then recurses into its own nested `selector:` and
        /// `assertions:` if its descriptor declares an <see cref="ParameterType.AssertionList"/> parameter.
        /// </summary>
        private void ScanAssertion(JsonNode? assertionNode, string location, string? candidateKind, CandidateKind? produces)
        {
            if (assertionNode is not JsonObject { Count: 1 } node)
            {
                return;
            }

            var (kind, parameters) = node.Single();
            if (!assertionsByKind.TryGetValue(kind, out var descriptor))
            {
                return;
            }

            if (produces is { } candidate
                && candidateKind is not null
                && descriptor.AppliesTo.Count > 0
                && !descriptor.AppliesTo.Contains(candidate))
            {
                issues.Add(new UnreachableAssertionIssue(ruleId, sourceFile, candidateKind, kind, location));
            }

            if (parameters is not JsonObject parameterObject
                || !descriptor.Parameters.Any(p => p.Type == ParameterType.AssertionList))
            {
                return;
            }

            var nestedSelectorKind = parameterObject["selector"]?["kind"]?.GetValue<string>();
            var nestedProduces = nestedSelectorKind is not null
                && selectorsByKind.TryGetValue(nestedSelectorKind, out var selectorDescriptor)
                    ? selectorDescriptor.Produces
                    : null;

            var nested = parameterObject["assertions"]?.AsArray();
            for (var index = 0; nested is not null && index < nested.Count; index++)
            {
                ScanAssertion(
                    nested[index],
                    $"{location}.assertions[{index}]",
                    nestedSelectorKind,
                    nestedProduces);
            }
        }
    }

    /// <summary>
    /// Groups rules whose enforceable body (<see cref="RuleBodyCanonicalizer.ExtractEnforceableBody"/>)
    /// is structurally identical. Needs the raw source document, not the parsed model -
    /// `IAssertion`/`ITargetSelector` expose only `Kind`, not the parameter values that would
    /// distinguish e.g. two `must_inherit_from` rules checking different base types (the same reason
    /// `rules explain --format json` reads the source document rather than introspecting the model).
    /// </summary>
    private static IReadOnlyList<ExactDuplicateGroup> FindExactDuplicates(
        IReadOnlyList<(RuleDefinition Rule, string SourceFile)> rules)
    {
        var groups = new Dictionary<string, List<(string Id, string SourceFile)>>(StringComparer.Ordinal);
        foreach (var (rule, sourceFile) in rules)
        {
            var document = RuleFileLoader.ReadDocument(sourceFile).AsObject();
            var body = RuleBodyCanonicalizer.ExtractEnforceableBody(document);

            var key = RuleBodyCanonicalizer.Canonicalize(body);
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
}
