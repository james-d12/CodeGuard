using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Assertions;

namespace RulesEngine.Evaluation.Tests.Assertions;

public class AttributeAccessorTests
{
    private static AttributeModel Obsolete(string reason) => new(
        "System.ObsoleteAttribute", [reason], new Dictionary<string, string>());

    private static MethodModel Method(IReadOnlyList<AttributeModel> attributes) =>
        new("Save", "System.Void", [], Accessibility.Public, MethodModifiers.None, attributes, "Contoso.Domain.Order", "Contoso.Domain", "Order.cs", 1, 1);

    private static PropertyModel Property(IReadOnlyList<AttributeModel> attributes) =>
        new("Name", "System.String", Accessibility.Public, true, true, null, false, false, false, attributes, "Contoso.Domain.Order", "Contoso.Domain", "Order.cs", 1, 1);

    private static ConstructorModel Constructor(IReadOnlyList<AttributeModel> attributes) =>
        new(Accessibility.Public, [], attributes, "Contoso.Domain.Order", "Contoso.Domain", "Order.cs", 1, 1);

    private static FieldModel Field(IReadOnlyList<AttributeModel> attributes) =>
        new("_name", "System.String", Accessibility.Private, FieldModifiers.None, attributes, "Contoso.Domain.Order", "Contoso.Domain", "Order.cs", 1, 1);

    private static ParameterModel Parameter(IReadOnlyList<AttributeModel> attributes) =>
        new("name", "System.String", attributes, false);

    [Fact]
    public void GetAttributes_ReturnsAttributes_ForType()
    {
        IReadOnlyList<AttributeModel> expected = [Obsolete("legacy")];
        var type = TestModels.Type("Contoso.Domain.Legacy") with { Attributes = expected };

        Assert.Same(expected, AttributeAccessor.GetAttributes(type));
    }

    [Fact]
    public void GetAttributes_ReturnsAttributes_ForMethod()
    {
        IReadOnlyList<AttributeModel> expected = [Obsolete("legacy")];

        Assert.Same(expected, AttributeAccessor.GetAttributes(Method(expected)));
    }

    [Fact]
    public void GetAttributes_ReturnsAttributes_ForProperty()
    {
        IReadOnlyList<AttributeModel> expected = [Obsolete("legacy")];

        Assert.Same(expected, AttributeAccessor.GetAttributes(Property(expected)));
    }

    [Fact]
    public void GetAttributes_ReturnsAttributes_ForConstructor()
    {
        IReadOnlyList<AttributeModel> expected = [Obsolete("legacy")];

        Assert.Same(expected, AttributeAccessor.GetAttributes(Constructor(expected)));
    }

    [Fact]
    public void GetAttributes_ReturnsAttributes_ForField()
    {
        IReadOnlyList<AttributeModel> expected = [Obsolete("legacy")];

        Assert.Same(expected, AttributeAccessor.GetAttributes(Field(expected)));
    }

    [Fact]
    public void GetAttributes_ReturnsAttributes_ForParameter()
    {
        IReadOnlyList<AttributeModel> expected = [Obsolete("legacy")];

        Assert.Same(expected, AttributeAccessor.GetAttributes(Parameter(expected)));
    }

    [Fact]
    public void GetAttributes_ReturnsNull_ForUnsupportedCandidate()
    {
        Assert.Null(AttributeAccessor.GetAttributes(42));
    }

    [Fact]
    public void HasArgument_ReturnsTrue_WhenPositionalConstructorArgumentMatches()
    {
        var attribute = Obsolete("use something else");

        Assert.True(AttributeAccessor.HasArgument(attribute, "use something else"));
    }

    [Fact]
    public void HasArgument_ReturnsTrue_WhenNamedArgumentValueMatches()
    {
        var attribute = new AttributeModel("Custom", [], new Dictionary<string, string> { ["Reason"] = "use something else" });

        Assert.True(AttributeAccessor.HasArgument(attribute, "use something else"));
    }

    [Fact]
    public void HasArgument_ReturnsFalse_WhenNeitherPositionalNorNamedMatches()
    {
        var attribute = new AttributeModel("Custom", ["other"], new Dictionary<string, string> { ["Reason"] = "other" });

        Assert.False(AttributeAccessor.HasArgument(attribute, "use something else"));
    }
}
