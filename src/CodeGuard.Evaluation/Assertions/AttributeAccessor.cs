using CodeGuard.Analysis.AnalysisModel;

namespace CodeGuard.Evaluation.Assertions;

internal static class AttributeAccessor
{
    public static IReadOnlyList<AttributeModel>? GetAttributes(object candidate) => candidate switch
    {
        TypeModel type => type.Attributes,
        MethodModel method => method.Attributes,
        PropertyModel property => property.Attributes,
        ConstructorModel constructor => constructor.Attributes,
        FieldModel field => field.Attributes,
        ParameterModel parameter => parameter.Attributes,
        _ => null
    };

    public static bool HasArgument(AttributeModel attribute, string argument) =>
        attribute.ConstructorArgumentLiterals.Contains(argument) || attribute.NamedArguments.Values.Contains(argument);
}
