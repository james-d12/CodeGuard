using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class ConstructorSelectorParser : ISelectorParser
{
    public string Kind => "constructor";

    public CapabilityDescriptor Descriptor => new(
        "constructor",
        "Constructors matching the given filters.",
        [
            ParameterDescriptor.OptionalGlob("declaring_type", "Type declaring the constructor."),
            new ParameterDescriptor("parameter_types", ParameterType.StringList, false, "Parameter type globs, in declaration order.")
        ])
    { Produces = CandidateKind.Constructor };

    public ITargetSelector Parse(JsonObject node) => new ConstructorSelector(
        node.GetOptionalString("declaring_type") ?? "*",
        node["parameter_types"] is JsonArray array ? array.Select(n => n!.GetValue<string>()).ToList() : null);
}
