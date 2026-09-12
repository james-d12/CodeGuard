namespace CodeGuard.Configuration.Capabilities;

/// <summary>
/// The kind of candidate a selector yields, and therefore the kind an assertion has to be able to
/// handle. Assertions type-check their candidate at runtime (<c>if (candidate is not TypeModel)</c>),
/// so pairing a selector with an assertion that can't accept what it produces is a rule that always
/// fails - declaring both ends here is what lets that be caught statically.
/// </summary>
public enum CandidateKind
{
    Repository,
    Project,
    Type,
    Method,
    Property,
    Constructor,
    Field,
    File,
    Directory,
    CallSite,
    Switch,
    ThrowSite,
    MutationSite,
    TryBlock,
    MethodBodyShape,
    Diagnostic
}

/// <summary>How a parameter's value is written in YAML, for documentation and tooling.</summary>
public enum ParameterType
{
    /// <summary>Matched literally.</summary>
    String,

    /// <summary>Matched with <c>GlobMatcher</c> - <c>*</c> wildcard only, no <c>?</c>/<c>**</c>/regex.</summary>
    Glob,

    /// <summary>A .NET regular expression.</summary>
    Regex,

    Bool,
    Int,

    /// <summary>An array of strings.</summary>
    StringList,

    /// <summary>One of <see cref="ParameterDescriptor.AllowedValues"/>, written snake_case.</summary>
    Enum,

    /// <summary>A nested <c>target</c>-style selector object (any registered selector kind).</summary>
    Selector,

    /// <summary>A nested, non-empty <c>assertions</c> list.</summary>
    AssertionList
}

/// <summary>One parameter of a selector, assertion or analyzer.</summary>
/// <param name="Name">The YAML key, snake_case.</param>
/// <param name="Required">
/// Whether omitting it is a parse error. An optional parameter should state its
/// <paramref name="Default"/> where it has a meaningful one.
/// </param>
public sealed record ParameterDescriptor(
    string Name,
    ParameterType Type,
    bool Required,
    string Summary,
    string? Default = null,
    IReadOnlyList<string>? AllowedValues = null)
{
    public static ParameterDescriptor RequiredGlob(string name, string summary) =>
        new(name, ParameterType.Glob, true, summary);

    public static ParameterDescriptor OptionalGlob(string name, string summary, string @default = "*") =>
        new(name, ParameterType.Glob, false, summary, @default);

    public static ParameterDescriptor OptionalBool(string name, string summary) =>
        new(name, ParameterType.Bool, false, summary);

    public static ParameterDescriptor OptionalInt(string name, string summary) =>
        new(name, ParameterType.Int, false, summary);
}

/// <summary>
/// The declarative description of one registered <c>kind</c>. Parsers read their parameters
/// imperatively, so without this the engine's vocabulary is only discoverable by reading source -
/// which is how the authoring skill's reference docs drifted 18 primitives behind the engine.
/// This is the single source both <c>codeguard rules discover</c> and those generated docs read from.
/// </summary>
/// <param name="Parameters">Declared in the order a human would write them.</param>
public sealed record CapabilityDescriptor(
    string Kind,
    string Summary,
    IReadOnlyList<ParameterDescriptor> Parameters)
{
    /// <summary>Set on selectors: the candidate kind this selector yields. Null for assertions and analyzers.</summary>
    public CandidateKind? Produces { get; init; }

    /// <summary>
    /// Set on assertions: the candidate kinds this assertion accepts. Empty means "any" - used by the
    /// existence/quantifier assertions, which ignore the outer candidate entirely.
    /// </summary>
    public IReadOnlyList<CandidateKind> AppliesTo { get; init; } = [];
}
