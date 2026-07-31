using CodeGuard.RuleModel.Rules;

namespace CodeGuard.Configuration.Validation;

public sealed record RuleSetValidationReport(
    IReadOnlyList<(RuleDefinition Rule, string SourceFile)> Rules,
    IReadOnlyList<RuleFileIssue> Issues)
{
    public bool IsValid => Issues.Count == 0;
}
