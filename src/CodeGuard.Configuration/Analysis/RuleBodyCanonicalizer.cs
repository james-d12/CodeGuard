using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CodeGuard.Configuration.Analysis;

/// <summary>
/// A rule's "enforceable body" - the raw <c>target</c>+<c>assertions</c>+<c>when</c> (or
/// <c>analyzer</c>) nodes that actually determine what it checks, as opposed to descriptive
/// metadata (<c>name</c>, <c>description</c>, <c>tags</c>, <c>remediation</c>, ...) that doesn't
/// change behavior. Shared by <see cref="RuleSetAnalyzer"/>'s exact-duplicate detection and
/// <c>CodeGuard.Configuration.Versioning.RuleVersionChecker</c>'s version-fingerprint drift check -
/// both need the same definition of "did this rule's behavior change," just applied differently
/// (comparing two rules to each other vs. comparing one rule to its own recorded fingerprint).
/// Operates on the raw source document, not the parsed <c>RuleDefinition</c> - <c>IAssertion</c>/
/// <c>ITargetSelector</c> expose only <c>Kind</c>, not parameter values, so only the raw document
/// distinguishes e.g. two <c>must_inherit_from</c> rules checking different base types.
/// </summary>
public static class RuleBodyCanonicalizer
{
    /// <summary>
    /// Pulls just the behavior-defining nodes out of a rule document: <c>analyzer</c> alone if
    /// present, otherwise <c>target</c>+<c>assertions</c>+<c>when</c>. <c>when</c> gates which
    /// candidates the assertions even run against, so it is part of the enforceable body just as
    /// much as <c>assertions</c> is.
    /// </summary>
    public static JsonObject ExtractEnforceableBody(JsonObject document) =>
        document.TryGetPropertyValue("analyzer", out var analyzer) && analyzer is not null
            ? new JsonObject { ["analyzer"] = analyzer.DeepClone() }
            : new JsonObject
            {
                ["target"] = document["target"]?.DeepClone(),
                ["assertions"] = document["assertions"]?.DeepClone(),
                ["when"] = document["when"]?.DeepClone()
            };

    /// <summary>
    /// Renders a node to JSON text with object keys sorted, so two documents that differ only in
    /// param order compare equal. <see cref="JsonNode"/> has no order-independent equality/hash of
    /// its own, so comparison needs a canonical string form rather than the node itself.
    /// </summary>
    public static string Canonicalize(JsonNode? node) => node switch
    {
        null => "null",
        JsonObject obj => "{" + string.Join(
            ",",
            obj.OrderBy(property => property.Key, StringComparer.Ordinal)
                .Select(property => $"{JsonSerializer.Serialize(property.Key)}:{Canonicalize(property.Value)}")) + "}",
        JsonArray array => "[" + string.Join(",", array.Select(Canonicalize)) + "]",
        _ => node.ToJsonString()
    };

    /// <summary>`sha256:&lt;64 hex&gt;` of <see cref="ExtractEnforceableBody"/>'s canonical form.</summary>
    public static string ComputeFingerprint(JsonObject document)
    {
        var canonical = Canonicalize(ExtractEnforceableBody(document));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return "sha256:" + Convert.ToHexStringLower(hash);
    }
}
