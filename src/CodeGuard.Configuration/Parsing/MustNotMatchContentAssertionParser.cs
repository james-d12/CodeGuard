using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustNotMatchContentAssertionParser : IAssertionParser
{
    public string Kind => "must_not_match_content";

    public CapabilityDescriptor Descriptor => new(
        "must_not_match_content",
        "File content must not match a regex.",
        [
            new ParameterDescriptor("pattern", ParameterType.Regex, true, "Regex that must not match the file's content.")
        ])
    { AppliesTo = [CandidateKind.File] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotMatchContentAssertion(parameters.GetRequiredString("pattern"));
}
