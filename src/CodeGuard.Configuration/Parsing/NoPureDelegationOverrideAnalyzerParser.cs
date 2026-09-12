using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Analyzers;
using CodeGuard.RuleModel.Analyzers;

namespace CodeGuard.Configuration.Parsing;

public sealed class NoPureDelegationOverrideAnalyzerParser : IAnalyzerParser
{
    public string Kind => "no-pure-delegation-override";

    public CapabilityDescriptor Descriptor => new(
        "no-pure-delegation-override",
        "Flags overrides whose body only delegates to the base implementation.",
        [
            ParameterDescriptor.OptionalGlob("base_type_pattern", "Base type whose overrides are inspected.")
        ]);

    public ICustomAnalyzer Parse(JsonObject node) => new NoPureDelegationOverrideAnalyzer(
        node.GetOptionalString("base_type_pattern") ?? "*");
}
