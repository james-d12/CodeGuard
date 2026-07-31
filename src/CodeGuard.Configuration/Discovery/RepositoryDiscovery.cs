namespace CodeGuard.Configuration.Discovery;

public interface IRepositoryDiscovery
{
    RepositoryLayout Resolve(string repoRoot, CodeGuardConfig config);
}

public sealed class RepositoryDiscovery : IRepositoryDiscovery
{
    public RepositoryLayout Resolve(string repoRoot, CodeGuardConfig config) => new(
        StandardsPaths: ResolveExisting(repoRoot, config.Repository.Standards),
        RulesPaths: ResolveExisting(repoRoot, config.Repository.Rules),
        SkillsPaths: ResolveExisting(repoRoot, config.Repository.Skills),
        AgentsPaths: ResolveExisting(repoRoot, config.Repository.Agents),
        SourcePaths: ResolveExisting(repoRoot, config.Repository.Source),
        TestsPaths: ResolveExisting(repoRoot, config.Repository.Tests));

    private static IReadOnlyList<string> ResolveExisting(string repoRoot, IReadOnlyList<string> relativePaths) =>
        relativePaths
            .Select(relativePath => Path.GetFullPath(Path.Combine(repoRoot, relativePath)))
            .Where(Directory.Exists)
            .ToList();
}
