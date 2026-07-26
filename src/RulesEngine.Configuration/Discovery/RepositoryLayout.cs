namespace RulesEngine.Configuration.Discovery;

public sealed record RepositoryLayout(
    IReadOnlyList<string> StandardsPaths,
    IReadOnlyList<string> RulesPaths,
    IReadOnlyList<string> SkillsPaths,
    IReadOnlyList<string> AgentsPaths,
    IReadOnlyList<string> SourcePaths,
    IReadOnlyList<string> TestsPaths);
