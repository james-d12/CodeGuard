using System.CommandLine;
using CodeGuard.Cli.Support;
using CodeGuard.Configuration.GlobalConfig;
using CodeGuard.Configuration.Loading;

namespace CodeGuard.Cli.Commands;

/// <summary>
/// Configures the rule source used as a fallback by every other command (see
/// <see cref="CliRepositoryContext"/>) when a target repo has no `.codeguard/config.yml` of its
/// own and no `--rules-source` override was passed. Persisted outside any repo, in the OS
/// user/app-data location (<see cref="GlobalSettingsPaths"/>).
///
/// Deliberately the *only* command that syncs a git rule source - re-run this to refresh; every
/// other command just reads whatever's already on disk (see docs/done/SETUP_COMMAND_PLAN.md,
/// "Determinism over convenience").
///
/// Bare `codeguard setup` (no `--source`/`--type`) is interactive and behaves differently
/// depending on whether a source is already configured:
///   - First run (no `settings.yml` yet): offers a 3-way menu - point at an existing local
///     directory, point at a git repo to clone, or "start fresh" and let CodeGuard create and
///     manage an empty rules folder under the app-data root (so a brand-new user isn't blocked on
///     already having a rules repo before `codeguard rules create` will work).
///   - Re-run (already configured): prints the current Kind/Location/Branch/resolved path and
///     rule count, then asks whether to update. Answering no (the default) just refreshes in place
///     (git sync, or a fresh rule-file count for a directory source) without re-prompting for a
///     source; answering yes falls into the same 3-way menu as a first run and overwrites
///     `settings.yml` on completion.
///
/// Non-interactive flags (`--source`, `--type`, `--branch`) skip all of the above and act
/// immediately with no prompts and no status display, so scripted/CI invocations are unaffected.
/// `--type managed` is the scriptable equivalent of the "start fresh" menu choice.
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
            Description = "Force the source kind instead of auto-detecting from --source: directory, git, or managed " +
                "(let CodeGuard create and manage a new rules folder - no --source needed)."
        };
        typeOption.AcceptOnlyFromAmong("directory", "git", "managed");

        var command = new Command("setup", "Configure the rules source used by validate/rules list/etc. across all repos");
        command.Add(sourceOption);
        command.Add(branchOption);
        command.Add(typeOption);

        command.SetAction((parseResult, _) =>
        {
            var suppliedSource = parseResult.GetValue(sourceOption);
            var branch = parseResult.GetValue(branchOption);
            var typeValue = parseResult.GetValue(typeOption);
            var settingsRoot = GlobalSettingsPaths.ResolveRoot();

            if (typeValue == "managed")
            {
                if (suppliedSource is not null)
                {
                    Console.Error.WriteLine("--source cannot be combined with --type managed.");
                    return Task.FromResult(1);
                }

                var managedPath = Path.Combine(settingsRoot, "rules");
                return Task.FromResult(ApplyAndSave(RuleSourceKind.Directory, managedPath, branch: null, createDirectoryIfMissing: true, settingsRoot));
            }

            if (suppliedSource is not null)
            {
                if (string.IsNullOrWhiteSpace(suppliedSource))
                {
                    Console.Error.WriteLine("A rules source is required.");
                    return Task.FromResult(1);
                }

                var kind = typeValue switch
                {
                    "directory" => RuleSourceKind.Directory,
                    "git" => RuleSourceKind.Git,
                    _ => RuleSourceResolver.DetectKind(suppliedSource)
                };

                return Task.FromResult(ApplyAndSave(kind, suppliedSource, branch, createDirectoryIfMissing: false, settingsRoot));
            }

            if (typeValue is "directory" or "git")
            {
                var kind = typeValue == "directory" ? RuleSourceKind.Directory : RuleSourceKind.Git;
                Console.Write(kind == RuleSourceKind.Directory ? "Directory path: " : "Git URL: ");
                var source = Console.ReadLine()?.Trim();
                if (string.IsNullOrWhiteSpace(source))
                {
                    Console.Error.WriteLine("A rules source is required.");
                    return Task.FromResult(1);
                }

                if (kind == RuleSourceKind.Git && branch is null)
                {
                    Console.Write("Branch (blank = repo's default branch): ");
                    var branchInput = Console.ReadLine()?.Trim();
                    branch = string.IsNullOrWhiteSpace(branchInput) ? null : branchInput;
                }

                return Task.FromResult(ApplyAndSave(kind, source, branch, createDirectoryIfMissing: false, settingsRoot));
            }

            // Bare `codeguard setup`: interactive, status-aware.
            var existing = GlobalSettingsStore.Load(GlobalSettingsPaths.SettingsFilePath(settingsRoot));
            if (existing is not null)
            {
                PrintCurrentSettings(existing, settingsRoot);
                Console.Write("Update this configuration? (y/N): ");
                var answer = Console.ReadLine()?.Trim();
                var wantsUpdate = answer is not null &&
                    (answer.Equals("y", StringComparison.OrdinalIgnoreCase) || answer.Equals("yes", StringComparison.OrdinalIgnoreCase));

                if (!wantsUpdate)
                {
                    return Task.FromResult(ApplyAndSave(existing.Kind, existing.Location, existing.Branch, createDirectoryIfMissing: false, settingsRoot));
                }

                Console.WriteLine();
                var (menuKind, menuSource, menuBranch, menuCreateDir) = PromptForNewSource(settingsRoot);
                return Task.FromResult(ApplyAndSave(menuKind, menuSource, menuBranch, menuCreateDir, settingsRoot));
            }

            Console.WriteLine("No rules source is configured yet.");
            var (newKind, newSource, newBranch, newCreateDir) = PromptForNewSource(settingsRoot);
            return Task.FromResult(ApplyAndSave(newKind, newSource, newBranch, newCreateDir, settingsRoot));
        });

        return command;
    }

    private static void PrintCurrentSettings(GlobalSettings settings, string settingsRoot)
    {
        Console.WriteLine("Current rules source:");
        Console.WriteLine($"  Kind:     {settings.Kind.ToString().ToLowerInvariant()}");
        Console.WriteLine($"  Location: {settings.Location}");
        if (settings.Kind == RuleSourceKind.Git)
        {
            Console.WriteLine($"  Branch:   {settings.Branch ?? "(default)"}");
        }

        var resolvedPath = settings.Kind == RuleSourceKind.Directory
            ? settings.Location
            : GlobalSettingsPaths.RulesCacheDirectory(settingsRoot, settings.Location);

        if (Directory.Exists(resolvedPath))
        {
            var ruleCount = RuleFileLoader.CreateDefault().LoadFromDirectories([resolvedPath]).Count;
            Console.WriteLine($"  Resolved: {resolvedPath} ({ruleCount} rule file(s))");
        }
        else
        {
            Console.WriteLine($"  Resolved: {resolvedPath} (not yet synced)");
        }

        Console.WriteLine();
    }

    private static (RuleSourceKind Kind, string Source, string? Branch, bool CreateDirectoryIfMissing) PromptForNewSource(string settingsRoot)
    {
        Console.WriteLine("How should CodeGuard find your rules?");
        Console.WriteLine("  1) A local directory you already have");
        Console.WriteLine("  2) A git repository (CodeGuard will clone it)");
        Console.WriteLine("  3) Start fresh - CodeGuard creates and manages a new rules folder for you");

        while (true)
        {
            Console.Write("Choice [1-3]: ");
            var choice = Console.ReadLine()?.Trim();
            switch (choice)
            {
                case "1":
                {
                    Console.Write("Directory path: ");
                    var dir = Console.ReadLine()?.Trim();
                    if (string.IsNullOrWhiteSpace(dir))
                    {
                        Console.WriteLine("A value is required.");
                        continue;
                    }

                    return (RuleSourceKind.Directory, dir, null, false);
                }
                case "2":
                {
                    Console.Write("Git URL: ");
                    var url = Console.ReadLine()?.Trim();
                    if (string.IsNullOrWhiteSpace(url))
                    {
                        Console.WriteLine("A value is required.");
                        continue;
                    }

                    Console.Write("Branch (blank = repo's default branch): ");
                    var branchInput = Console.ReadLine()?.Trim();
                    var branch = string.IsNullOrWhiteSpace(branchInput) ? null : branchInput;
                    return (RuleSourceKind.Git, url, branch, false);
                }
                case "3":
                    return (RuleSourceKind.Directory, Path.Combine(settingsRoot, "rules"), null, true);
                default:
                    Console.WriteLine("Please enter 1, 2, or 3.");
                    continue;
            }
        }
    }

    private static int ApplyAndSave(RuleSourceKind kind, string source, string? branch, bool createDirectoryIfMissing, string settingsRoot)
    {
        string resolvedPath;
        if (kind == RuleSourceKind.Directory)
        {
            if (!Directory.Exists(source))
            {
                if (!createDirectoryIfMissing)
                {
                    Console.Error.WriteLine($"Directory '{source}' was not found.");
                    return 1;
                }

                Directory.CreateDirectory(source);
            }

            resolvedPath = Path.GetFullPath(source);
        }
        else
        {
            var cacheDir = GlobalSettingsPaths.RulesCacheDirectory(settingsRoot, source);
            GitSyncResult syncResult;
            try
            {
                syncResult = GitRuleSourceSync.SyncOrClone(source, branch, cacheDir);
            }
            catch (InvalidOperationException ex)
            {
                Console.Error.WriteLine($"Failed to sync '{source}': {ex.Message}");
                return 1;
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
                return 1;
            }

            resolvedPath = cacheDir;
        }

        GlobalSettingsStore.Save(
            GlobalSettingsPaths.SettingsFilePath(settingsRoot),
            new GlobalSettings { Kind = kind, Location = source, Branch = branch });

        var ruleCount = RuleFileLoader.CreateDefault().LoadFromDirectories([resolvedPath]).Count;
        Console.WriteLine($"Configured rules source: {source}{(branch is null ? "" : $" (branch {branch})")}");
        Console.WriteLine($"Found {ruleCount} rule file(s) at {resolvedPath}.");

        if (createDirectoryIfMissing)
        {
            Console.WriteLine("Run 'codeguard rules create' to scaffold your first rule.");
        }

        return 0;
    }
}
