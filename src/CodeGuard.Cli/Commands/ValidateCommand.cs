using System.CommandLine;
using System.Diagnostics;
using CodeGuard.Analysis.Providers;
using CodeGuard.Cli.Support;
using CodeGuard.Analyzers.MSBuild;
using CodeGuard.Analyzers.Repository;
using CodeGuard.Core.Evaluation;
using CodeGuard.Core.Results;
using CodeGuard.Reporting;
using CodeGuard.Reporting.Console;
using CodeGuard.Reporting.Html;
using CodeGuard.Reporting.Json;
using CodeGuard.Reporting.Sarif;
using CodeGuard.RuleModel.Rules;
using Microsoft.Extensions.Logging;

namespace CodeGuard.Cli.Commands;

public static class ValidateCommand
{
    public static Command Build()
    {
        var pathOption = CommonOptions.CreatePathOption();
        var configOption = CommonOptions.CreateConfigOption();
        var rulesSourceOption = CommonOptions.CreateRulesSourceOption();
        var branchOption = CommonOptions.CreateBranchOption();

        var formatOption = new Option<string>("--format")
        {
            Description = "Output format: console, json, sarif, or html.",
            DefaultValueFactory = _ => "console"
        };
        formatOption.AcceptOnlyFromAmong("console", "json", "sarif", "html");

        var outputOption = new Option<string?>("--output")
        {
            Description = "File (or directory) to write the report to (default: stdout). If a directory " +
                "(existing, or ending in a path separator), a default filename based on --format is used, " +
                "e.g. validation-report.html."
        };

        var colorOption = new Option<bool>("--color")
        {
            Description = "Force ANSI color in console output, even when redirected. Ignored when --output is set."
        };

        var noColorOption = new Option<bool>("--no-color")
        {
            Description = "Disable ANSI color in console output, even in an interactive terminal."
        };

        var ruleOption = new Option<string[]>("--rule")
        {
            Description = "Restrict evaluation to these rule IDs (repeatable). Default: all discovered rules."
        };

        var solutionOption = new Option<string[]>("--solution")
        {
            Description = "Restrict analysis to these .sln/.slnx file(s) (repeatable). Default: every .sln/.slnx file found recursively under --path."
        };

        var severityThresholdOption = new Option<string>("--severity-threshold")
        {
            Description = "Drop violations below this severity from the report.",
            DefaultValueFactory = _ => "info"
        };
        severityThresholdOption.AcceptOnlyFromAmong("info", "warning", "error", "critical");

        var failOnOption = new Option<string>("--fail-on")
        {
            Description = "Minimum severity that causes a non-zero exit code.",
            DefaultValueFactory = _ => "info"
        };
        failOnOption.AcceptOnlyFromAmong("info", "warning", "error", "critical");

        var verbosityOption = CommonOptions.CreateVerbosityOption();

        var maxParallelismOption = new Option<int?>("--max-parallelism")
        {
            Description = "Maximum number of concurrent workers used for project analysis and rule " +
                "evaluation (default: number of processor cores). Set to 1 to force fully sequential execution."
        };
        maxParallelismOption.Validators.Add(result =>
        {
            var value = result.GetValueOrDefault<int?>();
            if (value is <= 0)
            {
                result.AddError("--max-parallelism must be a positive integer.");
            }
        });

        var command = new Command("validate", "Validate the repository against configured rules");
        command.Add(pathOption);
        command.Add(configOption);
        command.Add(rulesSourceOption);
        command.Add(branchOption);
        command.Add(formatOption);
        command.Add(outputOption);
        command.Add(colorOption);
        command.Add(noColorOption);
        command.Add(ruleOption);
        command.Add(solutionOption);
        command.Add(severityThresholdOption);
        command.Add(failOnOption);
        command.Add(verbosityOption);
        command.Add(maxParallelismOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            using var loggerFactory = CliLoggerFactory.Create(CliLoggerFactory.ParseVerbosity(parseResult.GetValue(verbosityOption)!));
            var logger = loggerFactory.CreateLogger(typeof(ValidateCommand));

            var context = CliRepositoryContext.Resolve(
                parseResult.GetValue(pathOption),
                parseResult.GetValue(configOption),
                parseResult.GetValue(rulesSourceOption),
                parseResult.GetValue(branchOption),
                loggerFactory: loggerFactory);

            if (!context.TryRequireRulesConfigured(Console.Error))
            {
                logger.LogError("No rules directory is configured for {RepoRoot}", context.RepoRoot);
                return 1;
            }

            var ruleReport = context.ValidateRules();
            if (!ruleReport.IsValid)
            {
                logger.LogWarning(
                    "{FailedCount} of {TotalCount} rule file(s) failed validation; aborting before analysis",
                    ruleReport.Issues.Count, ruleReport.Rules.Count + ruleReport.Issues.Count);
                RuleValidationReportWriter.WriteConsole(ruleReport, Console.Out);
                return 1;
            }

            var rules = ruleReport.Rules.Select(r => r.Rule).ToList();
            var selectedRuleIds = parseResult.GetValue(ruleOption) ?? [];
            if (selectedRuleIds.Length > 0)
            {
                var selectedSet = new HashSet<string>(selectedRuleIds, StringComparer.Ordinal);
                rules = rules.Where(r => selectedSet.Contains(r.Id)).ToList();
                logger.LogDebug(
                    "Restricted to {SelectedCount} of {TotalCount} rule(s) via --rule: {RuleIds}",
                    rules.Count, ruleReport.Rules.Count, string.Join(", ", selectedRuleIds));
            }
            else
            {
                logger.LogDebug("Using all {TotalCount} discovered rule(s) (no --rule filter)", rules.Count);
            }

            // Known limitation: if one of the resolved solutions is this tool's own currently-running
            // solution, Buildalyzer's design-time "Clean" step can delete shared output files (such as
            // its own logger assembly) still needed by this process, causing analysis of one of the
            // projects to fail. This doesn't affect validating any other repository.
            try
            {
                var solutionPaths = SolutionFileLocator.Resolve(
                    context.RepoRoot, parseResult.GetValue(solutionOption) ?? [], loggerFactory.CreateLogger(typeof(SolutionFileLocator)));

                logger.LogInformation("Analyzing {SolutionCount} solution(s) under {RepoRoot}", solutionPaths.Count, context.RepoRoot);

                var maxParallelism = parseResult.GetValue(maxParallelismOption);

                var builder = new AnalysisModelBuilder(
                    [
                        new RepositoryFileProvider(loggerFactory.CreateLogger<RepositoryFileProvider>()),
                        new MsBuildAnalysisProvider(solutionPaths, loggerFactory.CreateLogger<MsBuildAnalysisProvider>(), maxParallelism)
                    ],
                    loggerFactory.CreateLogger<AnalysisModelBuilder>());
                var buildStopwatch = Stopwatch.StartNew();
                var model = await builder.BuildAsync(context.RepoRoot, cancellationToken);
                logger.LogInformation("Analysis model built in {ElapsedMs} ms", buildStopwatch.ElapsedMilliseconds);

                var evaluator = new RuleEvaluator(loggerFactory.CreateLogger<RuleEvaluator>());
                var evaluateStopwatch = Stopwatch.StartNew();
                var result = evaluator.Evaluate(rules, model, maxParallelism);
                logger.LogInformation(
                    "Evaluation complete in {ElapsedMs} ms: {RulesEvaluated} rule(s) evaluated, {ViolationCount} violation(s), {ErrorCount} evaluation error(s)",
                    evaluateStopwatch.ElapsedMilliseconds, result.RulesEvaluated, result.Violations.Count, result.EvaluationErrors.Count);

                var severityThreshold = ParseSeverity(parseResult.GetValue(severityThresholdOption)!);
                result = ApplySeverityThreshold(result, severityThreshold);

                var rawOutput = parseResult.GetValue(outputOption);
                var outputPath = ReportOutputPathResolver.Resolve(
                    rawOutput, parseResult.GetValue(formatOption)!, rawOutput is not null && Directory.Exists(rawOutput));
                if (outputPath is not null)
                {
                    var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
                    if (!string.IsNullOrEmpty(outputDirectory))
                    {
                        Directory.CreateDirectory(outputDirectory);
                    }
                }

                var useColor = ColorSupport.ShouldUseColor(
                    parseResult.GetValue(colorOption),
                    parseResult.GetValue(noColorOption),
                    writingToFile: outputPath is not null,
                    consoleOutputRedirected: Console.IsOutputRedirected,
                    noColorEnvVar: Environment.GetEnvironmentVariable("NO_COLOR"));

                var reporter = CreateReporter(parseResult.GetValue(formatOption)!, useColor);

                logger.LogDebug("Writing {Format} report to {Destination}", parseResult.GetValue(formatOption), outputPath ?? "stdout");

                TextWriter writer = outputPath is null ? Console.Out : new StreamWriter(outputPath);
                try
                {
                    await reporter.WriteAsync(result, writer, cancellationToken);
                    await writer.FlushAsync(cancellationToken);
                }
                finally
                {
                    if (outputPath is not null)
                    {
                        await writer.DisposeAsync();
                    }
                }

                var failOnThreshold = ParseSeverity(parseResult.GetValue(failOnOption)!);
                var hasQualifyingViolations = result.Violations.Any(v => v.Severity >= failOnThreshold);
                return hasQualifyingViolations || result.EvaluationErrors.Count > 0 ? 1 : 0;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Validation failed: {Message}", ex.Message);
                await Console.Error.WriteLineAsync($"codeguard: {ex.Message}");
                return 1;
            }
        });

        return command;
    }

    private static ValidationResult ApplySeverityThreshold(ValidationResult result, Severity threshold)
    {
        if (threshold == Severity.Info)
        {
            return result;
        }

        var filteredViolations = result.Violations.Where(v => v.Severity >= threshold).ToList();
        var rulesFailed = filteredViolations.Select(v => v.RuleId).Distinct().Count();

        return result with
        {
            Violations = filteredViolations,
            RulesFailed = rulesFailed,
            RulesPassed = result.RulesEvaluated - rulesFailed - result.RulesErrored,
            Status = result.EvaluationErrors.Count > 0
                ? ValidationStatus.PartiallyEvaluated
                : filteredViolations.Count == 0 ? ValidationStatus.Passed : ValidationStatus.Failed
        };
    }

    private static IViolationReporter CreateReporter(string format, bool useColor) => format switch
    {
        "json" => new JsonViolationReporter(),
        "sarif" => new SarifViolationReporter(),
        "html" => new HtmlViolationReporter(),
        _ => new ConsoleViolationReporter(useColor)
    };

    private static Severity ParseSeverity(string value) => value switch
    {
        "info" => Severity.Info,
        "warning" => Severity.Warning,
        "error" => Severity.Error,
        "critical" => Severity.Critical,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown severity.")
    };
}
