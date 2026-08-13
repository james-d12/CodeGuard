using System.Text.Json.Nodes;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

/// <summary>
/// Wires up the selector/assertion parsers for every kind currently implemented in
/// CodeGuard.Evaluation. Extend this alongside new Evaluation implementations.
/// </summary>
public static class DefaultParsers
{
    public static SelectorParserRegistry CreateSelectorRegistry() => new(
    [
        new ClassInNamespaceSelectorParser(),
        new TypeSelectorParser(),
        new ProjectSelectorParser(),
        new InheritsFromSelectorParser(),
        new ImplementsSelectorParser(),
        new RecordSelectorParser(),
        new EnumSelectorParser(),
        new FileSelectorParser(),
        new RepositorySelectorParser(),
        new MethodSelectorParser(),
        new PropertySelectorParser(),
        new ConstructorSelectorParser(),
        new FieldSelectorParser(),
        new CallSiteSelectorParser(),
        new SwitchSelectorParser(),
        new ThrowSiteSelectorParser(),
        new MutationSiteSelectorParser(),
        new TryBlockSelectorParser(),
        new MethodBodyShapeSelectorParser(),
        new DiagnosticSelectorParser(),
        new DirectorySelectorParser()
    ]);

    public static AssertionParserRegistry CreateAssertionRegistry(SelectorParserRegistry selectorParsers)
    {
        // must_all_match/must_any_match/must_none_match parse a nested `assertions:` list via the
        // very AssertionParserRegistry being constructed here (any assertion kind can nest inside
        // one of these). That's a genuine self-reference, not just deep recursion, so `registry` is
        // captured by a local function rather than passed directly - by the time ParseNested is
        // actually invoked (during a later rule-file parse), `registry` is already assigned; it's
        // only unassigned during this constructor call, which ParseNested never runs during.
        AssertionParserRegistry? registry = null;
        IAssertion ParseNested(JsonObject node) => registry!.Parse(node);

        registry = new AssertionParserRegistry(
        [
            new MustInheritFromAssertionParser(),
            new MustImplementAssertionParser(),
            new MustHaveMethodAssertionParser(),
            new MustHavePropertyAssertionParser(),
            new MustHaveConstructorAssertionParser(),
            new MustBeInNamespaceAssertionParser(),
            new MustBeInProjectAssertionParser(),
            new MustReferencePackageAssertionParser(),
            new MustNotReferencePackageAssertionParser(),
            new MustReferenceProjectAssertionParser(),
            new MustNotReferenceProjectAssertionParser(),
            new MustNotDependOnAssertionParser(),
            new MustHaveMsBuildPropertyAssertionParser(),
            new MustHaveFileAssertionParser(),
            new MustNotHaveFileAssertionParser(),
            new MustHaveDirectoryAssertionParser(),
            new MustMatchContentAssertionParser(),
            new MustNotMatchContentAssertionParser(),
            new MustHaveJsonFieldAssertionParser(),
            new MustNotHaveJsonFieldAssertionParser(),
            new MustNotHaveMethodAssertionParser(),
            new MustNotHavePropertyAssertionParser(),
            new MustNotInheritFromAssertionParser(),
            new MustNotImplementAssertionParser(),
            new MustHaveParameterCountAssertionParser(),
            new MustMatchFilenameAssertionParser(),
            new MustMatchNameAssertionParser(),
            new MustHaveModifierAssertionParser(),
            new MustNotHaveModifierAssertionParser(),
            new MustHaveAttributeAssertionParser(),
            new MustNotHaveAttributeAssertionParser(),
            new MustExistAssertionParser(selectorParsers),
            new MustNotExistAssertionParser(selectorParsers),
            new MustMatchArgumentAssertionParser(),
            new MustHaveCountAssertionParser(selectorParsers),
            new MustDependOnAssertionParser(),
            new MustOnlyDependOnAssertionParser(),
            new MustHaveFieldAssertionParser(),
            new MustNotHaveFieldAssertionParser(),
            new MustNotBeInNamespaceAssertionParser(),
            new MustMatchNamespacePatternAssertionParser(),
            new MustUsePackageVersionAssertionParser(),
            new MustAllMatchAssertionParser(selectorParsers, ParseNested),
            new MustAnyMatchAssertionParser(selectorParsers, ParseNested),
            new MustNoneMatchAssertionParser(selectorParsers, ParseNested)
        ]);
        return registry;
    }

    public static ConditionParserRegistry CreateConditionRegistry(AssertionParserRegistry assertionParsers) =>
        new(assertionParsers);
}
