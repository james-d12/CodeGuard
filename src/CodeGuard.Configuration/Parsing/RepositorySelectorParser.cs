using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class RepositorySelectorParser : ISelectorParser
{
    public string Kind => "repository";

    public CapabilityDescriptor Descriptor => new(
        "repository",
        "The repository as a whole - a single candidate. Pair with must_exist/must_not_exist/must_have_count and a nested selector to express a repo-wide rule.",
        [])
    { Produces = CandidateKind.Repository };

    public ITargetSelector Parse(JsonObject node) => new RepositorySelector();
}
