namespace CodeGuard.RuleFuzzing.Tests.Generation;

/// <summary>
/// Whether a generated assertion's <c>AppliesTo</c> is drawn to match, or deliberately mismatch, its
/// selector's <c>Produces</c>. See docs/RULE_FUZZING_PLAN.md's "Static/dynamic agreement" oracle.
/// </summary>
internal enum PairingMode
{
    Compatible,
    Incompatible
}
