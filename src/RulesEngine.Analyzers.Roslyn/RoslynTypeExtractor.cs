using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RulesEngine.Analysis.AnalysisModel;
using Accessibility = RulesEngine.Analysis.AnalysisModel.Accessibility;
using TypeKind = RulesEngine.Analysis.AnalysisModel.TypeKind;

namespace RulesEngine.Analyzers.Roslyn;

public static class RoslynTypeExtractor
{
    public static IReadOnlyList<TypeModel> ExtractTypes(CSharpCompilation compilation, string projectName)
    {
        var types = new List<TypeModel>();
        CollectTypes(compilation.Assembly.GlobalNamespace, projectName, types);
        return types;
    }

    private static void CollectTypes(INamespaceOrTypeSymbol container, string projectName, List<TypeModel> types)
    {
        foreach (var member in container.GetMembers())
        {
            switch (member)
            {
                case INamespaceSymbol ns:
                    CollectTypes(ns, projectName, types);
                    break;
                case INamedTypeSymbol type:
                    types.Add(ToTypeModel(type, projectName));
                    CollectTypes(type, projectName, types);
                    break;
            }
        }
    }

    private static TypeModel ToTypeModel(INamedTypeSymbol symbol, string projectName)
    {
        var location = symbol.Locations.FirstOrDefault(l => l.IsInSource);
        var lineSpan = location?.GetLineSpan();

        var declaringType = symbol.ToDisplayString();

        return new TypeModel(
            Name: symbol.Name,
            FullName: symbol.ToDisplayString(),
            Namespace: symbol.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : symbol.ContainingNamespace.ToDisplayString(),
            Kind: MapTypeKind(symbol),
            BaseType: MapBaseType(symbol),
            Interfaces: symbol.AllInterfaces.Select(i => i.ToDisplayString()).ToList(),
            Accessibility: MapAccessibility(symbol.DeclaredAccessibility),
            Modifiers: MapTypeModifiers(symbol),
            Attributes: symbol.GetAttributes().Select(ToAttributeModel).ToList(),
            Methods: symbol.GetMembers().OfType<IMethodSymbol>()
                .Where(m => m.MethodKind == MethodKind.Ordinary)
                .Select(m => ToMethodModel(m, declaringType, projectName)).ToList(),
            Properties: symbol.GetMembers().OfType<IPropertySymbol>()
                .Select(p => ToPropertyModel(p, declaringType, projectName)).ToList(),
            Constructors: symbol.GetMembers().OfType<IMethodSymbol>()
                .Where(m => m.MethodKind == MethodKind.Constructor && !m.IsImplicitlyDeclared)
                .Select(c => ToConstructorModel(c, declaringType, projectName)).ToList(),
            Fields: symbol.TypeKind == Microsoft.CodeAnalysis.TypeKind.Enum
                ? []
                : symbol.GetMembers().OfType<IFieldSymbol>()
                    .Where(f => !f.IsImplicitlyDeclared)
                    .Select(f => ToFieldModel(f, declaringType, projectName)).ToList(),
            ProjectName: projectName,
            FilePath: location?.SourceTree?.FilePath ?? string.Empty,
            Line: (lineSpan?.StartLinePosition.Line ?? -1) + 1,
            Column: (lineSpan?.StartLinePosition.Character ?? -1) + 1);
    }

    private static TypeKind MapTypeKind(INamedTypeSymbol symbol) => symbol.TypeKind switch
    {
        Microsoft.CodeAnalysis.TypeKind.Interface => TypeKind.Interface,
        Microsoft.CodeAnalysis.TypeKind.Struct => TypeKind.Struct,
        Microsoft.CodeAnalysis.TypeKind.Enum => TypeKind.Enum,
        Microsoft.CodeAnalysis.TypeKind.Delegate => TypeKind.Delegate,
        Microsoft.CodeAnalysis.TypeKind.Class when symbol.IsRecord => TypeKind.Record,
        _ => TypeKind.Class
    };

    private static string? MapBaseType(INamedTypeSymbol symbol)
    {
        var baseType = symbol.BaseType;
        if (baseType is null || baseType.SpecialType is SpecialType.System_Object or SpecialType.System_ValueType)
        {
            return null;
        }

        return baseType.ToDisplayString();
    }

    private static Accessibility MapAccessibility(Microsoft.CodeAnalysis.Accessibility accessibility) => accessibility switch
    {
        Microsoft.CodeAnalysis.Accessibility.Private => Accessibility.Private,
        Microsoft.CodeAnalysis.Accessibility.Protected => Accessibility.Protected,
        Microsoft.CodeAnalysis.Accessibility.Internal => Accessibility.Internal,
        Microsoft.CodeAnalysis.Accessibility.ProtectedOrInternal => Accessibility.ProtectedInternal,
        Microsoft.CodeAnalysis.Accessibility.ProtectedAndInternal => Accessibility.PrivateProtected,
        _ => Accessibility.Public
    };

    private static TypeModifiers MapTypeModifiers(INamedTypeSymbol symbol)
    {
        var modifiers = TypeModifiers.None;
        if (symbol.IsStatic) modifiers |= TypeModifiers.Static;
        if (symbol.IsAbstract && symbol.TypeKind == Microsoft.CodeAnalysis.TypeKind.Class) modifiers |= TypeModifiers.Abstract;
        if (symbol.IsSealed) modifiers |= TypeModifiers.Sealed;
        if (IsPartial(symbol)) modifiers |= TypeModifiers.Partial;
        return modifiers;
    }

    private static bool IsPartial(INamedTypeSymbol symbol) =>
        symbol.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .Any(d => d.Modifiers.Any(SyntaxKind.PartialKeyword));

    private static AttributeModel ToAttributeModel(AttributeData attribute) => new(
        TypeName: attribute.AttributeClass?.ToDisplayString() ?? "<unknown>",
        ConstructorArgumentLiterals: attribute.ConstructorArguments.Select(a => a.Value?.ToString() ?? "null").ToList(),
        NamedArguments: attribute.NamedArguments.ToDictionary(a => a.Key, a => a.Value.Value?.ToString() ?? "null"));

    private static (string FilePath, int Line, int Column) GetLocation(ISymbol symbol)
    {
        var location = symbol.Locations.FirstOrDefault(l => l.IsInSource);
        var lineSpan = location?.GetLineSpan();
        return (
            location?.SourceTree?.FilePath ?? string.Empty,
            (lineSpan?.StartLinePosition.Line ?? -1) + 1,
            (lineSpan?.StartLinePosition.Character ?? -1) + 1);
    }

    private static MethodModel ToMethodModel(IMethodSymbol method, string declaringType, string projectName)
    {
        var (filePath, line, column) = GetLocation(method);
        return new MethodModel(
            Name: method.Name,
            ReturnType: method.ReturnType.ToDisplayString(),
            Parameters: method.Parameters.Select(ToParameterModel).ToList(),
            Accessibility: MapAccessibility(method.DeclaredAccessibility),
            Modifiers: MapMethodModifiers(method),
            Attributes: method.GetAttributes().Select(ToAttributeModel).ToList(),
            DeclaringType: declaringType,
            ProjectName: projectName,
            FilePath: filePath,
            Line: line,
            Column: column);
    }

    private static MethodModifiers MapMethodModifiers(IMethodSymbol method)
    {
        var modifiers = MethodModifiers.None;
        if (method.IsStatic) modifiers |= MethodModifiers.Static;
        if (method.IsAbstract) modifiers |= MethodModifiers.Abstract;
        if (method.IsVirtual) modifiers |= MethodModifiers.Virtual;
        if (method.IsOverride) modifiers |= MethodModifiers.Override;
        if (method.IsAsync) modifiers |= MethodModifiers.Async;
        return modifiers;
    }

    private static PropertyModel ToPropertyModel(IPropertySymbol property, string declaringType, string projectName)
    {
        var (filePath, line, column) = GetLocation(property);
        return new PropertyModel(
            Name: property.Name,
            Type: property.Type.ToDisplayString(),
            Accessibility: MapAccessibility(property.DeclaredAccessibility),
            HasGetter: property.GetMethod is not null,
            HasSetter: property.SetMethod is not null,
            SetterAccessibility: property.SetMethod is null ? null : MapAccessibility(property.SetMethod.DeclaredAccessibility),
            IsRequired: property.IsRequired,
            IsInit: property.SetMethod?.IsInitOnly ?? false,
            IsStatic: property.IsStatic,
            Attributes: property.GetAttributes().Select(ToAttributeModel).ToList(),
            DeclaringType: declaringType,
            ProjectName: projectName,
            FilePath: filePath,
            Line: line,
            Column: column);
    }

    private static ConstructorModel ToConstructorModel(IMethodSymbol constructor, string declaringType, string projectName)
    {
        var (filePath, line, column) = GetLocation(constructor);
        return new ConstructorModel(
            Accessibility: MapAccessibility(constructor.DeclaredAccessibility),
            Parameters: constructor.Parameters.Select(ToParameterModel).ToList(),
            Attributes: constructor.GetAttributes().Select(ToAttributeModel).ToList(),
            DeclaringType: declaringType,
            ProjectName: projectName,
            FilePath: filePath,
            Line: line,
            Column: column);
    }

    private static FieldModel ToFieldModel(IFieldSymbol field, string declaringType, string projectName)
    {
        var (filePath, line, column) = GetLocation(field);
        return new FieldModel(
            Name: field.Name,
            Type: field.Type.ToDisplayString(),
            Accessibility: MapAccessibility(field.DeclaredAccessibility),
            Modifiers: MapFieldModifiers(field),
            Attributes: field.GetAttributes().Select(ToAttributeModel).ToList(),
            DeclaringType: declaringType,
            ProjectName: projectName,
            FilePath: filePath,
            Line: line,
            Column: column,
            ConstantValue: field.HasConstantValue ? field.ConstantValue?.ToString() : null);
    }

    private static FieldModifiers MapFieldModifiers(IFieldSymbol field)
    {
        var modifiers = FieldModifiers.None;
        if (field.IsConst) modifiers |= FieldModifiers.Const;
        if (field.IsStatic) modifiers |= FieldModifiers.Static;
        if (field.IsReadOnly) modifiers |= FieldModifiers.Readonly;
        return modifiers;
    }

    private static ParameterModel ToParameterModel(IParameterSymbol parameter) => new(
        Name: parameter.Name,
        Type: parameter.Type.ToDisplayString(),
        Attributes: parameter.GetAttributes().Select(ToAttributeModel).ToList(),
        HasDefaultValue: parameter.HasExplicitDefaultValue);
}
