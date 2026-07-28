using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Analyzers;

namespace RulesEngine.Evaluation.Tests.Analyzers;

public class MemberOrderingAnalyzerTests
{
    private const string DeclaringType = "Contoso.Domain.Order";

    private static FieldModel Field(int line) => new(
        Name: "_total", Type: "decimal", Accessibility: Accessibility.Private, Modifiers: FieldModifiers.None,
        Attributes: [], DeclaringType: DeclaringType, ProjectName: "Contoso.Domain", FilePath: "Order.cs", Line: line, Column: 1);

    private static ConstructorModel Constructor(int line) => new(
        Accessibility: Accessibility.Public, Parameters: [], Attributes: [],
        DeclaringType: DeclaringType, ProjectName: "Contoso.Domain", FilePath: "Order.cs", Line: line, Column: 1);

    private static PropertyModel Property(int line) => new(
        Name: "Total", Type: "decimal", Accessibility: Accessibility.Public, HasGetter: true, HasSetter: false,
        SetterAccessibility: null, IsRequired: false, IsInit: false, IsStatic: false, Attributes: [],
        DeclaringType: DeclaringType, ProjectName: "Contoso.Domain", FilePath: "Order.cs", Line: line, Column: 1);

    private static MethodModel Method(int line) => new(
        Name: "Recalculate", ReturnType: "void", Parameters: [], Accessibility: Accessibility.Public,
        Modifiers: MethodModifiers.None, Attributes: [], DeclaringType: DeclaringType,
        ProjectName: "Contoso.Domain", FilePath: "Order.cs", Line: line, Column: 1);

    [Fact]
    public void Analyze_DoesNotFlag_TypeWithFieldsConstructorsPropertiesMethodsInOrder()
    {
        var type = TestModels.Type(
            DeclaringType,
            fields: [Field(1)],
            constructors: [Constructor(2)],
            properties: [Property(3)],
            methods: [Method(4)]);
        var project = TestModels.Project("Contoso.Domain", types: [type]);
        var model = TestModels.RepositoryWithFacts(projects: [project]);
        var analyzer = new MemberOrderingAnalyzer();

        var violations = analyzer.Analyze(model).ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Analyze_Flags_TypeWithMethodBeforeField()
    {
        var type = TestModels.Type(
            DeclaringType,
            fields: [Field(10)],
            methods: [Method(1)]);
        var project = TestModels.Project("Contoso.Domain", types: [type]);
        var model = TestModels.RepositoryWithFacts(projects: [project]);
        var analyzer = new MemberOrderingAnalyzer();

        var violations = analyzer.Analyze(model).ToList();

        var violation = Assert.Single(violations);
        Assert.Equal("Order.cs", violation.FilePath);
        Assert.Equal(10, violation.Line);
        Assert.Contains("Contoso.Domain.Order._total", violation.Message);
    }

    [Fact]
    public void Analyze_DoesNotFlag_MethodBeforeField_WhenCustomOrderAllowsIt()
    {
        var type = TestModels.Type(
            DeclaringType,
            fields: [Field(10)],
            methods: [Method(1)]);
        var project = TestModels.Project("Contoso.Domain", types: [type]);
        var model = TestModels.RepositoryWithFacts(projects: [project]);
        var analyzer = new MemberOrderingAnalyzer(["methods", "fields", "constructors", "properties"]);

        var violations = analyzer.Analyze(model).ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Name_IsMemberOrdering()
    {
        var analyzer = new MemberOrderingAnalyzer();

        Assert.Equal("member-ordering", analyzer.Name);
    }
}
