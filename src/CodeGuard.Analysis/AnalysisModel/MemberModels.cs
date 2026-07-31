namespace CodeGuard.Analysis.AnalysisModel;

public sealed record MethodModel(
    string Name,
    string ReturnType,
    IReadOnlyList<ParameterModel> Parameters,
    Accessibility Accessibility,
    MethodModifiers Modifiers,
    IReadOnlyList<AttributeModel> Attributes,
    string DeclaringType,
    string ProjectName,
    string FilePath,
    int Line,
    int Column);

public sealed record PropertyModel(
    string Name,
    string Type,
    Accessibility Accessibility,
    bool HasGetter,
    bool HasSetter,
    Accessibility? SetterAccessibility,
    bool IsRequired,
    bool IsInit,
    bool IsStatic,
    IReadOnlyList<AttributeModel> Attributes,
    string DeclaringType,
    string ProjectName,
    string FilePath,
    int Line,
    int Column);

public sealed record ConstructorModel(
    Accessibility Accessibility,
    IReadOnlyList<ParameterModel> Parameters,
    IReadOnlyList<AttributeModel> Attributes,
    string DeclaringType,
    string ProjectName,
    string FilePath,
    int Line,
    int Column);

public sealed record FieldModel(
    string Name,
    string Type,
    Accessibility Accessibility,
    FieldModifiers Modifiers,
    IReadOnlyList<AttributeModel> Attributes,
    string DeclaringType,
    string ProjectName,
    string FilePath,
    int Line,
    int Column,
    string? ConstantValue = null);

public sealed record ParameterModel(
    string Name,
    string Type,
    IReadOnlyList<AttributeModel> Attributes,
    bool HasDefaultValue);

public sealed record AttributeModel(
    string TypeName,
    IReadOnlyList<string> ConstructorArgumentLiterals,
    IReadOnlyDictionary<string, string> NamedArguments);
