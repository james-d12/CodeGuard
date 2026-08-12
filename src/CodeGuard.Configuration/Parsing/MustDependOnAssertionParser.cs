using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustDependOnAssertionParser : IAssertionParser
{
    public string Kind => "must_depend_on";

    public IAssertion Parse(JsonObject parameters) =>
        new MustDependOnAssertion(parameters.GetRequiredString("type"));
}
