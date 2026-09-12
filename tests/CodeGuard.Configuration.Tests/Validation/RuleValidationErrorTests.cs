using CodeGuard.Configuration.Loading;
using CodeGuard.Configuration.Validation;

namespace CodeGuard.Configuration.Tests.Validation;

/// <summary>
/// Covers the machine-readable half of rule validation. Codes and paths are a CLI contract an agent
/// branches on, so they are asserted explicitly rather than via message text.
/// </summary>
public sealed class RuleValidationErrorTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("codeguard-validation-errors-").FullName;

    [Fact]
    public void UnknownSelectorKind_IsCodedAndSuggestsTheNearestKind()
    {
        var error = SingleErrorFor("""
            id: X-001
            name: Some rule
            target:
              kind: clas
            assertions:
              - must_inherit_from:
                  type: "Entity<*>"
            """);

        Assert.Equal(RuleErrorCodes.UnknownSelectorKind, error.Code);
        Assert.Contains("Did you mean 'class'?", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownAssertionKind_IsCodedAndSuggestsTheNearestKind()
    {
        var error = SingleErrorFor("""
            id: X-002
            name: Some rule
            target:
              kind: class
              namespace: "Contoso"
            assertions:
              - must_inherits_from:
                  type: "Entity<*>"
            """);

        Assert.Equal(RuleErrorCodes.UnknownAssertionKind, error.Code);
        Assert.Contains("Did you mean 'must_inherit_from'?", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WildlyUnknownKind_IsCodedButSuggestsNothing()
    {
        var error = SingleErrorFor("""
            id: X-003
            name: Some rule
            target:
              kind: business_logic_quality
            assertions:
              - must_inherit_from:
                  type: "Entity<*>"
            """);

        Assert.Equal(RuleErrorCodes.UnknownSelectorKind, error.Code);
        Assert.DoesNotContain("Did you mean", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingRequiredParameter_IsCodedAndPointsAtTheProperty()
    {
        var error = SingleErrorFor("""
            id: X-004
            name: Some rule
            target:
              kind: class
              namespace: "Contoso"
            assertions:
              - must_inherit_from: {}
            """);

        Assert.Equal(RuleErrorCodes.InvalidParameter, error.Code);
        Assert.Equal("/type", error.Path);
    }

    [Fact]
    public void MissingNestedSelector_IsCodedAndPointsAtTheProperty()
    {
        var error = SingleErrorFor("""
            id: X-005
            name: Some rule
            target:
              kind: repository
            assertions:
              - must_not_exist: {}
            """);

        Assert.Equal(RuleErrorCodes.InvalidParameter, error.Code);
        Assert.Equal("/selector", error.Path);
    }

    [Fact]
    public void SchemaViolation_IsCodedAndCarriesAJsonPointer()
    {
        // `severity` is constrained by an enum in the schema, so this fails before parsing.
        var error = SingleErrorFor("""
            id: X-006
            name: Some rule
            severity: catastrophic
            target:
              kind: class
              namespace: "Contoso"
            assertions:
              - must_inherit_from:
                  type: "Entity<*>"
            """);

        Assert.Equal(RuleErrorCodes.SchemaViolation, error.Code);
        Assert.Equal("/severity", error.Path);
    }

    [Fact]
    public void UnknownTopLevelField_IsReportedRatherThanIgnored()
    {
        // The schema is additionalProperties:false - `metadata` is a known field now (see the test
        // below), but an invented one still isn't.
        var error = SingleErrorFor("""
            id: X-007
            name: Some rule
            provenance: standards.md
            target:
              kind: class
              namespace: "Contoso"
            assertions:
              - must_inherit_from:
                  type: "Entity<*>"
            """);

        Assert.Equal(RuleErrorCodes.SchemaViolation, error.Code);
    }

    private RuleValidationError SingleErrorFor(string yaml)
    {
        File.WriteAllText(Path.Combine(_directory, $"{Guid.NewGuid():N}.yml"), yaml);
        var report = RuleFileLoader.CreateDefault().ValidateDirectories([_directory]);
        var issue = Assert.Single(report.Issues);
        return issue.Errors[0];
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
