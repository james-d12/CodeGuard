using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustNotHaveDirectoryAssertionParser : IAssertionParser
{
    public string Kind => "must_not_have_directory";

    public CapabilityDescriptor Descriptor => new(
        "must_not_have_directory",
        "No matching directory may exist in the repository.",
        [
            ParameterDescriptor.RequiredGlob("path", "Repository-relative directory path that must not exist.")
        ])
    { AppliesTo = [CandidateKind.Repository] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotHaveDirectoryAssertion(parameters.GetRequiredString("path"));
}
