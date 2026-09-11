namespace CodeGuard.Configuration.Validation;

/// <summary>
/// Stable machine-readable codes for rule validation failures. These are part of the CLI's JSON
/// contract - an agent branches on the code rather than pattern-matching a human message, so renaming
/// one is a breaking change.
/// </summary>
public static class RuleErrorCodes
{
    /// <summary>The document does not conform to rule.schema.json.</summary>
    public const string SchemaViolation = "SCHEMA_VIOLATION";

    public const string UnknownSelectorKind = "UNKNOWN_SELECTOR_KIND";
    public const string UnknownAssertionKind = "UNKNOWN_ASSERTION_KIND";
    public const string UnknownAnalyzerKind = "UNKNOWN_ANALYZER_KIND";

    /// <summary>A required parameter was absent, or a present one was malformed.</summary>
    public const string InvalidParameter = "INVALID_PARAMETER";

    /// <summary>Two rule files in the same rule set declare the same id.</summary>
    public const string DuplicateRuleId = "DUPLICATE_RULE_ID";

    /// <summary>The file is empty, unreadable, or not parseable as YAML.</summary>
    public const string UnreadableRuleFile = "UNREADABLE_RULE_FILE";

    /// <summary>A parse failure with no more specific code.</summary>
    public const string ParseError = "PARSE_ERROR";
}

/// <summary>
/// One validation failure. <paramref name="Path"/> is a JSON Pointer into the rule document
/// (e.g. <c>/assertions/0</c>) where the failure can be located, and null where it cannot - it is
/// what lets an agent edit the offending node instead of re-reading the whole file.
/// </summary>
public sealed record RuleValidationError(string Code, string? Path, string Message)
{
    public override string ToString() => Path is null ? Message : $"{Path}: {Message}";
}
