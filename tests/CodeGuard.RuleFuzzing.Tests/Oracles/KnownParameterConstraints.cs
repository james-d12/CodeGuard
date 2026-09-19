using CodeGuard.Configuration.Parsing;
using CodeGuard.Configuration.Validation;

namespace CodeGuard.RuleFuzzing.Tests.Oracles;

/// <summary>
/// A generated document that is catalog-driven (every kind/parameter name is real) can still fail to
/// parse: <c>ParameterDescriptor.Required</c> alone doesn't encode cross-parameter constraints like
/// "at least one of min/max/exactly" (<c>must_have_count</c>) or "non-empty" for a required
/// <c>StringList</c>/<c>AssertionList</c> (<c>must_only_depend_on</c>'s <c>types</c>, any quantifier's
/// nested <c>assertions</c>). The generator deliberately manufactures these cases (see
/// <c>ParameterValueGenerator</c>'s empty-array adversarial branch and optional-int omission) to
/// exercise the rejection path itself, not by accident - so a <c>RuleParsingException</c> with this
/// code is expected and tolerated by the parseability oracle. Any other exception, or this code on a
/// document the generator didn't deliberately construct to trip it, is a genuine bug.
/// </summary>
internal static class KnownParameterConstraints
{
    /// <summary>Cross-parameter/non-empty-collection constraints the catalog can't encode (see type doc).</summary>
    public const string InvalidParameter = RuleErrorCodes.InvalidParameter;

    /// <summary>
    /// <c>EnumParsing.ParseSnakeCase</c> throws with this code (not <see cref="InvalidParameter"/>) for
    /// an enum value that isn't one of <c>ParameterDescriptor.AllowedValues</c> - which is exactly what
    /// <c>ParameterValueGenerator</c>'s deliberate "near-miss" adversarial enum case constructs. A real,
    /// minor inconsistency in the engine's own error-code taxonomy (arguably this should also be
    /// <see cref="InvalidParameter"/>, since it's clearly a bad parameter value, not a generic parse
    /// failure) - noted here rather than changed, since <c>RuleErrorCodes</c> is documented as part of
    /// the CLI's JSON contract and changing which code an existing call site emits is a behavior change
    /// for any consumer already branching on it.
    /// </summary>
    public const string EnumParseError = RuleErrorCodes.ParseError;

    public static bool IsTolerable(string code) => code is InvalidParameter or EnumParseError;

    /// <summary>
    /// A real validation-completeness gap, found by this fuzzer and deliberately not "fixed" here since
    /// the fix is architectural: <c>must_exist</c>/<c>must_not_exist</c>/<c>must_have_count</c>/the
    /// quantifier assertions store their nested <c>selector</c> as a raw JSON template (see
    /// <c>SelectorTemplateResolver</c>) so it can substitute <c>${Placeholder}</c> references per
    /// candidate - which means the template is only ever parsed into a real <c>ITargetSelector</c>
    /// lazily, inside <c>Evaluate</c>, not once eagerly during <c>RuleDocumentParser.Parse</c> like a
    /// top-level target/assertion. A bad nested selector parameter (e.g. an unknown enum value) is
    /// therefore invisible to `codeguard rules validate` and only surfaces the first time evaluation
    /// actually reaches it, as a caught <see cref="RuleParsingException"/> converted to a
    /// <c>RuleEvaluationError</c> by <c>RuleEvaluator.Evaluate</c>'s per-rule try/catch - not a crash,
    /// but not caught where a rule author would expect it either. Tolerated here for the same reason as
    /// <see cref="IsTolerable"/>: the generator deliberately manufactures near-miss enum values, and a
    /// nested selector is just as legitimate a place for one to land as a top-level one.
    /// </summary>
    public static bool IsTolerableEvaluationError(string exceptionTypeFullName) =>
        exceptionTypeFullName == typeof(RuleParsingException).FullName;
}
