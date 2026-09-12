using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustMatchContentAssertionParser : IAssertionParser
{
    public string Kind => "must_match_content";

    public CapabilityDescriptor Descriptor => new(
        "must_match_content",
        "File content must match a regex.",
        [
            new ParameterDescriptor("pattern", ParameterType.Regex, true, "Regex matched against the file's content.")
        ])
    { AppliesTo = [CandidateKind.File] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustMatchContentAssertion(parameters.GetRequiredString("pattern"));
}
