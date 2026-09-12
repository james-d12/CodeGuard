using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;

namespace CodeGuard.Configuration.Validation;

public sealed class RuleSchemaValidator
{
    private const string EmbeddedResourceName = "CodeGuard.Configuration.Validation.Schemas.rule.schema.json";

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

        // detail.InstanceLocation is a JSON Pointer to the offending node. Keeping it as a separate
        // field rather than folding it into the message is what lets a caller navigate to the node.
        //
        // Ordered deepest-path-first, because JsonSchema.Net reports the whole failure chain and the
        // root-level entries are the least actionable: a rule using the target+assertions form emits
        // `Required properties ["analyzer"] are not present` from the losing `oneOf` branch, which is
        // noise. The pinpointed error must lead, since a caller reading errors[0] should get the one
        // naming the offending property. Nothing is discarded - a genuine root-level failure is still
        // reported, just after the specific ones.
        var errors = (results.Details ?? [])
            .Where(detail => detail.Errors is { Count: > 0 })
            .SelectMany(detail => detail.Errors!.Values.Select(message => new RuleValidationError(
                RuleErrorCodes.SchemaViolation,
                detail.InstanceLocation.ToString() is { Length: > 0 } location ? location : null,
                message)))
            .OrderByDescending(error => error.Path is null ? -1 : error.Path.Count(c => c == '/'))
            .ThenBy(error => error.Path, StringComparer.Ordinal)
            .ToList();

        if (errors.Count == 0)
        {
            errors.Add(new RuleValidationError(
                RuleErrorCodes.SchemaViolation, null, "Document does not conform to the rule schema."));
        }

        throw new RuleSchemaValidationException(source, errors);
    }
}
