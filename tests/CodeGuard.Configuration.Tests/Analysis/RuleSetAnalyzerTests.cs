using CodeGuard.Configuration.Analysis;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Configuration.Loading;

namespace CodeGuard.Configuration.Tests.Analysis;

public sealed class RuleSetAnalyzerTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("codeguard-analyze-tests-").FullName;
    private readonly CapabilityCatalog _catalog = CapabilityCatalog.Create();

    [Fact]
    public void Analyze_ValidRuleWithBothTestOutcomes_ReportsNoFindings()
    {
        WriteRuleFile("valid.yml", RuleYaml("DDD-ENTITY-001", withTests: true));

        var report = Analyze();

        Assert.False(report.HasFindings);
        Assert.Equal(1, report.RuleCount);
        Assert.Empty(report.RulesWithoutTests);
        Assert.Empty(report.OneSidedTestRules);
    }

    [Fact]
    public void Analyze_InvalidRuleFile_IsReportedSeparatelyFromDuplicates()
    {
        WriteRuleFile("bad.yml", """
            id: DDD-ENTITY-001
            name: Some rule
            target:
              kind: not_a_real_kind
            assertions:
              - must_inherit_from:
                  type: "Contoso.Domain.Entity<TId>"
            """);

        var report = Analyze();

        Assert.True(report.HasFindings);
        Assert.Single(report.InvalidRules);
        Assert.Empty(report.DuplicateIds);
    }

    [Fact]
    public void Analyze_DuplicateRuleId_IsReportedSeparatelyFromInvalidRules()
    {
        WriteRuleFile("a.yml", RuleYaml("DDD-ENTITY-001", withTests: false));
        WriteRuleFile("b.yml", RuleYaml("DDD-ENTITY-001", withTests: false));

        var report = Analyze();

        Assert.Single(report.DuplicateIds);
        Assert.Empty(report.InvalidRules);
    }

    [Fact]
    public void Analyze_RuleWithNoTests_IsReportedAsRulesWithoutTests()
    {
        WriteRuleFile("no-tests.yml", RuleYaml("DDD-ENTITY-001", withTests: false));

        var report = Analyze();

        Assert.Equal(["DDD-ENTITY-001"], report.RulesWithoutTests);
        Assert.Empty(report.OneSidedTestRules);
    }

    [Fact]
    public void Analyze_RuleWithOnlyPassCases_IsReportedAsOneSided()
    {
        WriteRuleFile("pass-only.yml", $$"""
            id: DDD-ENTITY-001
            name: Some rule
            target:
              kind: class
              namespace: "Contoso.Domain.Entities"
            assertions:
              - must_inherit_from:
                  type: "Contoso.Domain.Entity<TId>"
            tests:
              - name: A passing case
                setup:
                  types:
                    - name: Order
                      namespace: Contoso.Domain.Entities
                      baseType: "Contoso.Domain.Entity<Guid>"
                expect: pass
            """);

        var report = Analyze();

        Assert.Empty(report.RulesWithoutTests);
        Assert.Equal(["DDD-ENTITY-001"], report.OneSidedTestRules);
    }

    [Fact]
    public void Analyze_AssertionThatCannotApplyToTarget_IsReportedAsUnreachable()
    {
        // `must_not_reference_project` only applies to a Project candidate (per its
        // CapabilityDescriptor.AppliesTo), but this rule targets a `class` selector, which
        // produces a Type candidate - so the assertion can never actually run.
        WriteRuleFile("unreachable.yml", """
            id: DDD-ENTITY-001
            name: Some rule
            target:
              kind: class
              namespace: "Contoso.Domain.Entities"
            assertions:
              - must_not_reference_project:
                  name: "*Infrastructure*"
            """);

        var report = Analyze();

        var issue = Assert.Single(report.UnreachableRules);
        Assert.Equal("DDD-ENTITY-001", issue.RuleId);
        Assert.Equal("class", issue.TargetKind);
        Assert.Equal("must_not_reference_project", issue.AssertionKind);
    }

    [Fact]
    public void Analyze_QuantifierAssertion_IsNeverReportedAsUnreachable()
    {
        // must_all_match ignores the outer candidate entirely (it evaluates a nested selector), so
        // its CapabilityDescriptor.AppliesTo is empty ("any") - it must never be flagged.
        WriteRuleFile("quantifier.yml", """
            id: DDD-ENTITY-001
            name: Some rule
            target:
              kind: class
              namespace: "Contoso.Domain.Entities"
            assertions:
              - must_all_match:
                  selector:
                    kind: method
                    name: "*"
                  assertions:
                    - must_have_modifier:
                        modifier: static
            """);

        var report = Analyze();

        Assert.Empty(report.UnreachableRules);
    }

    [Fact]
    public void Analyze_TwoRulesWithIdenticalTargetAndAssertions_AreGroupedAsExactDuplicates()
    {
        WriteRuleFile("a.yml", RuleYaml("DDD-ENTITY-001", withTests: false));
        WriteRuleFile("b.yml", RuleYaml("DDD-ENTITY-002", withTests: false));

        var report = Analyze();

        var group = Assert.Single(report.ExactDuplicateRules);
        Assert.Equal(["DDD-ENTITY-001", "DDD-ENTITY-002"], group.RuleIds);
    }

    [Fact]
    public void Analyze_ParameterOrderDiffersButValueIsTheSame_StillCountsAsExactDuplicate()
    {
        // Same must_have_count params, written in a different key order - a textual diff would miss
        // this, which is exactly why comparison goes through a canonical (key-sorted) form.
        WriteRuleFile("a.yml", """
            id: DDD-ENTITY-001
            name: Some rule
            target:
              kind: repository
            assertions:
              - must_have_count:
                  selector:
                    kind: type
                    namespace: "Contoso.Domain.Entities"
                  exactly: 1
            """);
        WriteRuleFile("b.yml", """
            id: DDD-ENTITY-002
            name: Some other rule
            target:
              kind: repository
            assertions:
              - must_have_count:
                  exactly: 1
                  selector:
                    namespace: "Contoso.Domain.Entities"
                    kind: type
            """);

        var report = Analyze();

        var group = Assert.Single(report.ExactDuplicateRules);
        Assert.Equal(["DDD-ENTITY-001", "DDD-ENTITY-002"], group.RuleIds);
    }

    [Fact]
    public void Analyze_RulesDifferingOnlyInWhen_AreNotGroupedAsExactDuplicates()
    {
        // Regression test: the canonical shape compared here must include `when`, not just
        // `target`+`assertions` - these two rules would otherwise look identical, even though
        // `when` means they apply to a different, non-overlapping set of candidates.
        WriteRuleFile("a.yml", """
            id: DDD-ENTITY-001
            name: Some rule
            target:
              kind: class
              namespace: "Contoso.Domain.Entities"
            when:
              must_have_attribute:
                type: "System.ObsoleteAttribute"
            assertions:
              - must_inherit_from:
                  type: "Contoso.Domain.Entity<TId>"
            """);
        WriteRuleFile("b.yml", """
            id: DDD-ENTITY-002
            name: Some other rule
            target:
              kind: class
              namespace: "Contoso.Domain.Entities"
            assertions:
              - must_inherit_from:
                  type: "Contoso.Domain.Entity<TId>"
            """);

        var report = Analyze();

        Assert.Empty(report.ExactDuplicateRules);
    }

    [Fact]
    public void Analyze_RuleWithoutMetadataSource_IsReportedAsMissingProvenanceButNotAFinding()
    {
        WriteRuleFile("no-provenance.yml", RuleYaml("DDD-ENTITY-001", withTests: true));

        var report = Analyze();

        Assert.Equal(["DDD-ENTITY-001"], report.RulesMissingProvenance);
        Assert.False(report.HasFindings);
    }

    [Fact]
    public void Analyze_RuleWithMetadataSource_IsNotReportedAsMissingProvenance()
    {
        WriteRuleFile("with-provenance.yml", """
            id: DDD-ENTITY-001
            name: Some rule
            metadata:
              source:
                document: Architecture Standards
            target:
              kind: class
              namespace: "Contoso.Domain.Entities"
            assertions:
              - must_inherit_from:
                  type: "Contoso.Domain.Entity<TId>"
            """);

        var report = Analyze();

        Assert.Empty(report.RulesMissingProvenance);
    }

    [Fact]
    public void Analyze_DisabledAndIllustrativeRules_AreCountedButDoNotCountAsFindings()
    {
        WriteRuleFile("disabled.yml", $$"""
            id: DDD-ENTITY-001
            name: Some rule
            enabled: false
            illustrative: true
            target:
              kind: class
              namespace: "Contoso.Domain.Entities"
            assertions:
              - must_inherit_from:
                  type: "Contoso.Domain.Entity<TId>"
            tests:
              - name: A passing case
                setup:
                  types:
                    - name: Order
                      namespace: Contoso.Domain.Entities
                      baseType: "Contoso.Domain.Entity<Guid>"
                expect: pass
              - name: A failing case
                setup:
                  types:
                    - name: LegacyThing
                      namespace: Contoso.Domain.Entities
                expect: fail
            """);

        var report = Analyze();

        Assert.Equal(["DDD-ENTITY-001"], report.DisabledRules);
        Assert.Equal(["DDD-ENTITY-001"], report.IllustrativeRules);
        Assert.False(report.HasFindings);
    }

    private RuleAnalysisReport Analyze()
    {
        var validation = RuleFileLoader.CreateDefault().ValidateDirectories([_directory]);
        return RuleSetAnalyzer.Analyze(validation, _catalog);
    }

    private static string RuleYaml(string id, bool withTests) => withTests
        ? $$"""
            id: {{id}}
            name: Some rule
            target:
              kind: class
              namespace: "Contoso.Domain.Entities"
            assertions:
              - must_inherit_from:
                  type: "Contoso.Domain.Entity<TId>"
            tests:
              - name: A passing case
                setup:
                  types:
                    - name: Order
                      namespace: Contoso.Domain.Entities
                      baseType: "Contoso.Domain.Entity<Guid>"
                expect: pass
              - name: A failing case
                setup:
                  types:
                    - name: LegacyThing
                      namespace: Contoso.Domain.Entities
                expect: fail
            """
        : $"""
            id: {id}
            name: Some rule
            target:
              kind: class
              namespace: "Contoso.Domain.Entities"
            assertions:
              - must_inherit_from:
                  type: "Contoso.Domain.Entity<TId>"
            """;

    private void WriteRuleFile(string relativePath, string yaml) =>
        File.WriteAllText(Path.Combine(_directory, relativePath), yaml);

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
