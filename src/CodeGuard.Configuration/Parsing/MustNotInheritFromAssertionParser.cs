using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustNotInheritFromAssertionParser : IAssertionParser
{
    public string Kind => "must_not_inherit_from";

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotInheritFromAssertion(parameters.GetRequiredString("type"));
}
