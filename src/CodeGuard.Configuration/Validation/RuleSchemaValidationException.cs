namespace CodeGuard.Configuration.Validation;

public sealed class RuleSchemaValidationException(string source, IReadOnlyList<RuleValidationError> errors)
    : Exception(BuildMessage(source, errors))
{
    public string DocumentSource { get; } = source;
    public IReadOnlyList<RuleValidationError> Errors { get; } = errors;

    private static string BuildMessage(string source, IReadOnlyList<RuleValidationError> errors) =>
        $"Rule document '{source}' failed schema validation:{Environment.NewLine}" +
        string.Join(Environment.NewLine, errors.Select(e => $"  - {e}"));
}
