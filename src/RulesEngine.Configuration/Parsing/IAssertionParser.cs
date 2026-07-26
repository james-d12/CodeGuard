using System.Text.Json.Nodes;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public interface IAssertionParser
{
    string Kind { get; }

    IAssertion Parse(JsonObject parameters);
}
