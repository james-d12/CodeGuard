using CodeGuard.Configuration.Validation;

namespace CodeGuard.Configuration.Parsing;

/// <summary>
/// Thrown when a structurally valid rule document can't be turned into a <c>RuleDefinition</c> -
/// an unregistered kind, or a missing/malformed parameter. Carries a <see cref="Code"/> and
/// <see cref="Path"/> so the failure survives into machine-readable output instead of collapsing
/// into a bare message.
/// </summary>
public sealed class RuleParsingException(
    string message,
    string code = RuleErrorCodes.ParseError,
    string? path = null) : Exception(message)
{
    public string Code { get; } = code;

    public string? Path { get; } = path;

    public RuleValidationError ToValidationError() => new(Code, Path, Message);
}
