using System.Text.Json.Nodes;
using RulesEngine.RuleModel.Conditions;

namespace RulesEngine.Configuration.Parsing;

public sealed class ConditionParserRegistry(AssertionParserRegistry assertionParsers)
{
    public IConditionNode Parse(JsonObject node)
    {
        if (node.Count != 1)
        {
            throw new RuleParsingException(
                "Each 'when' node must be a single-key mapping, e.g. 'and: [...]', 'not: {...}', or an assertion kind.");
        }

        var (key, valueNode) = node.Single();
        return key switch
        {
            "and" => new AndCondition(ParseChildren(key, valueNode)),
            "or" => new OrCondition(ParseChildren(key, valueNode)),
            "not" => new NotCondition(Parse(valueNode?.AsObject()
                ?? throw new RuleParsingException("'not' requires a single nested condition node."))),
            _ => new AssertionCondition(assertionParsers.Parse(node))
        };
    }

    private List<IConditionNode> ParseChildren(string key, JsonNode? valueNode)
    {
        var array = valueNode?.AsArray()
            ?? throw new RuleParsingException($"'{key}' requires an array of condition nodes.");
        return array.Select(child => Parse(child!.AsObject())).ToList();
    }
}
