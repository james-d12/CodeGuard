using Microsoft.CodeAnalysis.Sarif;
using CodeGuard.Core.Results;
using CodeGuard.RuleModel.Rules;

namespace CodeGuard.Reporting.Sarif;

public sealed class SarifViolationReporter : IViolationReporter
{
    public string Format => "sarif";

    public async Task WriteAsync(ValidationResult result, TextWriter writer, CancellationToken ct = default)
    {
        var rules = result.Violations
            .GroupBy(v => v.RuleId)
            .Select(g => new ReportingDescriptor
            {
                Id = g.Key,
                ShortDescription = new MultiformatMessageString(g.First().Message, null, null)
            })
            .ToList();

        var run = new Run
        {
            Tool = new Tool
            {
                Driver = new ToolComponent
                {
                    Name = "codeguard",
                    Rules = rules
                }
            },
            Results = result.Violations.Select(ToSarifResult).ToList(),
            Invocations =
            [
                new Invocation
                {
                    ExecutionSuccessful = result.EvaluationErrors.Count == 0,
                    ToolExecutionNotifications = result.EvaluationErrors.Select(ToSarifNotification).ToList()
                }
            ]
        };

        var log = new SarifLog
        {
            Version = SarifVersion.Current,
            SchemaUri = SarifVersion.Current.ConvertToSchemaUri(),
            Runs = [run]
        };

        using var memoryStream = new MemoryStream();
        log.Save(memoryStream);
        memoryStream.Position = 0;

        using var reader = new StreamReader(memoryStream);
        var json = await reader.ReadToEndAsync(ct);
        await writer.WriteLineAsync(json.AsMemory(), ct);
    }

    private static Result ToSarifResult(Violation violation)
    {
        var result = new Result
        {
            RuleId = violation.RuleId,
            Level = ToFailureLevel(violation.Severity),
            Message = new Message(violation.Message, null, null, null, null)
        };

        if (violation.File is not null)
        {
            result.Locations = [ToLocation(violation)];
        }

        return result;
    }

    private static Notification ToSarifNotification(RuleEvaluationError error) => new()
    {
        Level = FailureLevel.Error,
        Message = new Message($"{error.ExceptionType}: {error.Message}", null, null, null, null),
        Descriptor = new ReportingDescriptorReference { Id = error.RuleId }
    };

    private static Location ToLocation(Violation violation) => new()
    {
        PhysicalLocation = new PhysicalLocation
        {
            ArtifactLocation = new ArtifactLocation { Uri = new Uri(violation.File!, UriKind.RelativeOrAbsolute) },
            Region = violation.Line is null
                ? null
                : new Region { StartLine = violation.Line.Value, StartColumn = violation.Column ?? 1 }
        }
    };

    private static FailureLevel ToFailureLevel(Severity severity) => severity switch
    {
        Severity.Info => FailureLevel.Note,
        Severity.Warning => FailureLevel.Warning,
        Severity.Error or Severity.Critical => FailureLevel.Error,
        _ => FailureLevel.Warning
    };
}
