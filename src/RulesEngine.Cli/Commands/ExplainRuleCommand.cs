using System.CommandLine;
using RulesEngine.Cli.Support;
using RulesEngine.RuleModel.Rules;

namespace RulesEngine.Cli.Commands;

public static class ExplainRuleCommand
{
    public static Command Build()
    {
        var pathOption = CommonOptions.CreatePathOption();
        var configOption = CommonOptions.CreateConfigOption();
        var ruleIdArgument = new Argument<string>("ruleId")
        {
            Description = "The rule ID to explain, e.g. DDD-ENTITY-001."
        };

        var command = new Command("explain-rule", "Print full metadata and source YAML for a single rule");
        command.Add(pathOption);
        command.Add(configOption);
        command.Add(ruleIdArgument);

        command.SetAction((parseResult, cancellationToken) =>
        {
            var context = CliRepositoryContext.Resolve(
                parseResult.GetValue(pathOption),
                parseResult.GetValue(configOption));

            var ruleId = parseResult.GetValue(ruleIdArgument)!;
            var entries = context.LoadRulesWithSource();
            var entry = entries.FirstOrDefault(e => e.Rule.Id == ruleId);

            if (entry.Rule is null)
            {
                System.Console.Error.WriteLine($"Rule '{ruleId}' was not found under {string.Join(", ", context.Layout.RulesPaths)}.");
                return Task.FromResult(1);
            }

            PrintSummary(entry.Rule);
            System.Console.WriteLine();
            System.Console.WriteLine($"Source: {entry.SourceFile}");
            System.Console.WriteLine();
            System.Console.WriteLine("--- Raw YAML ---");
            System.Console.WriteLine(File.ReadAllText(entry.SourceFile));

            return Task.FromResult(0);
        });

        return command;
    }

    private static void PrintSummary(RuleDefinition rule)
    {
        System.Console.WriteLine($"Id:            {rule.Id}");
        System.Console.WriteLine($"Name:          {rule.Name}");
        if (rule.Description is not null)
        {
            System.Console.WriteLine($"Description:   {rule.Description.Trim()}");
        }
        System.Console.WriteLine($"Standard:      {rule.Standard ?? "-"}");
        System.Console.WriteLine($"Severity:      {rule.Severity}");
        System.Console.WriteLine($"Enforcement:   {rule.Enforcement.Classification}");
        System.Console.WriteLine($"Tags:          {(rule.Tags.Count == 0 ? "-" : string.Join(", ", rule.Tags))}");
        System.Console.WriteLine($"Enabled:       {rule.Enabled}");
        System.Console.WriteLine($"Illustrative:  {rule.Illustrative}");
        System.Console.WriteLine($"Target kind:   {rule.Target.Kind}");
        System.Console.WriteLine($"Assertions:    {string.Join(", ", rule.Assertions.Select(a => a.Kind))}");
        if (rule.Remediation is not null)
        {
            System.Console.WriteLine($"Remediation:   {rule.Remediation.Trim()}");
        }
        if (rule.Documentation.Count > 0)
        {
            System.Console.WriteLine($"Documentation: {string.Join(", ", rule.Documentation)}");
        }
    }
}
