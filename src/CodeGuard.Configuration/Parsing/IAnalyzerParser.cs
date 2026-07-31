using System.Text.Json.Nodes;
using CodeGuard.RuleModel.Analyzers;

namespace CodeGuard.Configuration.Parsing;

public interface IAnalyzerParser
{
    string Kind { get; }

    ICustomAnalyzer Parse(JsonObject node);
}
