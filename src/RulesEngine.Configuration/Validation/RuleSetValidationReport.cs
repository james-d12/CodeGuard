using RulesEngine.RuleModel.Rules;

namespace RulesEngine.Configuration.Validation;

public sealed record RuleSetValidationReport(
    IReadOnlyList<(RuleDefinition Rule, string SourceFile)> Rules,
    IReadOnlyList<RuleFileIssue> Issues)
{
    public bool IsValid => Issues.Count == 0;
}
