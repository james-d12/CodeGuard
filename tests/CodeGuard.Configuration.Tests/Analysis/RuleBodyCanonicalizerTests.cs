using System.Text.Json.Nodes;
using CodeGuard.Configuration.Analysis;

namespace CodeGuard.Configuration.Tests.Analysis;

public sealed class RuleBodyCanonicalizerTests
{
    [Fact]
    public void ExtractEnforceableBody_AnalyzerRule_IgnoresTargetAndAssertions()
    {
        var document = JsonNode.Parse("""
            {
                "id": "DDD-ENTITY-001",
                "name": "Some rule",
                "analyzer": { "kind": "exhaustive-switch" },
                "target": { "kind": "class" }
            }
            """)!.AsObject();

        var body = RuleBodyCanonicalizer.ExtractEnforceableBody(document);

        Assert.True(body.ContainsKey("analyzer"));
        Assert.False(body.ContainsKey("target"));
        Assert.False(body.ContainsKey("assertions"));
    }

    [Fact]
    public void ComputeFingerprint_DiffersWhenWhenBlockDiffers()
    {
        var withWhen = JsonNode.Parse("""
            {
                "id": "DDD-ENTITY-001",
                "name": "Some rule",
                "target": { "kind": "class" },
                "when": { "must_have_attribute": { "type": "System.ObsoleteAttribute" } },
                "assertions": [ { "must_inherit_from": { "type": "Entity<*>" } } ]
            }
            """)!.AsObject();

        var withoutWhen = JsonNode.Parse("""
            {
                "id": "DDD-ENTITY-001",
                "name": "Some rule",
                "target": { "kind": "class" },
                "assertions": [ { "must_inherit_from": { "type": "Entity<*>" } } ]
            }
            """)!.AsObject();

        Assert.NotEqual(RuleBodyCanonicalizer.ComputeFingerprint(withWhen), RuleBodyCanonicalizer.ComputeFingerprint(withoutWhen));
    }

    [Fact]
    public void ComputeFingerprint_IsInsensitiveToPropertyOrder()
    {
        var first = JsonNode.Parse("""
            {
                "target": { "kind": "class" },
                "assertions": [ { "must_have_count": { "selector": { "kind": "type" }, "exactly": 1 } } ]
            }
            """)!.AsObject();

        var second = JsonNode.Parse("""
            {
                "assertions": [ { "must_have_count": { "exactly": 1, "selector": { "kind": "type" } } } ],
                "target": { "kind": "class" }
            }
            """)!.AsObject();

        Assert.Equal(RuleBodyCanonicalizer.ComputeFingerprint(first), RuleBodyCanonicalizer.ComputeFingerprint(second));
    }

    [Fact]
    public void ComputeFingerprint_HasExpectedShape()
    {
        var document = JsonNode.Parse("""{ "target": { "kind": "repository" } }""")!.AsObject();

        var fingerprint = RuleBodyCanonicalizer.ComputeFingerprint(document);

        Assert.Matches("^sha256:[0-9a-f]{64}$", fingerprint);
    }

    [Fact]
    public void ComputeFingerprint_IsDeterministicAcrossCalls()
    {
        var document = JsonNode.Parse("""{ "target": { "kind": "repository" } }""")!.AsObject();

        Assert.Equal(RuleBodyCanonicalizer.ComputeFingerprint(document), RuleBodyCanonicalizer.ComputeFingerprint(document));
    }
}
