namespace CodeGuard.RuleFuzzing.Tests.Generation;

/// <summary>
/// Tunable knobs for the rule-document generator. Iteration counts default low (CI-friendly) and can
/// be raised locally via environment variables for a deeper soak - see README.md.
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
    /// <paramref name="ciDefault"/> - the low, fast count that rides along in the normal `dotnet test`
    /// run. Set the env var locally (e.g. `RULEFUZZ_ITERATIONS=20000`) for a deeper soak.
    /// </summary>
    public static int Iterations(string envVar, int ciDefault) =>
        int.TryParse(Environment.GetEnvironmentVariable(envVar), out var value) && value > 0
            ? value
            : ciDefault;
}
