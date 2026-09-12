using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class DiagnosticSelectorParser : ISelectorParser
{
    public string Kind => "diagnostic";

    public CapabilityDescriptor Descriptor => new(
        "diagnostic",
        "Raw Roslyn compiler diagnostics by ID. Requires a real compilation, so unavailable in rule tests unless supplied directly.",
        [
            ParameterDescriptor.OptionalGlob("id", "Diagnostic ID, e.g. CS1591."),
            ParameterDescriptor.OptionalGlob("project", "Project the site belongs to.")
        ])
    { Produces = CandidateKind.Diagnostic };

    public ITargetSelector Parse(JsonObject node) => new DiagnosticSelector(
        node.GetOptionalString("id") ?? "*",
        node.GetOptionalString("project") ?? "*");
}
