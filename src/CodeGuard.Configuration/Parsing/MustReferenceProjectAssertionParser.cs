using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustReferenceProjectAssertionParser : IAssertionParser
{
    public string Kind => "must_reference_project";

    public IAssertion Parse(JsonObject parameters) =>
        new MustReferenceProjectAssertion(parameters.GetRequiredString("name"));
}
