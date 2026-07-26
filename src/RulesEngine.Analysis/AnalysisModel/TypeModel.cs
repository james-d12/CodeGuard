namespace RulesEngine.Analysis.AnalysisModel;

public sealed record TypeModel(
    string Name,
    string FullName,
    string Namespace,
    TypeKind Kind,
    string? BaseType,
    IReadOnlyList<string> Interfaces,
    Accessibility Accessibility,
    TypeModifiers Modifiers,
    IReadOnlyList<AttributeModel> Attributes,
    IReadOnlyList<MethodModel> Methods,
    IReadOnlyList<PropertyModel> Properties,
    IReadOnlyList<ConstructorModel> Constructors,
    IReadOnlyList<FieldModel> Fields,
    string ProjectName,
    string FilePath,
    int Line,
    int Column);

public enum TypeKind
{
    Class,
    Record,
    Struct,
    Interface,
    Enum,
    Delegate
}
