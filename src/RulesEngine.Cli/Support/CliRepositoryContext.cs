using RulesEngine.Configuration.Discovery;
using RulesEngine.Configuration.Loading;
using RulesEngine.RuleModel.Rules;

namespace RulesEngine.Cli.Support;

/// <summary>
/// Resolves the repo root/config/rule-directory layout shared by every CLI command, so
/// validate/list-rules/explain-rule/list-standards don't each duplicate the resolution logic.
/// </summary>
public sealed class CliRepositoryContext
{
    public required string RepoRoot { get; init; }
    public required RepositoryLayout Layout { get; init; }

    public static CliRepositoryContext Resolve(string? path, string? configPath)
    {
        var repoRoot = Path.GetFullPath(path ?? Directory.GetCurrentDirectory());
        var config = RulesEngineConfigLoader.LoadOrDefault(repoRoot, configPath);
        var layout = new RepositoryDiscovery().Resolve(repoRoot, config);

        return new CliRepositoryContext { RepoRoot = repoRoot, Layout = layout };
    }

    public IReadOnlyList<RuleDefinition> LoadRules() =>
        RuleFileLoader.CreateDefault().LoadFromDirectories(Layout.RulesPaths);

    public IReadOnlyList<(RuleDefinition Rule, string SourceFile)> LoadRulesWithSource() =>
        RuleFileLoader.CreateDefault().LoadFromDirectoriesWithSource(Layout.RulesPaths);
}
