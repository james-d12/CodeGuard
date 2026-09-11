using CodeGuard.Configuration.Capabilities;
using CodeGuard.Configuration.Parsing;

namespace CodeGuard.Configuration.Tests.Capabilities;

/// <summary>
/// Guards the descriptor layer against the failure it exists to prevent: a primitive that is
/// registered and usable from a rule file but invisible to `rules discover` and to the generated
/// authoring docs, which is how the skill's references drifted 18 primitives behind the engine.
/// </summary>
public sealed class CapabilityCatalogTests
{
    private static readonly CapabilityCatalog Catalog = CapabilityCatalog.Create();

    public static TheoryData<string, CapabilityDescriptor> AllDescriptors()
    {
        var data = new TheoryData<string, CapabilityDescriptor>();
        foreach (var d in Catalog.Selectors) data.Add("selector", d);
        foreach (var d in Catalog.Assertions) data.Add("assertion", d);
        foreach (var d in Catalog.Analyzers) data.Add("analyzer", d);
        return data;
    }

    [Theory]
    [MemberData(nameof(AllDescriptors))]
    public void EveryDescriptor_IsUsablyPopulated(string category, CapabilityDescriptor descriptor)
    {
        Assert.False(string.IsNullOrWhiteSpace(descriptor.Kind), $"{category} has a blank kind");
        Assert.False(
            string.IsNullOrWhiteSpace(descriptor.Summary),
            $"{category} '{descriptor.Kind}' has no summary");

        foreach (var parameter in descriptor.Parameters)
        {
            Assert.False(
                string.IsNullOrWhiteSpace(parameter.Name),
                $"{category} '{descriptor.Kind}' has a blank parameter name");
            Assert.False(
                string.IsNullOrWhiteSpace(parameter.Summary),
                $"{category} '{descriptor.Kind}' parameter '{parameter.Name}' has no summary");
        }

        Assert.Distinct(descriptor.Parameters.Select(p => p.Name));
    }

    [Fact]
    public void EveryRegisteredSelectorKind_HasAMatchingDescriptor()
    {
        var selectors = DefaultParsers.CreateSelectorRegistry();
        Assert.Equal(
            selectors.Kinds.Order(StringComparer.Ordinal),
            Catalog.Selectors.Select(d => d.Kind));
    }

    [Fact]
    public void EveryRegisteredAssertionKind_HasAMatchingDescriptor()
    {
        var assertions = DefaultParsers.CreateAssertionRegistry(DefaultParsers.CreateSelectorRegistry());
        Assert.Equal(
            assertions.Kinds.Order(StringComparer.Ordinal),
            Catalog.Assertions.Select(d => d.Kind));
    }

    [Fact]
    public void EveryRegisteredAnalyzerKind_HasAMatchingDescriptor()
    {
        Assert.Equal(
            DefaultAnalyzers.CreateRegistry().Kinds.Order(StringComparer.Ordinal),
            Catalog.Analyzers.Select(d => d.Kind));
    }

    [Fact]
    public void EverySelector_DeclaresWhatItProduces()
    {
        // Produces/AppliesTo are what make an unreachable selector+assertion pairing detectable, so a
        // selector without one silently opts out of that check.
        var missing = Catalog.Selectors.Where(d => d.Produces is null).Select(d => d.Kind).ToList();
        Assert.Empty(missing);
    }

    [Fact]
    public void EveryAssertion_EitherTargetsCandidateKindsOrIsDeliberatelyCandidateAgnostic()
    {
        // The existence/quantifier assertions ignore the outer candidate entirely and legitimately
        // declare no AppliesTo. Everything else must name the kinds it accepts.
        string[] candidateAgnostic =
            ["must_exist", "must_not_exist", "must_have_count", "must_all_match", "must_any_match", "must_none_match"];

        var unscoped = Catalog.Assertions
            .Where(d => d.AppliesTo.Count == 0)
            .Select(d => d.Kind)
            .Order(StringComparer.Ordinal);

        Assert.Equal(candidateAgnostic.Order(StringComparer.Ordinal), unscoped);
    }
}
