using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class RecordSelectorParser : ISelectorParser
{
    public string Kind => "record";

    public CapabilityDescriptor Descriptor => new(
        "record",
        "Record types in a matching namespace.",
        [
            ParameterDescriptor.OptionalGlob("namespace", "Namespace the record is declared in.")
        ])
    { Produces = CandidateKind.Type };

    public ITargetSelector Parse(JsonObject node) =>
        new RecordSelector(node.GetOptionalString("namespace") ?? "*");
}
