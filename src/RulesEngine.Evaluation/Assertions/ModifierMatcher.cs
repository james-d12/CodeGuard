using RulesEngine.Analysis.AnalysisModel;

namespace RulesEngine.Evaluation.Assertions;

internal static class ModifierMatcher
{
    public static bool? Matches(object candidate, string modifierName) => candidate switch
    {
        TypeModel type => MatchesType(type, modifierName),
        MethodModel method => MatchesMethod(method, modifierName),
        FieldModel field => MatchesField(field, modifierName),
        PropertyModel property => MatchesProperty(property, modifierName),
        _ => null
    };

    private static bool? MatchesType(TypeModel type, string modifierName) => modifierName switch
    {
        "record" => type.Kind == TypeKind.Record,
        "sealed" => type.Modifiers.HasFlag(TypeModifiers.Sealed),
        "abstract" => type.Modifiers.HasFlag(TypeModifiers.Abstract),
        "static" => type.Modifiers.HasFlag(TypeModifiers.Static),
        "partial" => type.Modifiers.HasFlag(TypeModifiers.Partial),
        _ => null
    };

    private static bool? MatchesMethod(MethodModel method, string modifierName) => modifierName switch
    {
        "static" => method.Modifiers.HasFlag(MethodModifiers.Static),
        "abstract" => method.Modifiers.HasFlag(MethodModifiers.Abstract),
        "virtual" => method.Modifiers.HasFlag(MethodModifiers.Virtual),
        "override" => method.Modifiers.HasFlag(MethodModifiers.Override),
        "async" => method.Modifiers.HasFlag(MethodModifiers.Async),
        _ => null
    };

    private static bool? MatchesField(FieldModel field, string modifierName) => modifierName switch
    {
        "static" => field.Modifiers.HasFlag(FieldModifiers.Static),
        "const" => field.Modifiers.HasFlag(FieldModifiers.Const),
        "readonly" => field.Modifiers.HasFlag(FieldModifiers.Readonly),
        _ => null
    };

    private static bool? MatchesProperty(PropertyModel property, string modifierName) => modifierName switch
    {
        "static" => property.IsStatic,
        "required" => property.IsRequired,
        "init" => property.IsInit,
        _ => null
    };
}
