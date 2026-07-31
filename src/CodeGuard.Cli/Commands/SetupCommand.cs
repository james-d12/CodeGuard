using System.CommandLine;
using CodeGuard.Cli.Support;
using CodeGuard.Configuration.GlobalConfig;
using CodeGuard.Configuration.Loading;

namespace CodeGuard.Cli.Commands;

/// <summary>
/// Configures the rule source used as a fallback by every other command (see
/// <see cref="CliRepositoryContext"/>) when a target repo has no `.rulesengine/config.yml` of its
/// own and no `--rules-source` override was passed. Persisted outside any repo, in the OS
/// user/app-data location (<see cref="GlobalSettingsPaths"/>).
///
/// Deliberately the *only* command that syncs a git rule source - re-run this to refresh; every
/// other command just reads whatever's already on disk (see docs/SETUP_COMMAND_PLAN.md, "Determinism
/// over convenience").
/// </summary>
public static class SetupCommand
{
    public static Command Build()
    {
        var sourceOption = new Option<string?>("--source")
        {
            Description = "Rules location: a local directory path or a git repository URL. Prompted for interactively if omitted."
        };

        var branchOption = new Option<string?>("--branch")
        {
            Description = "Git branch to track (default: the repo's default branch). Only meaningful for a git source."
        };

        var typeOption = new Option<string?>("--type")
        {
            Description = "Force the source kind instead of auto-detecting from --source: directory or git."
        };
        typeOption.AcceptOnlyFromAmong("directory", "git");

        var command = new Command("setup", "Configure the rules source used by validate/rules list/etc. across all repos");
        command.Add(sourceOption);
        command.Add(branchOption);
        command.Add(typeOption);

        command.SetAction((parseResult, _) =>
        {
            var suppliedSource = parseResult.GetValue(sourceOption);
            var isInteractive = suppliedSource is null;
            var branch = parseResult.GetValue(branchOption);
            var typeValue = parseResult.GetValue(typeOption);

            var source = suppliedSource;
            if (isInteractive)
            {
                Console.Write("Rules source (local directory path or git URL): ");
                source = Console.ReadLine()?.Trim();
            }

            if (string.IsNullOrWhiteSpace(source))
            {
                Console.Error.WriteLine("A rules source is required.");
                return Task.FromResult(1);
            }

            var kind = typeValue switch
            {
                "directory" => RuleSourceKind.Directory,
                "git" => RuleSourceKind.Git,
                _ => RuleSourceResolver.DetectKind(source)
            };

            if (kind == RuleSourceKind.Git && isInteractive && branch is null)
            {
                Console.Write("Branch (blank = repo's default branch): ");
                var branchInput = Console.ReadLine()?.Trim();
                branch = string.IsNullOrWhiteSpace(branchInput) ? null : branchInput;
            }

            string resolvedPath;
            if (kind == RuleSourceKind.Directory)
            {
                if (!Directory.Exists(source))
                {
                    Console.Error.WriteLine($"Directory '{source}' was not found.");
                    return Task.FromResult(1);
                }

                resolvedPath = Path.GetFullPath(source);
            }
            else
            {
                var cacheDir = GlobalSettingsPaths.RulesCacheDirectory(GlobalSettingsPaths.ResolveRoot(), source);
                GitSyncResult syncResult;
                try
                {
                    syncResult = GitRuleSourceSync.SyncOrClone(source, branch, cacheDir);
                }
                catch (InvalidOperationException ex)
                {
                    Console.Error.WriteLine($"Failed to sync '{source}': {ex.Message}");
                    return Task.FromResult(1);
                }

                Console.WriteLine(syncResult switch
                {
                    GitSyncResult.Cloned => $"Cloned into {cacheDir}.",
                    GitSyncResult.AlreadyUpToDate => "Already up to date.",
                    GitSyncResult.FastForwarded => "Fast-forwarded to the latest commit.",
                    GitSyncResult.Blocked =>
                        $"The local cache at {cacheDir} has diverged or has uncommitted changes - left untouched. " +
                        "Resolve manually, or delete that directory and re-run setup.",
                    _ => throw new ArgumentOutOfRangeException(nameof(syncResult))
                });

                if (syncResult == GitSyncResult.Blocked)
                {
                    return Task.FromResult(1);
                }

                resolvedPath = cacheDir;
            }

            GlobalSettingsStore.Save(
                GlobalSettingsPaths.SettingsFilePath(GlobalSettingsPaths.ResolveRoot()),
                new GlobalSettings { Kind = kind, Location = source, Branch = branch });

            var ruleCount = RuleFileLoader.CreateDefault().LoadFromDirectories([resolvedPath]).Count;
            Console.WriteLine($"Configured rules source: {source}{(branch is null ? "" : $" (branch {branch})")}");
            Console.WriteLine($"Found {ruleCount} rule file(s) at {resolvedPath}.");

            return Task.FromResult(0);
        });

        return command;
    }
}
