using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustMatchNameAssertionParser : IAssertionParser
{
    public string Kind => "must_match_name";

    public CapabilityDescriptor Descriptor => new(
        "must_match_name",
        "Candidate's name must match a regex. Files match on relative path; constructors have no name and always fail.",
        [
            new ParameterDescriptor("regex", ParameterType.Regex, true, "Regex matched against the candidate's name.")
        ])
    { AppliesTo = [CandidateKind.Type, CandidateKind.Project, CandidateKind.Method, CandidateKind.Property, CandidateKind.Field, CandidateKind.File] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustMatchNameAssertion(parameters.GetRequiredString("regex"));
}
