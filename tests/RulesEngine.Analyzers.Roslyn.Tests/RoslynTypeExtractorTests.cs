using RulesEngine.Analysis.AnalysisModel;

namespace RulesEngine.Analyzers.Roslyn.Tests;

public class RoslynTypeExtractorTests
{
    private static TypeModel ExtractSingle(string source, string fullName)
    {
        var compilation = CompilationFactory.Create(source);
        var types = RoslynTypeExtractor.ExtractTypes(compilation, "Contoso.Domain");
        return types.Single(t => t.FullName == fullName);
    }

    [Fact]
    public void ExtractTypes_MapsBaseTypeAndInterfaces()
    {
        var type = ExtractSingle("""
            namespace Contoso.Domain.Entities;
            public interface IAggregateRoot { }
            public class Entity<TId> { }
            public class Order : Entity<int>, IAggregateRoot { }
            """, "Contoso.Domain.Entities.Order");

        Assert.Equal("Contoso.Domain.Entities.Entity<int>", type.BaseType);
        Assert.Contains("Contoso.Domain.Entities.IAggregateRoot", type.Interfaces);
    }

    [Fact]
    public void ExtractTypes_ObjectBaseTypeIsMappedToNull()
    {
        var type = ExtractSingle("""
            namespace Contoso.Domain;
            public class PlainClass { }
            """, "Contoso.Domain.PlainClass");

        Assert.Null(type.BaseType);
    }

    [Fact]
    public void ExtractTypes_DetectsRecordKind()
    {
        var type = ExtractSingle("""
            namespace Contoso.Domain;
            public record OrderPlaced(string OrderId);
            """, "Contoso.Domain.OrderPlaced");

        Assert.Equal(TypeKind.Record, type.Kind);
    }

    [Fact]
    public void ExtractTypes_DetectsInterfaceAndEnumKinds()
    {
        var compilation = CompilationFactory.Create("""
            namespace Contoso.Domain;
            public interface IThing { }
            public enum Status { Active, Inactive }
            """);
        var types = RoslynTypeExtractor.ExtractTypes(compilation, "Contoso.Domain");

        Assert.Equal(TypeKind.Interface, types.Single(t => t.FullName == "Contoso.Domain.IThing").Kind);
        Assert.Equal(TypeKind.Enum, types.Single(t => t.FullName == "Contoso.Domain.Status").Kind);
    }

    [Fact]
    public void ExtractTypes_MapsAttributes()
    {
        var type = ExtractSingle("""
            namespace Contoso.Domain;
            [System.Obsolete("use something else")]
            public class Legacy { }
            """, "Contoso.Domain.Legacy");

        var attribute = Assert.Single(type.Attributes);
        Assert.Equal("System.ObsoleteAttribute", attribute.TypeName);
        Assert.Contains("use something else", attribute.ConstructorArgumentLiterals);
    }

    [Fact]
    public void ExtractTypes_MapsModifiers()
    {
        var type = ExtractSingle("""
            namespace Contoso.Domain;
            public sealed class ValueObject { }
            """, "Contoso.Domain.ValueObject");

        Assert.True(type.Modifiers.HasFlag(TypeModifiers.Sealed));
        Assert.False(type.Modifiers.HasFlag(TypeModifiers.Abstract));
    }

    [Fact]
    public void ExtractTypes_DetectsPartialModifier()
    {
        var type = ExtractSingle("""
            namespace Contoso.Domain;
            public partial class Order { }
            """, "Contoso.Domain.Order");

        Assert.True(type.Modifiers.HasFlag(TypeModifiers.Partial));
    }

    [Fact]
    public void ExtractTypes_MapsMethodsWithReturnTypeAndParameters()
    {
        var type = ExtractSingle("""
            namespace Contoso.Domain;
            public class Aggregate
            {
                public static Aggregate Create(string name) => new();
            }
            """, "Contoso.Domain.Aggregate");

        var method = Assert.Single(type.Methods);
        Assert.Equal("Create", method.Name);
        Assert.Equal("Contoso.Domain.Aggregate", method.ReturnType);
        Assert.True(method.Modifiers.HasFlag(MethodModifiers.Static));
        var parameter = Assert.Single(method.Parameters);
        Assert.Equal("name", parameter.Name);
        Assert.Equal("string", parameter.Type);
    }

    [Fact]
    public void ExtractTypes_MapsPropertiesWithGetterSetterAccessibility()
    {
        var type = ExtractSingle("""
            namespace Contoso.Domain;
            public class Order
            {
                public string Id { get; private set; } = "";
                public string ReadOnlyName { get; } = "";
            }
            """, "Contoso.Domain.Order");

        var id = type.Properties.Single(p => p.Name == "Id");
        Assert.True(id.HasGetter);
        Assert.True(id.HasSetter);
        Assert.Equal(Accessibility.Private, id.SetterAccessibility);

        var readOnlyName = type.Properties.Single(p => p.Name == "ReadOnlyName");
        Assert.True(readOnlyName.HasGetter);
        Assert.False(readOnlyName.HasSetter);
        Assert.Null(readOnlyName.SetterAccessibility);
    }

    [Fact]
    public void ExtractTypes_MapsExplicitConstructorsAndExcludesImplicitDefault()
    {
        var withExplicit = ExtractSingle("""
            namespace Contoso.Domain;
            public class Order
            {
                private Order() { }
            }
            """, "Contoso.Domain.Order");
        var ctor = Assert.Single(withExplicit.Constructors);
        Assert.Equal(Accessibility.Private, ctor.Accessibility);

        var withImplicit = ExtractSingle("""
            namespace Contoso.Domain;
            public class LegacyThing { }
            """, "Contoso.Domain.LegacyThing");
        Assert.Empty(withImplicit.Constructors);
    }

    [Fact]
    public void ExtractTypes_PopulatesFileAndLineInformation()
    {
        var type = ExtractSingle("""
            namespace Contoso.Domain;

            public class Order { }
            """, "Contoso.Domain.Order");

        Assert.Equal(3, type.Line);
        Assert.True(type.Column > 0);
    }

    [Fact]
    public void ExtractTypes_FindsNestedTypes()
    {
        var compilation = CompilationFactory.Create("""
            namespace Contoso.Domain;
            public class Order
            {
                public class OrderLine { }
            }
            """);
        var types = RoslynTypeExtractor.ExtractTypes(compilation, "Contoso.Domain");

        Assert.Contains(types, t => t.FullName == "Contoso.Domain.Order.OrderLine");
    }
}
