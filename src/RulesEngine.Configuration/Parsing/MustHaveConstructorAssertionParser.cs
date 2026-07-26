using System.Text.Json.Nodes;
using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

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
