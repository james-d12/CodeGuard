namespace CodeGuard.Configuration.Validation;

public sealed record RuleFileIssue(string SourceFile, IReadOnlyList<string> Errors);
