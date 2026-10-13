using Microsoft.CodeAnalysis.Sarif;
using CodeGuard.Core.Results;
using CodeGuard.Reporting.Contracts;
using CodeGuard.Reporting.Provenance;
using CodeGuard.RuleModel.Rules;

namespace CodeGuard.Reporting.Sarif;

public sealed class SarifViolationReporter : IViolationReporter
{
    private readonly ValidationProvenance? _provenance;
    private readonly string _toolVersion;

    /// <param name="provenance">
    /// Populates <c>run.versionControlProvenance</c> (when a repository URI is known) and the run property
    /// bag's <c>ruleSources</c>. Null leaves both out.
    /// </param>
    /// <param name="toolVersion">Reported as <c>tool.driver.version</c>. Defaults to this assembly's version.</param>
    public SarifViolationReporter(ValidationProvenance? provenance = null, string? toolVersion = null)
    {
        _provenance = provenance;
        _toolVersion = toolVersion ?? ValidationReportMapper.ResolveToolVersion(typeof(SarifViolationReporter).Assembly);
    }

    public string Format => "sarif";

    public async Task WriteAsync(ValidationResult result, TextWriter writer, CancellationToken ct = default)
    {
        var rules = result.Violations
            .GroupBy(v => v.RuleId)
            .Select(g =>
            {
                var descriptor = new ReportingDescriptor
                {
                    Id = g.Key,
                    ShortDescription = new MultiformatMessageString(g.First().Message, null, null)
                };

                // ReportingDescriptor has no native `version` property in this SDK (unlike
                // ToolComponent, which does) - the properties bag is SARIF's own sanctioned
                // extensibility point for exactly this kind of tool-specific metadata.
                descriptor.SetProperty("version", g.First().RuleVersion);
                return descriptor;
            })
            .ToList();

        var run = new Run
        {
            Tool = new Tool
            {
                Driver = new ToolComponent
                {
                    Name = "codeguard",
                    Version = _toolVersion,
                    Rules = rules
                }
            },
            Results = result.Violations.Select(ToSarifResult).ToList(),
            Invocations =
            [
                new Invocation
                {
                    ExecutionSuccessful = result.EvaluationErrors.Count == 0,
                    ToolExecutionNotifications = result.EvaluationErrors.Select(ToSarifNotification)
                        .Concat(result.AnalysisWarnings.Select(ToSarifNotification))
                        .ToList()
                }
            ]
        };

        AddProvenance(run, _provenance);

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

    /// <summary>
    /// Repository identity goes in <c>versionControlProvenance</c>, which SARIF requires to carry a
    /// <c>repositoryUri</c>, so it is only written when the repository is known and parses as a URI.
    /// Rule-set provenance has no SARIF home, so it goes in the run property bag.
    /// </summary>
    private static void AddProvenance(Run run, ValidationProvenance? provenance)
    {
        if (provenance is null)
        {
            return;
        }

        if (provenance.Repository is not null && Uri.TryCreate(provenance.Repository, UriKind.Absolute, out var repositoryUri))
        {
            run.VersionControlProvenance =
            [
                new VersionControlDetails
                {
                    RepositoryUri = repositoryUri,
                    RevisionId = provenance.Commit,
                    Branch = provenance.Branch
                }
            ];
        }

        run.SetProperty("ruleSources", provenance.RuleSources
            .Select(r => new Dictionary<string, string?>
            {
                ["kind"] = r.Kind,
                ["location"] = r.Location,
                ["commit"] = r.Commit
            })
            .ToList());
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

        // Violation metadata has no fixed SARIF home, so it goes into the result's property bag as-is.
        if (violation.Metadata is not null)
        {
            foreach (var (key, value) in violation.Metadata)
            {
                result.SetProperty(key, value);
            }
        }

        return result;
    }

    private static Notification ToSarifNotification(RuleEvaluationError error) => new()
    {
        Level = FailureLevel.Error,
        Message = new Message($"{error.ExceptionType}: {error.Message}", null, null, null, null),
        Descriptor = new ReportingDescriptorReference { Id = error.RuleId }
    };

    private static Notification ToSarifNotification(AnalysisWarning warning) => new()
    {
        Level = FailureLevel.Warning,
        Message = new Message(warning.Message, null, null, null, null),
        Descriptor = new ReportingDescriptorReference { Id = warning.Code }
    };

    private static Location ToLocation(Violation violation) => new()
    {
        PhysicalLocation = new PhysicalLocation
        {
            ArtifactLocation = new ArtifactLocation { Uri = new Uri(violation.File!, UriKind.RelativeOrAbsolute) },
            Region = violation.Line is null ? null : ToRegion(violation)
        }
    };

    // SARIF's Region.EndLine/EndColumn are non-nullable ints, so they are only set when the violation carries an end span.
    // Without one the region has just the start, which is still valid SARIF.
    private static Region ToRegion(Violation violation)
    {
        var region = new Region { StartLine = violation.Line!.Value, StartColumn = violation.Column ?? 1 };
        if (violation.EndLine is { } endLine)
        {
            region.EndLine = endLine;
        }

        if (violation.EndColumn is { } endColumn)
        {
            region.EndColumn = endColumn;
        }

        return region;
    }

    private static FailureLevel ToFailureLevel(Severity severity) => severity switch
    {
        Severity.Info => FailureLevel.Note,
        Severity.Warning => FailureLevel.Warning,
        Severity.Error or Severity.Critical => FailureLevel.Error,
        _ => FailureLevel.Warning
    };
}
