namespace CodeGuard.Cli.Tests;

/// <summary>
/// Redirects <c>GlobalSettingsPaths.ResolveRoot()</c>'s tier-4 lookup (a prior `codeguard setup`
/// run) to a fresh, empty temp directory for the scope's lifetime, so end-to-end command tests of
/// "nothing configured anywhere" are deterministic regardless of whether this machine has ever run
/// `codeguard setup` for real. Works because <c>GlobalSettingsPaths.ResolveRoot()</c> honors
/// <c>XDG_CONFIG_HOME</c> on Linux - the only platform CI (`ubuntu-latest`) and local dev here run.
/// Callers must join <see cref="ConsoleOutputCollection"/> (or otherwise avoid running in parallel
/// with other tests) since the environment variable mutation is process-global.
/// </summary>
public sealed class IsolatedGlobalSettingsScope : IDisposable
{
    private readonly string? _originalXdgConfigHome;
    private readonly string _root;

    public IsolatedGlobalSettingsScope()
    {
        _root = Directory.CreateTempSubdirectory("codeguard-isolated-global-").FullName;
        _originalXdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", _root);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", _originalXdgConfigHome);
        Directory.Delete(_root, recursive: true);
    }
}
