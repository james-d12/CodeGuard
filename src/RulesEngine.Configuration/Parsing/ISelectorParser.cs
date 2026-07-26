using System.Text.Json.Nodes;
using RulesEngine.RuleModel.Selectors;

namespace RulesEngine.Configuration.Parsing;

public interface ISelectorParser
{
    string Kind { get; }

    ITargetSelector Parse(JsonObject node);
}
