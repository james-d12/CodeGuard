using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CsCheck;

namespace CodeGuard.RuleFuzzing.Tests.Generation;

/// <summary>Assembles complete rule documents from <see cref="SelectorGenerator"/>/<see cref="AssertionGenerator"/>/
/// <see cref="AnalyzerRuleGenerator"/> output. Each method here corresponds to one oracle's needs.</summary>
internal static class RuleDocumentGenerator
{
    /// <summary>Selector + 1-3 assertions whose <c>AppliesTo</c> is satisfied by the selector's
    /// <c>Produces</c> - the primary crash-fuzzing shape (never expected to be statically unreachable).</summary>
    public static Gen<JsonObject> CompatibleRule(CapabilityCatalog catalog) =>
        SelectorGenerator.Any(catalog).SelectMany(target =>
            AssertionGenerator.Any(catalog, target.Produces, PairingMode.Compatible, 0)
                .Array[RuleFuzzOptions.MinAssertionsPerRule, RuleFuzzOptions.MaxAssertionsPerRule]
                .Select(assertions => BuildDocument(target.Node, assertions)));

    /// <summary>Selector + exactly one top-level assertion guaranteed to statically mismatch it - feeds
    /// the static/dynamic agreement oracle (must appear in <c>RuleSetAnalyzer.UnreachableRules</c> and
    /// dynamically violate on every applicable candidate).</summary>
    public static Gen<JsonObject> IncompatibleRule(CapabilityCatalog catalog) =>
        SelectorGenerator.Any(catalog).SelectMany(target =>
            (AssertionGenerator.GuaranteedMismatch(catalog, target.Produces, 0)
                ?? AssertionGenerator.Any(catalog, target.Produces, PairingMode.Incompatible, 0))
                .Select(assertion => BuildDocument(target.Node, [assertion])));

    /// <summary>
    /// Selector + a single <c>must_all_match</c> assertion whose *nested* selector/assertion pairing is
    /// guaranteed mismatched. The outer target/assertion pairing is never itself unreachable (quantifier
    /// kinds have empty <c>AppliesTo</c>), so this exercises the documented gap where
    /// <c>RuleSetAnalyzer.FindUnreachableAssertions</c> does not recurse into nested selectors.
    /// </summary>
    public static Gen<JsonObject> NestedIncompatibleRule(CapabilityCatalog catalog) =>
        SelectorGenerator.Any(catalog).SelectMany(outer =>
            SelectorGenerator.Any(catalog).SelectMany(inner =>
                (AssertionGenerator.GuaranteedMismatch(catalog, inner.Produces, 1)
                    ?? AssertionGenerator.Any(catalog, inner.Produces, PairingMode.Incompatible, 1))
                    .Select(nestedAssertion =>
                    {
                        var mustAllMatch = new JsonObject
                        {
                            ["must_all_match"] = new JsonObject
                            {
                                ["selector"] = inner.Node,
                                ["assertions"] = new JsonArray(nestedAssertion)
                            }
                        };
                        return BuildDocument(outer.Node, [mustAllMatch]);
                    })));

    public static Gen<JsonObject> AnalyzerRule(CapabilityCatalog catalog) => AnalyzerRuleGenerator.AnyRule(catalog);

    private static JsonObject BuildDocument(JsonObject target, JsonNode?[] assertions) => new()
    {
        ["id"] = $"FUZZ-{Guid.NewGuid():N}",
        ["name"] = "Fuzz-generated rule",
        ["target"] = target,
        ["assertions"] = new JsonArray(assertions)
    };
}
