using System.Text.Json.Nodes;
using CsCheck;

namespace CodeGuard.RuleFuzzing.Tests.Generation;

/// <summary>Folds field generators into a <c>Gen&lt;JsonObject&gt;</c> being built up one key at a time.</summary>
internal static class JsonObjectGenExtensions
{
    /// <summary>
    /// A fresh, unattached <c>JsonObject</c> for every single generation. <c>Gen.Const(Func&lt;T&gt;)</c>
    /// is a *lazy constant* - CsCheck calls the factory once and reuses that one instance thereafter,
    /// not once per generation - so using it here would mutate and re-attach the same object across
    /// every iteration, throwing "The node already has a parent." <c>Gen.Bool.Select(...)</c> forces a
    /// genuinely fresh call on every generation, since <c>Select</c>'s projection runs against whatever
    /// the underlying generator actually produced that time.
    /// </summary>
    public static Gen<JsonObject> NewObject() => Gen.Bool.Select(_ => new JsonObject());

    /// <summary>A fresh <c>JsonValue</c> wrapping <paramref name="value"/> on every generation - see
    /// <see cref="NewObject"/>'s doc comment for why a plain <c>Gen.Const</c> of a pre-built JsonNode
    /// is unsafe to reuse across iterations.</summary>
    public static Gen<JsonNode?> FreshString(string value) => Gen.Bool.Select(_ => (JsonNode?)JsonValue.Create(value));

    public static Gen<JsonObject> AddField(this Gen<JsonObject> genObj, string key, Gen<JsonNode?> valueGen) =>
        genObj.SelectMany(obj => valueGen.Select(value =>
        {
            obj[key] = value;
            return obj;
        }));

    /// <summary>Includes the field with probability <see cref="RuleFuzzOptions.IncludeOptionalWeight"/>; otherwise omits it entirely.</summary>
    public static Gen<JsonObject> MaybeAddField(this Gen<JsonObject> genObj, string key, Gen<JsonNode?> valueGen) =>
        genObj.SelectMany(obj => Gen.Frequency(
            (RuleFuzzOptions.IncludeOptionalWeight, valueGen),
            (RuleFuzzOptions.OmitOptionalWeight, Gen.Const((JsonNode?)null)))
            .Select(value =>
            {
                if (value is not null)
                {
                    obj[key] = value;
                }

                return obj;
            }));
}
