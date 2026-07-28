namespace RulesEngine.Configuration.Parsing;

/// <summary>
/// Wires up the custom analyzer parsers currently implemented in RulesEngine.Evaluation.Analyzers.
/// Each analyzer is constructed per-rule from its YAML 'analyzer' node params (mirrors DefaultParsers'
/// selector/assertion registries) rather than a single shared instance, so scoping (namespace, etc.)
/// is a rule-authoring decision, not something hardcoded into the analyzer's C#.
/// </summary>
public static class DefaultAnalyzers
{
    public static AnalyzerParserRegistry CreateRegistry() => new(
    [
        new RoslynDiagnosticPassthroughAnalyzerParser(),
        new ExhaustiveSwitchAnalyzerParser(),
        new NoExceptionsAnalyzerParser(),
        new ImmutableMutationAnalyzerParser(),
        new CatchClauseCountAnalyzerParser(),
        new MemberOrderingAnalyzerParser(),
        new NoPureDelegationOverrideAnalyzerParser(),
        new CompanionTypeCardinalityAnalyzerParser(),
        new DuplicateAttributeArgumentAnalyzerParser(),
        new ConstYamlValueConsistencyAnalyzerParser(),
        new ProjectConventionAnalyzerParser()
    ]);
}
