using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Analyzers;

namespace RulesEngine.Evaluation.Tests.Analyzers;

public class DuplicateAttributeArgumentAnalyzerTests
{
    private static AttributeModel EventIdAttribute(string id) => new(
        TypeName: "EventIdAttribute",
        ConstructorArgumentLiterals: [id],
        NamedArguments: new Dictionary<string, string>());

    private static MethodModel Method(string name, string declaringType, string id) => new(
        Name: name, ReturnType: "void", Parameters: [], Accessibility: Accessibility.Public,
        Modifiers: MethodModifiers.None, Attributes: [EventIdAttribute(id)], DeclaringType: declaringType,
        ProjectName: "Contoso.Reporting", FilePath: $"{declaringType}.cs", Line: 5, Column: 1);

    [Fact]
    public void Analyze_Flags_MembersSharingTheSameAttributeArgumentValue()
    {
        var typeA = TestModels.Type("Contoso.Reporting.OrderCreatedHandler", methods: [Method("Handle", "Contoso.Reporting.OrderCreatedHandler", "100")]);
        var typeB = TestModels.Type("Contoso.Reporting.OrderShippedHandler", methods: [Method("Handle", "Contoso.Reporting.OrderShippedHandler", "100")]);
        var project = TestModels.Project("Contoso.Reporting", types: [typeA, typeB]);
        var model = TestModels.RepositoryWithFacts(projects: [project]);
        var analyzer = new DuplicateAttributeArgumentAnalyzer("EventIdAttribute");

        var violations = analyzer.Analyze(model).ToList();

        Assert.Equal(2, violations.Count);
        Assert.Contains(violations, v => v.Message.Contains("Contoso.Reporting.OrderCreatedHandler.Handle"));
        Assert.Contains(violations, v => v.Message.Contains("Contoso.Reporting.OrderShippedHandler.Handle"));
    }

    [Fact]
    public void Analyze_Flags_MembersSharingTheSameAttributeArgumentValue_AcrossPropertiesAndFields()
    {
        var property = new PropertyModel(
            Name: "EventId", Type: "System.Int32", Accessibility: Accessibility.Public, HasGetter: true, HasSetter: false,
            SetterAccessibility: null, IsRequired: false, IsInit: false, IsStatic: false, Attributes: [EventIdAttribute("100")],
            DeclaringType: "Contoso.Reporting.OrderCreatedHandler", ProjectName: "Contoso.Reporting",
            FilePath: "OrderCreatedHandler.cs", Line: 5, Column: 1);
        var field = new FieldModel(
            Name: "_eventId", Type: "System.Int32", Accessibility: Accessibility.Private, Modifiers: FieldModifiers.None,
            Attributes: [EventIdAttribute("100")], DeclaringType: "Contoso.Reporting.OrderShippedHandler",
            ProjectName: "Contoso.Reporting", FilePath: "OrderShippedHandler.cs", Line: 5, Column: 1);
        var typeA = TestModels.Type("Contoso.Reporting.OrderCreatedHandler", properties: [property]);
        var typeB = TestModels.Type("Contoso.Reporting.OrderShippedHandler", fields: [field]);
        var project = TestModels.Project("Contoso.Reporting", types: [typeA, typeB]);
        var model = TestModels.RepositoryWithFacts(projects: [project]);
        var analyzer = new DuplicateAttributeArgumentAnalyzer("EventIdAttribute");

        var violations = analyzer.Analyze(model).ToList();

        Assert.Equal(2, violations.Count);
        Assert.Contains(violations, v => v.Message.Contains("Contoso.Reporting.OrderCreatedHandler.EventId"));
        Assert.Contains(violations, v => v.Message.Contains("Contoso.Reporting.OrderShippedHandler._eventId"));
    }

    [Fact]
    public void Analyze_Ignores_AttributesWithFewerArgumentsThanConfiguredIndex()
    {
        var attribute = new AttributeModel("RouteAttribute", ["GET"], new Dictionary<string, string>());
        var method = new MethodModel(
            Name: "Get", ReturnType: "void", Parameters: [], Accessibility: Accessibility.Public,
            Modifiers: MethodModifiers.None, Attributes: [attribute], DeclaringType: "Contoso.Api.OrdersController",
            ProjectName: "Contoso.Api", FilePath: "OrdersController.cs", Line: 5, Column: 1);
        var type = TestModels.Type("Contoso.Api.OrdersController", methods: [method]);
        var project = TestModels.Project("Contoso.Api", types: [type]);
        var model = TestModels.RepositoryWithFacts(projects: [project]);
        var analyzer = new DuplicateAttributeArgumentAnalyzer("RouteAttribute", argumentIndex: 1);

        var violations = analyzer.Analyze(model).ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Analyze_DoesNotFlag_MembersWithUniqueArgumentValues()
    {
        var typeA = TestModels.Type("Contoso.Reporting.OrderCreatedHandler", methods: [Method("Handle", "Contoso.Reporting.OrderCreatedHandler", "100")]);
        var typeB = TestModels.Type("Contoso.Reporting.OrderShippedHandler", methods: [Method("Handle", "Contoso.Reporting.OrderShippedHandler", "200")]);
        var project = TestModels.Project("Contoso.Reporting", types: [typeA, typeB]);
        var model = TestModels.RepositoryWithFacts(projects: [project]);
        var analyzer = new DuplicateAttributeArgumentAnalyzer("EventIdAttribute");

        var violations = analyzer.Analyze(model).ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Analyze_Ignores_AttributesNotMatchingConfiguredPattern()
    {
        var typeA = TestModels.Type("Contoso.Reporting.OrderCreatedHandler", methods: [Method("Handle", "Contoso.Reporting.OrderCreatedHandler", "100")]);
        var typeB = TestModels.Type("Contoso.Reporting.OrderShippedHandler", methods: [Method("Handle", "Contoso.Reporting.OrderShippedHandler", "100")]);
        var project = TestModels.Project("Contoso.Reporting", types: [typeA, typeB]);
        var model = TestModels.RepositoryWithFacts(projects: [project]);
        var analyzer = new DuplicateAttributeArgumentAnalyzer("SomeOtherAttribute");

        var violations = analyzer.Analyze(model).ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Analyze_UsesConfiguredArgumentIndex_NotAlwaysTheFirst()
    {
        var sharedSecond = new AttributeModel("RouteAttribute", ["GET", "/orders/duplicate"], new Dictionary<string, string>());
        var methodA = new MethodModel(
            Name: "Get", ReturnType: "void", Parameters: [], Accessibility: Accessibility.Public,
            Modifiers: MethodModifiers.None, Attributes: [sharedSecond], DeclaringType: "Contoso.Api.OrdersController",
            ProjectName: "Contoso.Api", FilePath: "OrdersController.cs", Line: 5, Column: 1);
        var methodB = new MethodModel(
            Name: "GetAll", ReturnType: "void", Parameters: [], Accessibility: Accessibility.Public,
            Modifiers: MethodModifiers.None, Attributes: [sharedSecond], DeclaringType: "Contoso.Api.OrdersController",
            ProjectName: "Contoso.Api", FilePath: "OrdersController.cs", Line: 10, Column: 1);
        var type = TestModels.Type("Contoso.Api.OrdersController", methods: [methodA, methodB]);
        var project = TestModels.Project("Contoso.Api", types: [type]);
        var model = TestModels.RepositoryWithFacts(projects: [project]);
        var analyzer = new DuplicateAttributeArgumentAnalyzer("RouteAttribute", argumentIndex: 1);

        var violations = analyzer.Analyze(model).ToList();

        Assert.Equal(2, violations.Count);
        Assert.All(violations, v => Assert.Contains("/orders/duplicate", v.Message));
    }

    [Fact]
    public void Name_IsDuplicateAttributeArgument()
    {
        var analyzer = new DuplicateAttributeArgumentAnalyzer("EventIdAttribute");

        Assert.Equal("duplicate-attribute-argument", analyzer.Name);
    }
}
