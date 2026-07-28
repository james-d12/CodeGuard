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
        new ImplementsSelectorParser(),
        new RecordSelectorParser(),
        new EnumSelectorParser(),
        new FileSelectorParser(),
        new RepositorySelectorParser(),
        new MethodSelectorParser(),
        new PropertySelectorParser(),
        new ConstructorSelectorParser(),
        new FieldSelectorParser(),
        new CallSiteSelectorParser()
    ]);

    public static AssertionParserRegistry CreateAssertionRegistry(SelectorParserRegistry selectorParsers) => new(
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
        new MustMatchArgumentAssertionParser()
    ]);

    public static ConditionParserRegistry CreateConditionRegistry(AssertionParserRegistry assertionParsers) =>
        new(assertionParsers);
}
