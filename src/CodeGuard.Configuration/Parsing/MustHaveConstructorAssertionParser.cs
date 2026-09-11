using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustHaveConstructorAssertionParser : IAssertionParser
{
    public string Kind => "must_have_constructor";

    public CapabilityDescriptor Descriptor => new(
        "must_have_constructor",
        "Type must declare a constructor with one of the given accessibilities.",
        [
            new ParameterDescriptor("accessibility", ParameterType.StringList, true, "Accepted constructor accessibilities. Must be non-empty.", AllowedValues: ["public", "private", "protected", "internal", "protected_internal", "private_protected"])
        ])
    { AppliesTo = [CandidateKind.Type] };

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
