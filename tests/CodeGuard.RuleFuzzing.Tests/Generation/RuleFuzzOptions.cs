namespace CodeGuard.RuleFuzzing.Tests.Generation;

/// <summary>
/// Tunable knobs for the rule-document generator. Default iteration counts run the full soak (several
/// thousand per oracle) every time, including a plain local `dotnet test` - the whole suite is
/// in-process against virtual models (no disk/Roslyn/MSBuild) and completes in well under 30 seconds
/// even at these depths, so there's no need to gate real coverage behind an opt-in env var. The env
/// vars below still exist to push further for an even deeper one-off soak - see README.md.
/// </summary>
public static class RuleFuzzOptions
{
    public const int MaxNestingDepth = 3;
    public const int MinAssertionsPerRule = 1;
    public const int MaxAssertionsPerRule = 3;

    /// <summary>Out of 4 total: 3-in-4 chance an optional parameter is included at all.</summary>
    public const int IncludeOptionalWeight = 3;
    public const int OmitOptionalWeight = 1;

    /// <summary>Out of 4 total: 1-in-4 chance a parameter value is drawn from the adversarial corpus.</summary>
    public const int AdversarialValueWeight = 1;
    public const int BenignValueWeight = 3;

    /// <summary>
    /// Reads an iteration count from <paramref name="envVar"/> if set to a positive integer, otherwise
    /// <paramref name="defaultIterations"/> - already a full-depth soak, run unconditionally by every
    /// `dotnet test` invocation. Set the env var locally (e.g. `RULEFUZZ_ITERATIONS=50000`) to push
    /// past that for a one-off, even-deeper soak.
    /// </summary>
    public static int Iterations(string envVar, int defaultIterations) =>
        int.TryParse(Environment.GetEnvironmentVariable(envVar), out var value) && value > 0
            ? value
            : defaultIterations;
}
