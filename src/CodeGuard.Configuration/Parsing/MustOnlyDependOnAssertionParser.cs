using CodeGuard.Configuration.Capabilities;
using CodeGuard.Configuration.Validation;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;
using System.Text.Json.Nodes;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustOnlyDependOnAssertionParser : IAssertionParser
{
    public string Kind => "must_only_depend_on";

    public CapabilityDescriptor Descriptor => new(
        "must_only_depend_on",
        "Every type the project references must match one of these patterns. No implicit framework exemption - primitives render as C# keywords (string, int), so name them explicitly.",
        [
            new ParameterDescriptor("types", ParameterType.StringList, true, "Allowed type patterns. Must be non-empty.")
        ])
    { AppliesTo = [CandidateKind.Project] };

    public IAssertion Parse(JsonObject parameters)
    {
        var types = parameters.GetStringArray("types");
        if (types.Count == 0)
        {
            throw new RuleParsingException(
                "'must_only_depend_on' requires a non-empty 'types' array.",
                RuleErrorCodes.InvalidParameter, "/types");
        }

        return new MustOnlyDependOnAssertion(types);
    }
}
