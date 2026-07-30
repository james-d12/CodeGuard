using RulesEngine.Configuration.Discovery;
using RulesEngine.Configuration.GlobalConfig;
using RulesEngine.Configuration.Loading;
using RulesEngine.Configuration.Validation;
using RulesEngine.RuleModel.Rules;

namespace RulesEngine.Cli.Support;

/// <summary>
/// Resolves the repo root/config/rule-directory layout shared by every CLI command, so
/// validate/list-rules/explain-rule don't each duplicate the resolution logic.
///
/// Rules-path resolution precedence (see docs/SETUP_COMMAND_PLAN.md):
///   1. <paramref name="rulesSource"/> ("--rules-source"), if passed - highest precedence, an
///      explicit one-off override.
///   2. `--config` explicit path (unchanged existing behavior).
///   3. `&lt;repoRoot&gt;/.rulesengine/config.yml`, if present (unchanged existing behavior).
///   4. A prior `rules-engine setup` run's global settings, if the above yield no rules paths.
///   5. The hardcoded `["rules"]` fallback already baked into RulesEngineConfigLoader.
/// </summary>
public sealed class CliRepositoryContext
{
    public required string RepoRoot { get; init; }
    public required RepositoryLayout Layout { get; init; }

    /// <param name="globalSettingsRoot">
    /// Overrides where tier 4 looks for a prior `setup` run's <c>settings.yml</c> and git cache
    /// (default: the real OS app-data root, <see cref="GlobalSettingsPaths.ResolveRoot"/>). Exists
    /// so tests can point this at a temp directory instead of touching the developer's real
    /// machine-wide RuleEngine config - every CLI command call site leaves this at its default.
    /// </param>
    public static CliRepositoryContext Resolve(
        string? path, string? configPath, string? rulesSource = null, string? branch = null, string? globalSettingsRoot = null)
    {
        var repoRoot = Path.GetFullPath(path ?? Directory.GetCurrentDirectory());
        var config = RulesEngineConfigLoader.LoadOrDefault(repoRoot, configPath);

        if (rulesSource is not null)
        {
            var overridePath = RuleSourceResolver.ResolveToLocalPath(rulesSource, branch, cacheRootOverride: globalSettingsRoot);
            config = WithRules(config, [overridePath]);
        }

        var layout = new RepositoryDiscovery().Resolve(repoRoot, config);

        if (rulesSource is null && layout.RulesPaths.Count == 0)
        {
            var settingsRoot = globalSettingsRoot ?? GlobalSettingsPaths.ResolveRoot();
            var globalSettings = GlobalSettingsStore.Load(GlobalSettingsPaths.SettingsFilePath(settingsRoot));
            if (globalSettings is not null)
            {
                var globalPath = RuleSourceResolver.ResolveToLocalPath(
                    globalSettings.Location, globalSettings.Branch, globalSettings.Kind, cacheRootOverride: globalSettingsRoot);
                config = WithRules(config, [globalPath]);
                layout = new RepositoryDiscovery().Resolve(repoRoot, config);
            }
        }

        return new CliRepositoryContext { RepoRoot = repoRoot, Layout = layout };
    }

    public IReadOnlyList<RuleDefinition> LoadRules() =>
        RuleFileLoader.CreateDefault().LoadFromDirectories(Layout.RulesPaths);

    public IReadOnlyList<(RuleDefinition Rule, string SourceFile)> LoadRulesWithSource() =>
        RuleFileLoader.CreateDefault().LoadFromDirectoriesWithSource(Layout.RulesPaths);

    /// <summary>
    /// Non-throwing counterpart to <see cref="LoadRules"/>/<see cref="LoadRulesWithSource"/> -
    /// validates every configured rule file and reports every problem found instead of throwing on
    /// the first one. Used by `check-rules` and `validate`'s pre-flight rule-set gate
    /// (`docs/done/RULE_VALIDATION_PLAN.md`).
    /// </summary>
    public RuleSetValidationReport ValidateRules() =>
        RuleFileLoader.CreateDefault().ValidateDirectories(Layout.RulesPaths);

    private static RulesEngineConfig WithRules(RulesEngineConfig source, IReadOnlyList<string> rules) => new()
    {
        Repository = new RepositoryConfig
        {
            Standards = [.. source.Repository.Standards],
            Rules = [.. rules],
            Skills = [.. source.Repository.Skills],
            Agents = [.. source.Repository.Agents],
            Source = [.. source.Repository.Source],
            Tests = [.. source.Repository.Tests]
        }
    };
}
