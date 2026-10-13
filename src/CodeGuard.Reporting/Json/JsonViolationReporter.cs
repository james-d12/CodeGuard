using System.Text.Json;
using System.Text.Json.Serialization;
using CodeGuard.Core.Results;
using CodeGuard.Reporting.Contracts;
using CodeGuard.Reporting.Provenance;

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
    private readonly ValidationProvenance? _provenance;

    /// <param name="toolVersion">Reported as <c>toolVersion</c>. Defaults to this assembly's version.</param>
    /// <param name="duration">Reported as <c>durationMs</c>. Defaults to zero, i.e. not measured.</param>
    /// <param name="provenance">Reported as <c>repository</c>/<c>commit</c>/<c>branch</c>/<c>ruleSources</c>. Omitted when null.</param>
    public JsonViolationReporter(string? toolVersion = null, TimeSpan duration = default, ValidationProvenance? provenance = null)
    {
        _toolVersion = toolVersion ?? ValidationReportMapper.ResolveToolVersion(typeof(JsonViolationReporter).Assembly);
        _duration = duration;
        _provenance = provenance;
    }

    public string Format => "json";

    public async Task WriteAsync(ValidationResult result, TextWriter writer, CancellationToken ct = default)
    {
        var report = ValidationReportMapper.ToV1(result, _duration, _toolVersion, _provenance);
        var json = JsonSerializer.Serialize(report, Options);
        await writer.WriteLineAsync(json.AsMemory(), ct);
    }
}
