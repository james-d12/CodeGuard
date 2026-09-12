using CodeGuard.Configuration.Parsing;

namespace CodeGuard.Configuration.Capabilities;

/// <summary>
/// The engine's complete rule-authoring vocabulary, read from the parser registries rather than a
/// hand-maintained list. This is what `codeguard rules discover` serves and what the authoring
/// skill's reference docs are generated from - both derive from the registries, so neither can drift
/// from what the engine actually parses.
/// </summary>
public sealed record CapabilityCatalog(
    IReadOnlyList<CapabilityDescriptor> Selectors,
    IReadOnlyList<CapabilityDescriptor> Assertions,
    IReadOnlyList<CapabilityDescriptor> Analyzers,
    IReadOnlyList<string> Conditions)
{
    /// <summary>
    /// The `when:` combinators. Unlike the other three these aren't registry-driven -
    /// <see cref="ConditionParserRegistry"/> hardcodes them in a switch and delegates everything else
    /// to the assertion registry, so any assertion kind is also valid as a `when:` leaf.
    /// </summary>
    public static readonly IReadOnlyList<string> ConditionKinds = ["and", "or", "not"];

    public static CapabilityCatalog Create()
    {
        var selectors = DefaultParsers.CreateSelectorRegistry();
        var assertions = DefaultParsers.CreateAssertionRegistry(selectors);
        var analyzers = DefaultAnalyzers.CreateRegistry();

        return new CapabilityCatalog(
            selectors.Descriptors,
            assertions.Descriptors,
            analyzers.Descriptors,
            ConditionKinds);
    }
}
