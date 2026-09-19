using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CsCheck;

namespace CodeGuard.RuleFuzzing.Tests.Generation;

/// <summary>Builds an analyzer-shaped rule document (<c>{"analyzer": {"kind": ..., ...params}}</c>),
/// exercising <c>RuleEvaluator.EvaluateAnalyzerRule</c> separately from the selector/assertion path.</summary>
internal static class AnalyzerRuleGenerator
{
    public static Gen<JsonObject> AnyRule(CapabilityCatalog catalog) =>
        Gen.OneOf(catalog.Analyzers.Select(BuildAnalyzerNode).ToArray()).Select(analyzerNode => new JsonObject
        {
            ["id"] = $"FUZZ-{Guid.NewGuid():N}",
            ["name"] = "Fuzz-generated analyzer rule",
            ["analyzer"] = analyzerNode
        });

    private static Gen<JsonObject> BuildAnalyzerNode(CapabilityDescriptor descriptor)
    {
        var genObj = JsonObjectGenExtensions.NewObject().AddField("kind", JsonObjectGenExtensions.FreshString(descriptor.Kind));

        foreach (var param in descriptor.Parameters)
        {
            var valueGen = ParameterValueGenerator.ForParameter(param);
            genObj = param.Required ? genObj.AddField(param.Name, valueGen) : genObj.MaybeAddField(param.Name, valueGen);
        }

        return genObj;
    }
}
