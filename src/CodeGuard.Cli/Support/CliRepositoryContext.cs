using CodeGuard.Configuration.Discovery;
using CodeGuard.Configuration.GlobalConfig;
using CodeGuard.Configuration.Loading;
using CodeGuard.Configuration.Validation;
using CodeGuard.RuleModel.Rules;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CodeGuard.Cli.Support;

/// <summary>
/// Resolves the repo root/config/rule-directory layout shared by every CLI command, so
/// validate/rules list/rules explain don't each duplicate the resolution logic.
///
/// Rules-path resolution precedence (see docs/SETUP_COMMAND_PLAN.md):
///   1. <paramref name="rulesSource"/> ("--rules-source"), if passed - highest precedence, an
///      explicit one-off override.
///   2. `--config` explicit path (unchanged existing behavior).
///   3. `&lt;repoRoot&gt;/.codeguard/config.yml`, if present (unchanged existing behavior).
///   4. A prior `codeguard setup` run's global settings, if the above yield no rules paths.
///   5. The hardcoded `["rules"]` fallback already baked into CodeGuardConfigLoader.
///
/// <see cref="RulesProvenance"/>/<see cref="ConfigFilePath"/>/<see cref="GlobalSettings"/> record
/// which of the above actually won, for `codeguard info` to report back to the user.
/// </summary>
public sealed class CliRepositoryContext
{
    public required string RepoRoot { get; init; }
    public required RepositoryLayout Layout { get; init; }
    public required RulesSourceProvenance RulesProvenance { get; init; }
    public required string ConfigFilePath { get; init; }
    public GlobalSettings? GlobalSettings { get; init; }
    public ILoggerFactory? LoggerFactory { get; init; }

    /// <param name="globalSettingsRoot">
    /// Overrides where tier 4 looks for a prior `setup` run's <c>settings.yml</c> and git cache
    /// (default: the real OS app-data root, <see cref="GlobalSettingsPaths.ResolveRoot"/>). Exists
    /// so tests can point this at a temp directory instead of touching the developer's real
    /// machine-wide RuleEngine config - every CLI command call site leaves this at its default.
    /// </param>
    /// <param name="loggerFactory">
    /// Used both for this method's own resolution logging and, via the returned context, for every
    /// <see cref="RuleFileLoader"/> constructed by <see cref="LoadRules"/>/<see cref="LoadRulesWithSource"/>/
    /// <see cref="ValidateRules"/> - so it's stored as a factory rather than a single <c>ILogger&lt;T&gt;</c>.
    /// </param>
    public static CliRepositoryContext Resolve(
        string? path, string? configPath, string? rulesSource = null, string? branch = null,
        string? globalSettingsRoot = null, ILoggerFactory? loggerFactory = null)
    {
        var logger = loggerFactory?.CreateLogger<CliRepositoryContext>() ?? NullLogger<CliRepositoryContext>.Instance;

        var repoRoot = Path.GetFullPath(path ?? Directory.GetCurrentDirectory());
        logger.LogDebug("Resolving repository context for {RepoRoot}", repoRoot);
        var configFilePath = CodeGuardConfigLoader.ResolveConfigFilePath(repoRoot, configPath);
        var config = CodeGuardConfigLoader.LoadOrDefault(repoRoot, configPath);

        var provenance = rulesSource is not null
            ? RulesSourceProvenance.CliOverride
            : File.Exists(configFilePath)
                ? RulesSourceProvenance.RepositoryConfig
                : RulesSourceProvenance.Default;

        if (rulesSource is not null)
        {
            logger.LogDebug("Using ad-hoc --rules-source override: {RulesSource} (branch {Branch})", rulesSource, branch ?? "(default)");
            var overridePath = RuleSourceResolver.ResolveToLocalPath(
                rulesSource, branch, cacheRootOverride: globalSettingsRoot, logger: loggerFactory?.CreateLogger(typeof(RuleSourceResolver)));
            config = WithRules(config, [overridePath]);
        }

        var layout = new RepositoryDiscovery().Resolve(repoRoot, config);

        GlobalSettings? globalSettings = null;
        if (rulesSource is null && layout.RulesPaths.Count == 0)
        {
            var settingsRoot = globalSettingsRoot ?? GlobalSettingsPaths.ResolveRoot();
            globalSettings = GlobalSettingsStore.Load(GlobalSettingsPaths.SettingsFilePath(settingsRoot));
            if (globalSettings is not null)
            {
                logger.LogDebug("No rules paths resolved from repo config; using global settings source {Location}", globalSettings.Location);
                var globalPath = RuleSourceResolver.ResolveToLocalPath(
                    globalSettings.Location, globalSettings.Branch, globalSettings.Kind,
                    cacheRootOverride: globalSettingsRoot, logger: loggerFactory?.CreateLogger(typeof(RuleSourceResolver)));
                config = WithRules(config, [globalPath]);
                layout = new RepositoryDiscovery().Resolve(repoRoot, config);
                provenance = RulesSourceProvenance.GlobalSettings;
            }
        }

        logger.LogInformation(
            "Resolved rules source: provenance={Provenance}, repoRoot={RepoRoot}, rulesPaths={RulesPathCount}",
            provenance, repoRoot, layout.RulesPaths.Count);

        return new CliRepositoryContext
        {
            RepoRoot = repoRoot,
            Layout = layout,
            RulesProvenance = provenance,
            ConfigFilePath = configFilePath,
            GlobalSettings = globalSettings,
            LoggerFactory = loggerFactory
        };
    }

    public IReadOnlyList<RuleDefinition> LoadRules() =>
        RuleFileLoader.CreateDefault(LoggerFactory?.CreateLogger<RuleFileLoader>()).LoadFromDirectories(Layout.RulesPaths);

    public IReadOnlyList<(RuleDefinition Rule, string SourceFile)> LoadRulesWithSource() =>
        RuleFileLoader.CreateDefault(LoggerFactory?.CreateLogger<RuleFileLoader>()).LoadFromDirectoriesWithSource(Layout.RulesPaths);

    /// <summary>
    /// Non-throwing counterpart to <see cref="LoadRules"/>/<see cref="LoadRulesWithSource"/> -
    /// validates every configured rule file and reports every problem found instead of throwing on
    /// the first one. Used by `rules validate` and `validate`'s pre-flight rule-set gate
    /// (`docs/done/RULE_VALIDATION_PLAN.md`).
    /// </summary>
    public RuleSetValidationReport ValidateRules() =>
        RuleFileLoader.CreateDefault(LoggerFactory?.CreateLogger<RuleFileLoader>()).ValidateDirectories(Layout.RulesPaths);

    /// <summary>
    /// Shared guard for every command that needs at least one resolved rules directory before
    /// proceeding (`rules validate`/`list`/`explain`/`create`, `validate`). Centralizes the message so
    /// it can't drift between commands - previously only `rules create` had this check.
    /// </summary>
    public bool TryRequireRulesConfigured(TextWriter errorWriter)
    {
        if (Layout.RulesPaths.Count > 0)
        {
            return true;
        }

        errorWriter.WriteLine(
            "No rules directory is configured. Run 'codeguard setup', pass --rules-source, " +
            "or add a .codeguard/config.yml with a rules path.");
        return false;
    }

    private static CodeGuardConfig WithRules(CodeGuardConfig source, IReadOnlyList<string> rules) => new()
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
