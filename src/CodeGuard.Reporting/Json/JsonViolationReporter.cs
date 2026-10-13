using System.Text.Json;
using System.Text.Json.Serialization;
using CodeGuard.Core.Results;
using CodeGuard.Reporting.Contracts;

namespace CodeGuard.Reporting.Json;

/// <summary>
/// Writes the versioned <see cref="ValidationReportV1"/> contract. Output shape is documented in
/// <c>docs/architecture/RESULT_CONTRACT.md</c>.
/// </summary>
public sealed class JsonViolationReporter : IViolationReporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _toolVersion;
    private readonly TimeSpan _duration;

    /// <param name="toolVersion">Reported as <c>toolVersion</c>. Defaults to this assembly's version.</param>
    /// <param name="duration">Reported as <c>durationMs</c>. Defaults to zero, i.e. not measured.</param>
    public JsonViolationReporter(string? toolVersion = null, TimeSpan duration = default)
    {
        _toolVersion = toolVersion ?? ValidationReportMapper.ResolveToolVersion(typeof(JsonViolationReporter).Assembly);
        _duration = duration;
    }

    public string Format => "json";

    public async Task WriteAsync(ValidationResult result, TextWriter writer, CancellationToken ct = default)
    {
        var report = ValidationReportMapper.ToV1(result, _duration, _toolVersion);
        var json = JsonSerializer.Serialize(report, Options);
        await writer.WriteLineAsync(json.AsMemory(), ct);
    }
}
