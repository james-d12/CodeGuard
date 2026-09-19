using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CsCheck;

namespace CodeGuard.RuleFuzzing.Tests.Generation;

/// <summary>
/// Builds a single-key <c>{"&lt;kind&gt;": {...params}}</c> assertion node. Recursion (nested
/// <c>selector</c>/<c>assertions</c> parameters, used by the quantifier kinds) is bounded by
/// <paramref name="depth"/> against <see cref="RuleFuzzOptions.MaxNestingDepth"/> - past that,
/// generation is structurally restricted to assertions with no further recursive parameters, so
/// termination doesn't depend on probability.
/// </summary>
internal static class AssertionGenerator
{
    public static Gen<JsonObject> Any(CapabilityCatalog catalog, CandidateKind? produces, PairingMode mode, int depth)
    {
        var pool = LeafSafePool(catalog, LooselyEligible(catalog, produces, mode), depth);
        return Gen.OneOf(pool.Select(d => BuildFor(d, catalog, mode, depth)).ToArray());
    }

    /// <summary>
    /// An assertion guaranteed to statically mismatch <paramref name="produces"/> (non-empty
    /// <c>AppliesTo</c> that excludes it) - null if <paramref name="produces"/> is null or no such
    /// assertion exists at this depth, in which case the caller should fall back to <see cref="Any"/>.
    /// </summary>
    public static Gen<JsonObject>? GuaranteedMismatch(CapabilityCatalog catalog, CandidateKind? produces, int depth)
    {
        if (produces is not { } kind)
        {
            return null;
        }

        var pool = LeafSafePool(
            catalog,
            catalog.Assertions.Where(d => d.AppliesTo.Count > 0 && !d.AppliesTo.Contains(kind)).ToList(),
            depth);

        return pool.Count == 0 ? null : Gen.OneOf(pool.Select(d => BuildFor(d, catalog, PairingMode.Incompatible, depth)).ToArray());
    }

    /// <summary>Every registered assertion has at least one non-recursive kind applicable to it (an
    /// empty result here would mean the catalog itself has a gap, not a generator bug).</summary>
    private static List<CapabilityDescriptor> LeafSafePool(CapabilityCatalog catalog, List<CapabilityDescriptor> pool, int depth)
    {
        if (depth < RuleFuzzOptions.MaxNestingDepth)
        {
            return pool;
        }

        var leafSafe = pool.Where(d => !HasRecursiveParameter(d)).ToList();
        return leafSafe.Count > 0 ? leafSafe : catalog.Assertions.Where(d => !HasRecursiveParameter(d)).ToList();
    }

    private static List<CapabilityDescriptor> LooselyEligible(CapabilityCatalog catalog, CandidateKind? produces, PairingMode mode)
    {
        var eligible = catalog.Assertions.Where(d => IsLooselyEligible(d, produces, mode)).ToList();
        return eligible.Count > 0 ? eligible : catalog.Assertions.Where(d => IsLooselyEligible(d, produces, PairingMode.Compatible)).ToList();
    }

    /// <summary>An assertion with empty <c>AppliesTo</c> (existence/quantifier kinds - they evaluate a
    /// nested selector, not the outer candidate) is always eligible regardless of mode; this is what
    /// makes <see cref="GuaranteedMismatch"/> - not this method - the source of truth for "definitely
    /// mismatched" when the cross-check oracle needs one.</summary>
    private static bool IsLooselyEligible(CapabilityDescriptor assertion, CandidateKind? produces, PairingMode mode)
    {
        if (assertion.AppliesTo.Count == 0 || produces is not { } kind)
        {
            return true;
        }

        var applies = assertion.AppliesTo.Contains(kind);
        return mode == PairingMode.Compatible ? applies : !applies;
    }

    private static bool HasRecursiveParameter(CapabilityDescriptor descriptor) =>
        descriptor.Parameters.Any(p => p.Type is ParameterType.Selector or ParameterType.AssertionList);

    private static Gen<JsonObject> BuildFor(CapabilityDescriptor descriptor, CapabilityCatalog catalog, PairingMode mode, int depth)
    {
        var genParams = JsonObjectGenExtensions.NewObject();

        foreach (var param in descriptor.Parameters)
        {
            var valueGen = param.Type switch
            {
                ParameterType.Selector => SelectorGenerator.Any(catalog).Select(pair => (JsonNode?)pair.Node),
                ParameterType.AssertionList => NestedAssertionList(catalog, mode, depth),
                _ => ParameterValueGenerator.ForParameter(param)
            };

            genParams = param.Required ? genParams.AddField(param.Name, valueGen) : genParams.MaybeAddField(param.Name, valueGen);
        }

        return genParams.Select(paramsObj => new JsonObject { [descriptor.Kind] = paramsObj });
    }

    /// <summary>
    /// The quantifier kinds (<c>must_all_match</c>/<c>must_any_match</c>/<c>must_none_match</c>) nest
    /// their own <c>selector</c> too, generated once here so the nested assertions can be paired
    /// against its <c>Produces</c> - the same relationship as the top-level target/assertions, one
    /// level deeper. <c>RuleSetAnalyzer.FindUnreachableAssertions</c> deliberately does not recurse
    /// into this nesting (see its own doc comment), which <c>UnreachableRuleCrossCheckFuzzTests</c>
    /// asserts as an explicit, monitored gap.
    /// </summary>
    private static Gen<JsonNode?> NestedAssertionList(CapabilityCatalog catalog, PairingMode mode, int depth) =>
        SelectorGenerator.Any(catalog).SelectMany(pair =>
            Any(catalog, pair.Produces, mode, depth + 1).Array[1, 3]
                .Select(items => (JsonNode?)new JsonArray(items.Select(item => (JsonNode?)item).ToArray())));
}
