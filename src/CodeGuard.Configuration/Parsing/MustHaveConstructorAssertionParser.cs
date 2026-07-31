using System.Text.Json.Nodes;
using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustHaveConstructorAssertionParser : IAssertionParser
{
    public string Kind => "must_have_constructor";

    public IAssertion Parse(JsonObject parameters)
    {
        var accessibilities = parameters.GetStringArray("accessibility");
        if (accessibilities.Count == 0)
        {
            throw new RuleParsingException("'must_have_constructor' requires at least one 'accessibility' value.");
        }

        return new MustHaveConstructorAssertion(
            accessibilities.Select(EnumParsing.ParseSnakeCase<Accessibility>).ToList());
    }
}
