using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustBeInProjectAssertionParser : IAssertionParser
{
    public string Kind => "must_be_in_project";

    public IAssertion Parse(JsonObject parameters) =>
        new MustBeInProjectAssertion(parameters.GetRequiredString("pattern"));
}
