using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustInheritFromAssertionParser : IAssertionParser
{
    public string Kind => "must_inherit_from";

    public IAssertion Parse(JsonObject parameters) =>
        new MustInheritFromAssertion(parameters.GetRequiredString("type"));
}
