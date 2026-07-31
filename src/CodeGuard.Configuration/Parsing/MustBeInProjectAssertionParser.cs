using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustBeInProjectAssertionParser : IAssertionParser
{
    public string Kind => "must_be_in_project";

    public IAssertion Parse(JsonObject parameters) =>
        new MustBeInProjectAssertion(parameters.GetRequiredString("pattern"));
}
