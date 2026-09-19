using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CsCheck;

namespace CodeGuard.RuleFuzzing.Tests.Generation;

/// <summary>
/// Produces documents that are catalog-valid except for one deliberate schema-shape break - used only
/// by the narrow <c>RuleSchemaValidator</c>-only oracle (<c>SchemaViolationFuzzTests</c>), not the main
/// crash-fuzzing loop.
/// </summary>
internal static class SchemaViolationGenerator
{
    public static Gen<JsonObject> AnyViolation(CapabilityCatalog catalog) =>
        RuleDocumentGenerator.CompatibleRule(catalog).SelectMany(valid =>
            Gen.OneOfConst("missing_id", "missing_name", "two_key_assertion", "target_and_analyzer", "empty_assertions")
                .Select(kind => Break(valid, kind)));

    private static JsonObject Break(JsonObject valid, string kind)
    {
        var broken = (JsonObject)valid.DeepClone();
        switch (kind)
        {
            case "missing_id":
                broken.Remove("id");
                break;
            case "missing_name":
                broken.Remove("name");
                break;
            case "two_key_assertion":
                var assertions = broken["assertions"]!.AsArray();
                if (assertions.Count > 0 && assertions[0] is JsonObject firstAssertion)
                {
                    firstAssertion["extra_key"] = new JsonObject();
                }

                break;
            case "target_and_analyzer":
                broken["analyzer"] = new JsonObject { ["kind"] = "no-exceptions" };
                break;
            case "empty_assertions":
                broken["assertions"] = new JsonArray();
                break;
        }

        return broken;
    }
}
