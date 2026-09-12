using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class InheritsFromSelectorParser : ISelectorParser
{
    public string Kind => "inherits_from";

    public CapabilityDescriptor Descriptor => new(
        "inherits_from",
        "Types deriving from a matching base type.",
        [
            ParameterDescriptor.RequiredGlob("type", "Base type name. Roslyn renders closed generics, so use Entity<*> not Entity<TId>.")
        ])
    { Produces = CandidateKind.Type };

    public ITargetSelector Parse(JsonObject node) =>
        new InheritsFromSelector(node.GetRequiredString("type"));
}
