namespace RulesEngine.Configuration.Parsing;

/// <summary>
/// Wires up the selector/assertion parsers for every kind currently implemented in
/// RulesEngine.Evaluation. Extend this alongside new Evaluation implementations.
/// </summary>
public static class DefaultParsers
{
    public static SelectorParserRegistry CreateSelectorRegistry() => new(
    [
        new ClassInNamespaceSelectorParser(),
        new TypeSelectorParser(),
        new ProjectSelectorParser(),
        new InheritsFromSelectorParser(),
        new ImplementsSelectorParser()
    ]);

    public static AssertionParserRegistry CreateAssertionRegistry() => new(
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
        new MustNotDependOnAssertionParser()
    ]);
}
