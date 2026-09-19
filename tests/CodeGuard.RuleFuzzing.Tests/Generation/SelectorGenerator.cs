using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CsCheck;

namespace CodeGuard.RuleFuzzing.Tests.Generation;

/// <summary>
/// Builds a <c>{"kind": ..., ...params}</c> node for a randomly-chosen selector kind from
/// <see cref="CapabilityCatalog.Selectors"/>. No selector currently declares a
/// <see cref="ParameterType.Selector"/>/<see cref="ParameterType.AssertionList"/> parameter of its
/// own (only assertions do, for the quantifier kinds), so selector generation needs no depth budget
/// today - <see cref="AssertionGenerator"/> is where recursion actually happens.
/// </summary>
internal static class SelectorGenerator
{
    public static Gen<(JsonObject Node, CandidateKind? Produces)> Any(CapabilityCatalog catalog) =>
        Gen.OneOf(catalog.Selectors.Select(BuildFor).ToArray());

    private static Gen<(JsonObject Node, CandidateKind? Produces)> BuildFor(CapabilityDescriptor descriptor)
    {
        var genObj = JsonObjectGenExtensions.NewObject().AddField("kind", JsonObjectGenExtensions.FreshString(descriptor.Kind));

        foreach (var param in descriptor.Parameters)
        {
            var valueGen = ParameterValueGenerator.ForParameter(param);
            genObj = param.Required ? genObj.AddField(param.Name, valueGen) : genObj.MaybeAddField(param.Name, valueGen);
        }

        return genObj.Select(obj => (obj, descriptor.Produces));
    }
}
