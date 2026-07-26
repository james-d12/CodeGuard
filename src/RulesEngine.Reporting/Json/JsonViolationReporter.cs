using System.Text.Json;
using System.Text.Json.Serialization;
using RulesEngine.Core.Results;

namespace RulesEngine.Reporting.Json;

public sealed class JsonViolationReporter : IViolationReporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public string Format => "json";

    public async Task WriteAsync(ValidationResult result, TextWriter writer, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(result, Options);
        await writer.WriteLineAsync(json.AsMemory(), ct);
    }
}
