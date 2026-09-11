using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Analyzers;
using CodeGuard.RuleModel.Analyzers;

namespace CodeGuard.Configuration.Parsing;

public sealed class RoslynDiagnosticPassthroughAnalyzerParser : IAnalyzerParser
{
    public string Kind => "roslyn-diagnostic-passthrough";

    public CapabilityDescriptor Descriptor => new(
        "roslyn-diagnostic-passthrough",
        "Surfaces raw Roslyn compiler diagnostics as rule violations. Requires a real compilation.",
        [
            new ParameterDescriptor("diagnostic_ids", ParameterType.StringList, true, "Diagnostic IDs to surface, e.g. CS1591.")
        ]);

    public ICustomAnalyzer Parse(JsonObject node) => new RoslynDiagnosticPassthroughAnalyzer(
        node.GetStringArray("diagnostic_ids"));
}
