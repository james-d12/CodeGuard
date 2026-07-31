namespace CodeGuard.Configuration.Validation;

public sealed class RuleSchemaValidationException(string source, IReadOnlyList<string> errors)
    : Exception(BuildMessage(source, errors))
{
    public string DocumentSource { get; } = source;
    public IReadOnlyList<string> Errors { get; } = errors;

    private static string BuildMessage(string source, IReadOnlyList<string> errors) =>
        $"Rule document '{source}' failed schema validation:{Environment.NewLine}" +
        string.Join(Environment.NewLine, errors.Select(e => $"  - {e}"));
}
