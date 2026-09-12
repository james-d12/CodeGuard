using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustHaveDirectoryAssertionParser : IAssertionParser
{
    public string Kind => "must_have_directory";

    public CapabilityDescriptor Descriptor => new(
        "must_have_directory",
        "A matching directory must exist in the repository.",
        [
            ParameterDescriptor.RequiredGlob("path", "Repository-relative directory path.")
        ])
    { AppliesTo = [CandidateKind.Repository] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustHaveDirectoryAssertion(parameters.GetRequiredString("path"));
}
