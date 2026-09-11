using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustMatchArgumentAssertionParser : IAssertionParser
{
    public string Kind => "must_match_argument";

    public CapabilityDescriptor Descriptor => new(
        "must_match_argument",
        "Call site's argument at an index must match a regex.",
        [
            new ParameterDescriptor("index", ParameterType.Int, true, "Zero-based argument index."),
            new ParameterDescriptor("pattern", ParameterType.Regex, true, "Regex matched against the argument.")
        ])
    { AppliesTo = [CandidateKind.CallSite] };

    public IAssertion Parse(JsonObject parameters) => new MustMatchArgumentAssertion(
        parameters.GetOptionalInt("index") ?? throw new RuleParsingException("'must_match_argument' requires an 'index'."),
        parameters.GetRequiredString("pattern"));
}
