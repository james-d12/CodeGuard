using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustOnlyDependOnAssertionParser : IAssertionParser
{
    public string Kind => "must_only_depend_on";

    public IAssertion Parse(JsonObject parameters)
    {
        var types = parameters.GetStringArray("types");
        if (types.Count == 0)
        {
            throw new RuleParsingException("'must_only_depend_on' requires a non-empty 'types' array.");
        }

        return new MustOnlyDependOnAssertion(types);
    }
}
