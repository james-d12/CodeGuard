using System.CommandLine;
using RulesEngine.Analysis.Providers;
using RulesEngine.Analyzers.MSBuild;
using RulesEngine.Analyzers.Repository;
using RulesEngine.Cli.Support;
using RulesEngine.Core.Evaluation;
using RulesEngine.Core.Results;
using RulesEngine.Reporting;
using RulesEngine.Reporting.Console;
using RulesEngine.Reporting.Json;
using RulesEngine.Reporting.Sarif;
using RulesEngine.RuleModel.Rules;

namespace RulesEngine.Cli.Commands;

public static class ValidateCommand
{
    public static Command Build()
    {
        var pathOption = CommonOptions.CreatePathOption();
        var configOption = CommonOptions.CreateConfigOption();

        var formatOption = new Option<string>("--format")
        {
            Description = "Output format: console, json, or sarif.",
            DefaultValueFactory = _ => "console"
        };
        formatOption.AcceptOnlyFromAmong("console", "json", "sarif");

        var outputOption = new Option<string?>("--output")
        {
            Description = "File to write the report to (default: stdout)."
        };

        var ruleOption = new Option<string[]>("--rule")
        {
            Description = "Restrict evaluation to these rule IDs (repeatable). Default: all discovered rules."
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

        var command = new Command("validate", "Validate the repository against configured rules");
        command.Add(pathOption);
        command.Add(configOption);
        command.Add(formatOption);
        command.Add(outputOption);
        command.Add(ruleOption);
        command.Add(severityThresholdOption);
        command.Add(failOnOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var context = CliRepositoryContext.Resolve(
                parseResult.GetValue(pathOption),
                parseResult.GetValue(configOption));

            var rules = context.LoadRules();
            var selectedRuleIds = parseResult.GetValue(ruleOption) ?? [];
            if (selectedRuleIds.Length > 0)
            {
                var selectedSet = new HashSet<string>(selectedRuleIds, StringComparer.Ordinal);
                rules = rules.Where(r => selectedSet.Contains(r.Id)).ToList();
            }

            // Known limitation: if solutionPath is this tool's own currently-running solution,
            // Buildalyzer's design-time "Clean" step can delete shared output files (such as its own
            // logger assembly) still needed by this process, causing analysis of one of the projects
            // to fail. This doesn't affect validating any other repository.
            var solutionPath = Directory.GetFiles(context.RepoRoot, "*.sln").Single();

            var builder = new AnalysisModelBuilder([new RepositoryFileProvider(), new MsBuildAnalysisProvider(solutionPath)]);
            var model = await builder.BuildAsync(context.RepoRoot, cancellationToken);

            var evaluator = new RuleEvaluator();
            var result = evaluator.Evaluate(rules, model);

            var severityThreshold = ParseSeverity(parseResult.GetValue(severityThresholdOption)!);
            result = ApplySeverityThreshold(result, severityThreshold);

            var reporter = CreateReporter(parseResult.GetValue(formatOption)!);
            var outputPath = parseResult.GetValue(outputOption);

            TextWriter writer = outputPath is null ? System.Console.Out : new StreamWriter(outputPath);
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
            return result.Violations.Any(v => v.Severity >= failOnThreshold) ? 1 : 0;
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
            RulesPassed = result.RulesEvaluated - rulesFailed,
            Status = filteredViolations.Count == 0 ? ValidationStatus.Passed : ValidationStatus.Failed
        };
    }

    private static IViolationReporter CreateReporter(string format) => format switch
    {
        "json" => new JsonViolationReporter(),
        "sarif" => new SarifViolationReporter(),
        _ => new ConsoleViolationReporter()
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
