namespace RulesEngine.Analysis.AnalysisModel;

public sealed record MethodModel(
    string Name,
    string ReturnType,
    IReadOnlyList<ParameterModel> Parameters,
    Accessibility Accessibility,
    MethodModifiers Modifiers);

public sealed record PropertyModel(
    string Name,
    string Type,
    Accessibility Accessibility,
    bool HasGetter,
    bool HasSetter,
    Accessibility? SetterAccessibility);

public sealed record ConstructorModel(
    Accessibility Accessibility,
    IReadOnlyList<ParameterModel> Parameters);

public sealed record ParameterModel(string Name, string Type);

public sealed record AttributeModel(
    string TypeName,
    IReadOnlyList<string> ConstructorArgumentLiterals);
