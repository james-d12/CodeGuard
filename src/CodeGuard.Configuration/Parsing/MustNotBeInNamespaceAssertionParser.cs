using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustNotBeInNamespaceAssertionParser : IAssertionParser
{
    public string Kind => "must_not_be_in_namespace";

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotBeInNamespaceAssertion(parameters.GetRequiredString("pattern"));
}
