using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustMatchFilenameAssertionParser : IAssertionParser
{
    public string Kind => "must_match_filename";

    public CapabilityDescriptor Descriptor => new(
        "must_match_filename",
        "Type must be declared in a file named after it.",
        [])
    { AppliesTo = [CandidateKind.Type] };

    public IAssertion Parse(JsonObject parameters) => new MustMatchFilenameAssertion();
}
