using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class TryBlockSelectorParser : ISelectorParser
{
    public string Kind => "try_block";

    public ITargetSelector Parse(JsonObject node) => new TryBlockSelector(
        node.GetOptionalInt("min_catch_clause_count"),
        node.GetOptionalInt("max_catch_clause_count"),
        node.GetOptionalString("containing_type") ?? "*",
        node.GetOptionalString("containing_method") ?? "*",
        node.GetOptionalString("project") ?? "*");
}
