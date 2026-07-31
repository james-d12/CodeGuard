using System.CommandLine;
using CodeGuard.Cli.Support;
using CodeGuard.RuleModel.Rules;

namespace CodeGuard.Cli.Commands;

public static class ExplainRuleCommand
{
    public static Command Build()
    {
        var pathOption = CommonOptions.CreatePathOption();
        var configOption = CommonOptions.CreateConfigOption();
        var rulesSourceOption = CommonOptions.CreateRulesSourceOption();
        var branchOption = CommonOptions.CreateBranchOption();
        var ruleIdArgument = new Argument<string>("ruleId")
        {
            Description = "The rule ID to explain, e.g. DDD-ENTITY-001."
        };

        var command = new Command("explain-rule", "Print full metadata and source YAML for a single rule");
        command.Add(pathOption);
        command.Add(configOption);
        command.Add(rulesSourceOption);
        command.Add(branchOption);
        command.Add(ruleIdArgument);

        command.SetAction((parseResult, _) =>
        {
            var context = CliRepositoryContext.Resolve(
                parseResult.GetValue(pathOption),
                parseResult.GetValue(configOption),
                parseResult.GetValue(rulesSourceOption),
                parseResult.GetValue(branchOption));

            var ruleId = parseResult.GetValue(ruleIdArgument)!;
            var entries = context.LoadRulesWithSource();
            var entry = entries.FirstOrDefault(e => e.Rule.Id == ruleId);

            if (entry.Rule is null)
            {
                Console.Error.WriteLine($"Rule '{ruleId}' was not found under {string.Join(", ", context.Layout.RulesPaths)}.");
                return Task.FromResult(1);
            }

            PrintSummary(entry.Rule);
            Console.WriteLine();
            Console.WriteLine($"Source: {entry.SourceFile}");
            Console.WriteLine();
            Console.WriteLine("--- Raw YAML ---");
            Console.WriteLine(File.ReadAllText(entry.SourceFile));

            return Task.FromResult(0);
        });

        return command;
    }

    private static void PrintSummary(RuleDefinition rule)
    {
        Console.WriteLine($"Id:            {rule.Id}");
        Console.WriteLine($"Name:          {rule.Name}");
        if (rule.Description is not null)
        {
            Console.WriteLine($"Description:   {rule.Description.Trim()}");
        }
        Console.WriteLine($"Severity:      {rule.Severity}");
        Console.WriteLine($"Enforcement:   {rule.Enforcement.Classification}");
        Console.WriteLine($"Tags:          {(rule.Tags.Count == 0 ? "-" : string.Join(", ", rule.Tags))}");
        Console.WriteLine($"Enabled:       {rule.Enabled}");
        Console.WriteLine($"Illustrative:  {rule.Illustrative}");
        if (rule.Analyzer is not null)
        {
            Console.WriteLine($"Analyzer:      {rule.Analyzer.Name}");
        }
        else
        {
            Console.WriteLine($"Target kind:   {rule.Target!.Kind}");
            Console.WriteLine($"Assertions:    {string.Join(", ", rule.Assertions!.Select(a => a.Kind))}");
        }
        if (rule.Remediation is not null)
        {
            Console.WriteLine($"Remediation:   {rule.Remediation.Trim()}");
        }
        if (rule.Documentation.Count > 0)
        {
            Console.WriteLine($"Documentation: {string.Join(", ", rule.Documentation)}");
        }
    }
}
