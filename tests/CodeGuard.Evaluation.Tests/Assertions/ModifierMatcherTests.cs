using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Assertions;

namespace CodeGuard.Evaluation.Tests.Assertions;

public class ModifierMatcherTests
{
    private static TypeModel Type(TypeModifiers modifiers = TypeModifiers.None, TypeKind kind = TypeKind.Class) =>
        TestModels.Type("Contoso.Domain.Order", kind) with { Modifiers = modifiers };

    private static MethodModel Method(MethodModifiers modifiers = MethodModifiers.None) =>
        new("Save", "System.Void", [], Accessibility.Public, modifiers, [], "Contoso.Domain.Order", "Contoso.Domain", "Order.cs", 1, 1);

    private static FieldModel Field(FieldModifiers modifiers = FieldModifiers.None) =>
        new("_name", "System.String", Accessibility.Private, modifiers, [], "Contoso.Domain.Order", "Contoso.Domain", "Order.cs", 1, 1);

    private static PropertyModel Property(bool isStatic = false, bool isRequired = false, bool isInit = false) =>
        new("Name", "System.String", Accessibility.Public, true, true, null, isRequired, isInit, isStatic, [], "Contoso.Domain.Order", "Contoso.Domain", "Order.cs", 1, 1);

    [Fact]
    public void Matches_ReturnsNull_ForUnsupportedCandidate()
    {
        Assert.Null(ModifierMatcher.Matches(42, "static"));
    }

    [Theory]
    [InlineData(TypeKind.Record, TypeModifiers.None, "record", true)]
    [InlineData(TypeKind.Class, TypeModifiers.None, "record", false)]
    [InlineData(TypeKind.Class, TypeModifiers.Sealed, "sealed", true)]
    [InlineData(TypeKind.Class, TypeModifiers.None, "sealed", false)]
    [InlineData(TypeKind.Class, TypeModifiers.Abstract, "abstract", true)]
    [InlineData(TypeKind.Class, TypeModifiers.None, "abstract", false)]
    [InlineData(TypeKind.Class, TypeModifiers.Static, "static", true)]
    [InlineData(TypeKind.Class, TypeModifiers.None, "static", false)]
    [InlineData(TypeKind.Class, TypeModifiers.Partial, "partial", true)]
    [InlineData(TypeKind.Class, TypeModifiers.None, "partial", false)]
    public void Matches_EvaluatesTypeModifiers(TypeKind kind, TypeModifiers modifiers, string modifierName, bool expected)
    {
        Assert.Equal(expected, ModifierMatcher.Matches(Type(modifiers, kind), modifierName));
    }

    [Fact]
    public void Matches_ReturnsNull_ForUnknownTypeModifierName()
    {
        Assert.Null(ModifierMatcher.Matches(Type(), "unknown"));
    }

    [Theory]
    [InlineData(MethodModifiers.Static, "static", true)]
    [InlineData(MethodModifiers.None, "static", false)]
    [InlineData(MethodModifiers.Abstract, "abstract", true)]
    [InlineData(MethodModifiers.None, "abstract", false)]
    [InlineData(MethodModifiers.Virtual, "virtual", true)]
    [InlineData(MethodModifiers.None, "virtual", false)]
    [InlineData(MethodModifiers.Override, "override", true)]
    [InlineData(MethodModifiers.None, "override", false)]
    [InlineData(MethodModifiers.Async, "async", true)]
    [InlineData(MethodModifiers.None, "async", false)]
    public void Matches_EvaluatesMethodModifiers(MethodModifiers modifiers, string modifierName, bool expected)
    {
        Assert.Equal(expected, ModifierMatcher.Matches(Method(modifiers), modifierName));
    }

    [Fact]
    public void Matches_ReturnsNull_ForUnknownMethodModifierName()
    {
        Assert.Null(ModifierMatcher.Matches(Method(), "unknown"));
    }

    [Theory]
    [InlineData(FieldModifiers.Static, "static", true)]
    [InlineData(FieldModifiers.None, "static", false)]
    [InlineData(FieldModifiers.Const, "const", true)]
    [InlineData(FieldModifiers.None, "const", false)]
    [InlineData(FieldModifiers.Readonly, "readonly", true)]
    [InlineData(FieldModifiers.None, "readonly", false)]
    public void Matches_EvaluatesFieldModifiers(FieldModifiers modifiers, string modifierName, bool expected)
    {
        Assert.Equal(expected, ModifierMatcher.Matches(Field(modifiers), modifierName));
    }

    [Fact]
    public void Matches_ReturnsNull_ForUnknownFieldModifierName()
    {
        Assert.Null(ModifierMatcher.Matches(Field(), "unknown"));
    }

    [Fact]
    public void Matches_EvaluatesPropertyStaticModifier()
    {
        Assert.True(ModifierMatcher.Matches(Property(isStatic: true), "static"));
        Assert.False(ModifierMatcher.Matches(Property(isStatic: false), "static"));
    }

    [Fact]
    public void Matches_EvaluatesPropertyRequiredModifier()
    {
        Assert.True(ModifierMatcher.Matches(Property(isRequired: true), "required"));
        Assert.False(ModifierMatcher.Matches(Property(isRequired: false), "required"));
    }

    [Fact]
    public void Matches_EvaluatesPropertyInitModifier()
    {
        Assert.True(ModifierMatcher.Matches(Property(isInit: true), "init"));
        Assert.False(ModifierMatcher.Matches(Property(isInit: false), "init"));
    }

    [Fact]
    public void Matches_ReturnsNull_ForUnknownPropertyModifierName()
    {
        Assert.Null(ModifierMatcher.Matches(Property(), "unknown"));
    }
}
