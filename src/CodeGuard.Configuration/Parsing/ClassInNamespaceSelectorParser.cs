using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class ClassInNamespaceSelectorParser : ISelectorParser
{
    public string Kind => "class";

    public CapabilityDescriptor Descriptor => new(
        "class",
        "Classes in a matching namespace.",
        [
            ParameterDescriptor.RequiredGlob("namespace", "Namespace the class is declared in.")
        ])
    { Produces = CandidateKind.Type };

    public ITargetSelector Parse(JsonObject node) =>
        new ClassInNamespaceSelector(node.GetRequiredString("namespace"));
}
