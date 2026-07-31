using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustNotReferenceProjectAssertionParser : IAssertionParser
{
    public string Kind => "must_not_reference_project";

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotReferenceProjectAssertion(parameters.GetRequiredString("name"));
}
