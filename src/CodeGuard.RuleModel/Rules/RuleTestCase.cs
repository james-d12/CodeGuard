using System.Text.Json.Nodes;

namespace CodeGuard.RuleModel.Rules;

public enum TestExpectation
{
    Pass,
    Fail
}

public sealed record RuleTestCase(string Name, JsonObject Setup, TestExpectation Expect);
