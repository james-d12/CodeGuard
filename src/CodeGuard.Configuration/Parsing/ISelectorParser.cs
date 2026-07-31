using System.Text.Json.Nodes;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public interface ISelectorParser
{
    string Kind { get; }

    ITargetSelector Parse(JsonObject node);
}
