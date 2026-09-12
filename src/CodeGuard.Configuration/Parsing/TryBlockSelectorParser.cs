using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class TryBlockSelectorParser : ISelectorParser
{
    public string Kind => "try_block";

    public CapabilityDescriptor Descriptor => new(
        "try_block",
        "Try blocks by catch-clause count.",
        [
            ParameterDescriptor.OptionalInt("min_catch_clause_count", "Minimum number of catch clauses, inclusive."),
            ParameterDescriptor.OptionalInt("max_catch_clause_count", "Maximum number of catch clauses, inclusive."),
            ParameterDescriptor.OptionalGlob("containing_type", "Type the site appears in."),
            ParameterDescriptor.OptionalGlob("containing_method", "Method the site appears in."),
            ParameterDescriptor.OptionalGlob("project", "Project the site belongs to.")
        ])
    { Produces = CandidateKind.TryBlock };

    public ITargetSelector Parse(JsonObject node) => new TryBlockSelector(
        node.GetOptionalInt("min_catch_clause_count"),
        node.GetOptionalInt("max_catch_clause_count"),
        node.GetOptionalString("containing_type") ?? "*",
        node.GetOptionalString("containing_method") ?? "*",
        node.GetOptionalString("project") ?? "*");
}
