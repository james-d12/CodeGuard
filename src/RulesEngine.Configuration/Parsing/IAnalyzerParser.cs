using System.Text.Json.Nodes;
using RulesEngine.RuleModel.Analyzers;

namespace RulesEngine.Configuration.Parsing;

public interface IAnalyzerParser
{
    string Kind { get; }

    ICustomAnalyzer Parse(JsonObject node);
}
