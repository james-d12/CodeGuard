using System.Text.RegularExpressions;
using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Evaluation.Assertions;

/// <summary>
/// Generic package-version constraint: <paramref name="constraint"/> is a comparator
/// (<c>&gt;=</c>, <c>&lt;=</c>, <c>&gt;</c>, <c>&lt;</c>, <c>==</c>, <c>!=</c>; defaults to
/// <c>==</c> if the constraint is a bare version) followed by a dotted numeric version, e.g.
/// <c>"&gt;=8.0.0"</c>. Deliberately one parameterized primitive rather than separate
/// at-least/at-most/exactly kinds (docs/PRIMITIVES.md §15), per docs/REFACTORING.md §2.1's
/// genericity principle. Comparison is numeric-segment-only: any pre-release suffix after a
/// <c>-</c> is stripped before comparing, so this is not a full SemVer precedence implementation -
/// sufficient for ordinary package floor/ceiling policies.
/// </summary>
public sealed class MustUsePackageVersionAssertion(string packageIdPattern, string constraint) : IAssertion
{
    private static readonly Regex ConstraintPattern = new(@"^(>=|<=|==|!=|>|<)?\s*(.+)$");

    public string Kind => "must_use_package_version";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not ProjectModel project)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against projects.");
        }

        var matches = project.PackageReferences.Where(p => GlobMatcher.IsMatch(p.Id, packageIdPattern)).ToList();
        if (matches.Count == 0)
        {
            return AssertionOutcome.Failure($"Project '{project.Name}' does not reference a package matching '{packageIdPattern}'.");
        }

        var match = ConstraintPattern.Match(constraint);
        var op = match.Groups[1].Success && match.Groups[1].Value.Length > 0 ? match.Groups[1].Value : "==";
        var requiredVersion = ParseVersion(match.Groups[2].Value);

        var offending = matches.Where(p => !Satisfies(ParseVersion(p.Version), op, requiredVersion)).ToList();

        return offending.Count == 0
            ? AssertionOutcome.Success()
            : AssertionOutcome.Failure(
                $"Project '{project.Name}' must reference '{packageIdPattern}' with version {constraint} " +
                $"(found: {string.Join(", ", offending.Select(p => $"{p.Id} {p.Version}"))}).");
    }

    private static bool Satisfies(Version actual, string op, Version required) => op switch
    {
        ">=" => actual >= required,
        "<=" => actual <= required,
        ">" => actual > required,
        "<" => actual < required,
        "!=" => actual != required,
        _ => actual == required
    };

    private static Version ParseVersion(string raw)
    {
        var numeric = raw.Split('-', 2)[0].Trim();
        if (!numeric.Contains('.'))
        {
            numeric += ".0";
        }

        return Version.TryParse(numeric, out var version) ? version : new Version(0, 0);
    }
}
