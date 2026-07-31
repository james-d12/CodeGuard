namespace CodeGuard.Configuration.GlobalConfig;

public enum RuleSourceKind
{
    Directory,
    Git
}

/// <summary>
/// Persisted at `&lt;app-data-root&gt;/settings.yml` via <see cref="GlobalSettingsStore"/> - the
/// rule source configured by `codeguard setup`, used as a last-resort fallback by
/// <c>CliRepositoryContext</c> when a target repo has no `.codeguard/config.yml` of its own.
/// </summary>
public sealed class GlobalSettings
{
    public required RuleSourceKind Kind { get; init; }

    public required string Location { get; init; }

    /// <summary>Git only; null means track the remote's default branch.</summary>
    public string? Branch { get; init; }
}
