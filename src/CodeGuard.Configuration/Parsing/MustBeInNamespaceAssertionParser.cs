using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustBeInNamespaceAssertionParser : IAssertionParser
{
    public string Kind => "must_be_in_namespace";

    public IAssertion Parse(JsonObject parameters) =>
        new MustBeInNamespaceAssertion(parameters.GetRequiredString("pattern"));
}
