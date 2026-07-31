using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodeGuard.Cli.Support;
using CodeGuard.Configuration.GlobalConfig;
using CodeGuard.Configuration.Validation;

namespace CodeGuard.Cli.Commands;

public static class InfoCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static Command Build()
    {
        var pathOption = CommonOptions.CreatePathOption();
        var configOption = CommonOptions.CreateConfigOption();
        var rulesSourceOption = CommonOptions.CreateRulesSourceOption();
        var branchOption = CommonOptions.CreateBranchOption();

        var formatOption = new Option<string>("--format")
        {
            Description = "Output format: console or json.",
            DefaultValueFactory = _ => "console"
        };
        formatOption.AcceptOnlyFromAmong("console", "json");

        var command = new Command(
            "info",
            "Show where codeguard's rules are configured from (repo config, `codeguard setup`, an ad-hoc " +
            "--rules-source, or the default) and how many rules were discovered.");
        command.Add(pathOption);
        command.Add(configOption);
        command.Add(rulesSourceOption);
        command.Add(branchOption);
        command.Add(formatOption);

        command.SetAction((parseResult, _) =>
        {
            var rulesSourceValue = parseResult.GetValue(rulesSourceOption);
            var branchValue = parseResult.GetValue(branchOption);

            var context = CliRepositoryContext.Resolve(
                parseResult.GetValue(pathOption),
                parseResult.GetValue(configOption),
                rulesSourceValue,
                branchValue);

            // ValidateRules(), not LoadRules()/LoadRulesWithSource(): info must keep working (and say
            // so) when a rule file is malformed, since that's exactly the situation someone runs it to
            // diagnose.
            var report = context.ValidateRules();

            if (parseResult.GetValue(formatOption) == "json")
            {
                WriteJson(context, report, rulesSourceValue, branchValue);
            }
            else
            {
                WriteConsole(context, report, rulesSourceValue, branchValue);
            }

            return Task.FromResult(0);
        });

        return command;
    }

    private static void WriteConsole(
        CliRepositoryContext context, RuleSetValidationReport report, string? rulesSourceValue, string? branchValue)
    {
        Console.WriteLine($"Repo root: {context.RepoRoot}");
        Console.WriteLine();

        Console.WriteLine("Rules source:");
        switch (context.RulesProvenance)
        {
            case RulesSourceProvenance.CliOverride:
                var kind = RuleSourceResolver.DetectKind(rulesSourceValue!);
                Console.WriteLine("  Provenance:  --rules-source override");
                Console.WriteLine($"  Kind:        {kind}");
                Console.WriteLine($"  Location:    {rulesSourceValue}");
                if (kind == RuleSourceKind.Git)
                {
                    Console.WriteLine($"  Branch:      {branchValue ?? "(repo default)"}");
                }
                break;
            case RulesSourceProvenance.GlobalSettings:
                var settings = context.GlobalSettings!;
                Console.WriteLine("  Provenance:  global settings (via `codeguard setup`)");
                Console.WriteLine($"  Kind:        {settings.Kind}");
                Console.WriteLine($"  Location:    {settings.Location}");
                if (settings.Kind == RuleSourceKind.Git)
                {
                    Console.WriteLine($"  Branch:      {settings.Branch ?? "(repo default)"}");
                }
                break;
            case RulesSourceProvenance.RepositoryConfig:
                Console.WriteLine("  Provenance:  repository config");
                Console.WriteLine($"  Config file: {context.ConfigFilePath}");
                break;
            case RulesSourceProvenance.Default:
                Console.WriteLine(
                    $"  Provenance:  default (no {context.ConfigFilePath} found and `codeguard setup` has not been run)");
                break;
        }

        Console.WriteLine(
            $"  Resolved directories: {Describe(context.Layout.RulesPaths)}");
        Console.WriteLine();

        var enabledCount = report.Rules.Count(r => r.Rule.Enabled);
        Console.WriteLine("Rules discovered:");
        Console.WriteLine($"  Total:    {report.Rules.Count}");
        Console.WriteLine($"  Enabled:  {enabledCount}");
        Console.WriteLine($"  Disabled: {report.Rules.Count - enabledCount}");
        if (report.Issues.Count > 0)
        {
            Console.WriteLine(
                $"  Invalid:  {report.Issues.Count} rule file(s) failed to parse - run `codeguard rules check` for details.");
        }

        Console.WriteLine();
        Console.WriteLine("  By severity:");
        foreach (var group in report.Rules
            .GroupBy(r => r.Rule.Severity.ToString())
            .OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            Console.WriteLine($"    {group.Key,-10} {group.Count()}");
        }

        Console.WriteLine();
        Console.WriteLine("  By standard:");
        foreach (var group in report.Rules
            .GroupBy(r => StandardFor(r.SourceFile, context.Layout.RulesPaths))
            .OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            Console.WriteLine($"    {group.Key,-14} {group.Count()}");
        }

        Console.WriteLine();
        Console.WriteLine("Other configured paths:");
        Console.WriteLine($"  Standards: {Describe(context.Layout.StandardsPaths)}");
        Console.WriteLine($"  Skills:    {Describe(context.Layout.SkillsPaths)}");
        Console.WriteLine($"  Agents:    {Describe(context.Layout.AgentsPaths)}");
        Console.WriteLine($"  Source:    {Describe(context.Layout.SourcePaths)}");
        Console.WriteLine($"  Tests:     {Describe(context.Layout.TestsPaths)}");
    }

    private static void WriteJson(
        CliRepositoryContext context, RuleSetValidationReport report, string? rulesSourceValue, string? branchValue)
    {
        var enabledCount = report.Rules.Count(r => r.Rule.Enabled);

        var (kind, location, branch) = context.RulesProvenance switch
        {
            RulesSourceProvenance.CliOverride =>
                (RuleSourceResolver.DetectKind(rulesSourceValue!) as RuleSourceKind?, rulesSourceValue, branchValue),
            RulesSourceProvenance.GlobalSettings =>
                (context.GlobalSettings!.Kind as RuleSourceKind?, context.GlobalSettings.Location, context.GlobalSettings.Branch),
            _ => (null as RuleSourceKind?, null as string, null as string)
        };

        var summary = new InfoSummary(
            context.RepoRoot,
            new RulesSourceSummary(
                context.RulesProvenance, kind, location, branch, context.ConfigFilePath, context.Layout.RulesPaths),
            new RuleCounts(
                report.Rules.Count,
                enabledCount,
                report.Rules.Count - enabledCount,
                report.Issues.Count,
                report.Rules.GroupBy(r => r.Rule.Severity.ToString()).ToDictionary(g => g.Key, g => g.Count()),
                report.Rules
                    .GroupBy(r => StandardFor(r.SourceFile, context.Layout.RulesPaths))
                    .ToDictionary(g => g.Key, g => g.Count())),
            new LayoutSummary(
                context.Layout.StandardsPaths,
                context.Layout.SkillsPaths,
                context.Layout.AgentsPaths,
                context.Layout.SourcePaths,
                context.Layout.TestsPaths));

        Console.WriteLine(JsonSerializer.Serialize(summary, JsonOptions));
    }

    private static string Describe(IReadOnlyList<string> paths) => paths.Count == 0 ? "(none)" : string.Join(", ", paths);

    private static string StandardFor(string sourceFile, IReadOnlyList<string> rulesPaths)
    {
        foreach (var root in rulesPaths)
        {
            var relative = Path.GetRelativePath(root, sourceFile);
            if (!relative.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relative))
            {
                var separatorIndex = relative.IndexOf(Path.DirectorySeparatorChar);
                return separatorIndex < 0 ? "(root)" : relative[..separatorIndex];
            }
        }

        return "(unknown)";
    }

    private sealed record InfoSummary(string RepoRoot, RulesSourceSummary RulesSource, RuleCounts RuleCounts, LayoutSummary Layout);

    private sealed record RulesSourceSummary(
        RulesSourceProvenance Provenance,
        RuleSourceKind? Kind,
        string? Location,
        string? Branch,
        string ConfigFilePath,
        IReadOnlyList<string> ResolvedDirectories);

    private sealed record RuleCounts(
        int Total,
        int Enabled,
        int Disabled,
        int InvalidFiles,
        IReadOnlyDictionary<string, int> BySeverity,
        IReadOnlyDictionary<string, int> ByStandard);

    private sealed record LayoutSummary(
        IReadOnlyList<string> StandardsPaths,
        IReadOnlyList<string> SkillsPaths,
        IReadOnlyList<string> AgentsPaths,
        IReadOnlyList<string> SourcePaths,
        IReadOnlyList<string> TestsPaths);
}
