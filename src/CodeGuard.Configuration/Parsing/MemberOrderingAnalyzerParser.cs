using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Configuration.Validation;
using CodeGuard.Evaluation.Analyzers;
using CodeGuard.RuleModel.Analyzers;

namespace CodeGuard.Configuration.Parsing;

public sealed class MemberOrderingAnalyzerParser : IAnalyzerParser
{
    public string Kind => "member-ordering";

    public CapabilityDescriptor Descriptor => new(
        "member-ordering",
        "Flags types whose members are not declared in the configured order.",
        [
            new ParameterDescriptor("order", ParameterType.StringList, false, "Member kinds in required declaration order. A built-in default is used when omitted.")
        ]);

    public ICustomAnalyzer Parse(JsonObject node)
    {
        var order = node["order"]?.AsArray().Select(n => n!.GetValue<string>()).ToList();
        var duplicates = order?.GroupBy(name => name).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates is { Count: > 0 })
        {
            // MemberOrderingAnalyzer's constructor builds a name->rank dictionary from `order` - a
            // duplicate entry throws a raw, uncaught ArgumentException there (not a RuleParsingException),
            // which RuleFileLoader.TryLoadFromFile's catch clause doesn't handle, crashing the whole CLI
            // command instead of reporting a clean per-rule validation error. Reject it here instead.
            throw new RuleParsingException(
                $"'member-ordering' requires unique 'order' entries; duplicated: {string.Join(", ", duplicates)}.",
                RuleErrorCodes.InvalidParameter, "/order");
        }

        return new MemberOrderingAnalyzer(order);
    }
}
