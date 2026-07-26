using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;

namespace RulesEngine.Configuration.Validation;

public sealed class RuleSchemaValidator
{
    private const string EmbeddedResourceName = "RulesEngine.Configuration.Validation.Schemas.rule.schema.json";

    // JsonSchema.Net registers schemas globally by their $id, so parsing the same
    // schema text twice in one process throws. Parse it once and share it.
    private static readonly Lazy<JsonSchema> DefaultSchema = new(LoadDefaultSchema);

    private readonly JsonSchema _schema;

    private RuleSchemaValidator(JsonSchema schema) => _schema = schema;

    public static RuleSchemaValidator CreateDefault() => new(DefaultSchema.Value);

    private static JsonSchema LoadDefaultSchema()
    {
        var assembly = typeof(RuleSchemaValidator).Assembly;
        using var stream = assembly.GetManifestResourceStream(EmbeddedResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded rule schema resource '{EmbeddedResourceName}' was not found in assembly '{assembly.FullName}'.");
        using var reader = new StreamReader(stream);
        return JsonSchema.FromText(reader.ReadToEnd());
    }

    public void Validate(JsonNode? document, string source)
    {
        var element = document is null ? default : document.Deserialize<JsonElement>();
        var results = _schema.Evaluate(element, new EvaluationOptions { OutputFormat = OutputFormat.List });
        if (results.IsValid)
        {
            return;
        }

        var errors = (results.Details ?? [])
            .Where(detail => detail.Errors is { Count: > 0 })
            .SelectMany(detail => detail.Errors!.Values.Select(message => $"{detail.InstanceLocation}: {message}"))
            .ToList();

        if (errors.Count == 0)
        {
            errors.Add("Document does not conform to the rule schema.");
        }

        throw new RuleSchemaValidationException(source, errors);
    }
}
