using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustNotDependOnAssertionParser : IAssertionParser
{
    public string Kind => "must_not_depend_on";

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotDependOnAssertion(parameters.GetRequiredString("type"));
}
