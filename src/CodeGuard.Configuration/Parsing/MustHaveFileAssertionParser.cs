using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustHaveFileAssertionParser : IAssertionParser
{
    public string Kind => "must_have_file";

    public CapabilityDescriptor Descriptor => new(
        "must_have_file",
        "A matching file must exist in the repository.",
        [
            ParameterDescriptor.RequiredGlob("path", "Repository-relative file path.")
        ])
    { AppliesTo = [CandidateKind.Repository] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustHaveFileAssertion(parameters.GetRequiredString("path"));
}
