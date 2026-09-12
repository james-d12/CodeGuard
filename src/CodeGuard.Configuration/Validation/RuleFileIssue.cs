namespace CodeGuard.Configuration.Validation;

/// <summary>Every validation failure found in one rule file.</summary>
public sealed record RuleFileIssue(string SourceFile, IReadOnlyList<RuleValidationError> Errors);
