using System.Text.Json.Nodes;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public interface IAssertionParser
{
    string Kind { get; }

    IAssertion Parse(JsonObject parameters);
}
