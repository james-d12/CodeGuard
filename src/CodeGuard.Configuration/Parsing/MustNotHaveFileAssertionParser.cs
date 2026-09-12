using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustNotHaveFileAssertionParser : IAssertionParser
{
    public string Kind => "must_not_have_file";

    public CapabilityDescriptor Descriptor => new(
        "must_not_have_file",
        "No matching file may exist in the repository.",
        [
            ParameterDescriptor.RequiredGlob("path", "Repository-relative file path that must not exist.")
        ])
    { AppliesTo = [CandidateKind.Repository] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotHaveFileAssertion(parameters.GetRequiredString("path"));
}
