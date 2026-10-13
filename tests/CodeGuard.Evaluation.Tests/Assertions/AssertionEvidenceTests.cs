using System.Text.Json.Nodes;
using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Evaluation.Tests.Assertions;

/// <summary>
/// Structured Expected/Actual/Evidence carried on <see cref="AssertionOutcome"/> (issue #69). These sit
/// alongside, not instead of, the human-readable Message, which the other test classes continue to assert on.
/// </summary>
public class AssertionEvidenceTests
{
    private static readonly RepositoryModel EmptyModel = new(".", [], [], [], [], [], [], [], [], []);
    private static readonly JsonObject Template = new() { ["kind"] = "stub" };

    private sealed class StubSelector(IEnumerable<object> candidates) : ITargetSelector
    {
        public string Kind => "stub";
        public IEnumerable<object> SelectCandidates(RepositoryModel model) => candidates;
    }

    private sealed class StubAssertion(Func<object, AssertionOutcome> evaluate) : IAssertion
    {
        public string Kind => "stub_assertion";

        public AssertionOutcome Evaluate(object candidate, RepositoryModel model) => evaluate(candidate);
    }

    [Fact]
    public void MustInheritFrom_Failure_CarriesPatternAsExpected_BaseTypeAsActual_TypeAsEvidence()
    {
        var type = TestModels.Type("Contoso.Domain.Entities.LegacyThing", baseType: "Contoso.Other.Base");

        var outcome = new MustInheritFromAssertion("Contoso.Domain.Entity<*>").Evaluate(type, EmptyModel);

        Assert.False(outcome.Passed);
        Assert.Equal("Contoso.Domain.Entity<*>", outcome.Expected);
        Assert.Equal("Contoso.Other.Base", outcome.Actual);
        Assert.Equal(["Contoso.Domain.Entities.LegacyThing"], outcome.Evidence);
        Assert.Contains("must inherit from", outcome.Message);
    }

    [Fact]
    public void MustInheritFrom_MissingBaseType_ReportsNoneAsActual()
    {
        var type = TestModels.Type("Contoso.Domain.Entities.LegacyThing");

        var outcome = new MustInheritFromAssertion("Contoso.Domain.Entity<*>").Evaluate(type, EmptyModel);

        Assert.Equal("(none)", outcome.Actual);
    }

    [Fact]
    public void Success_HasNoStructuredEvidence()
    {
        var type = TestModels.Type("Contoso.Domain.Entities.Order", baseType: "Contoso.Domain.Entity<int>");

        var outcome = new MustInheritFromAssertion("Contoso.Domain.Entity<*>").Evaluate(type, EmptyModel);

        Assert.True(outcome.Passed);
        Assert.Null(outcome.Expected);
        Assert.Null(outcome.Actual);
        Assert.Null(outcome.Evidence);
    }

    [Fact]
    public void MustNotDependOn_Failure_CarriesOffendingTypesAsEvidence()
    {
        var offender = TestModels.Type("Contoso.Domain.Order", interfaces: ["Forbidden.Infra.IThing"]);
        var project = TestModels.Project("Contoso.Domain", types: [offender]);

        var outcome = new MustNotDependOnAssertion("Forbidden.Infra.*").Evaluate(project, EmptyModel);

        Assert.False(outcome.Passed);
        Assert.Equal("no dependency on 'Forbidden.Infra.*'", outcome.Expected);
        Assert.Equal("Forbidden.Infra.IThing", outcome.Actual);
        Assert.Equal(["Contoso.Domain.Order"], outcome.Evidence);
    }

    [Fact]
    public void MustOnlyDependOn_Failure_CarriesDisallowedTypesAsEvidence()
    {
        var offender = TestModels.Type("Contoso.Domain.Order", interfaces: ["Forbidden.Infra.IThing"]);
        var project = TestModels.Project("Contoso.Domain", types: [offender]);

        var outcome = new MustOnlyDependOnAssertion(["System.*", "Contoso.*"]).Evaluate(project, EmptyModel);

        Assert.False(outcome.Passed);
        Assert.Equal("System.*, Contoso.*", outcome.Expected);
        Assert.Equal(["Forbidden.Infra.IThing"], outcome.Evidence);
    }

    [Fact]
    public void MustMatchName_Failure_CarriesRegexAsExpected_NameAsActual()
    {
        var type = TestModels.Type("Contoso.Domain.OrderService");

        var outcome = new MustMatchNameAssertion("^.+Repository$").Evaluate(type, EmptyModel);

        Assert.False(outcome.Passed);
        Assert.Equal("^.+Repository$", outcome.Expected);
        Assert.Equal("OrderService", outcome.Actual);
    }

    [Fact]
    public void MustMatchNamespacePattern_Failure_CarriesNamespaceAsActual()
    {
        var type = TestModels.Type("Contoso.Domain.Orders.Order");

        var outcome = new MustMatchNamespacePatternAssertion(".*\\.Events$").Evaluate(type, EmptyModel);

        Assert.False(outcome.Passed);
        Assert.Equal(".*\\.Events$", outcome.Expected);
        Assert.Equal("Contoso.Domain.Orders", outcome.Actual);
    }

    [Fact]
    public void MustHaveCount_Failure_CarriesExpectedCountAndMatchesAsEvidence()
    {
        var matches = new object[] { TestModels.Type("A.One"), TestModels.Type("A.Two") };
        var assertion = new MustHaveCountAssertion(Template, _ => new StubSelector(matches), min: null, max: null, exactly: 0);

        var outcome = assertion.Evaluate(TestModels.Type("Outer"), EmptyModel);

        Assert.False(outcome.Passed);
        Assert.Equal("exactly 0", outcome.Expected);
        Assert.Equal("2", outcome.Actual);
        Assert.Equal(["A.One", "A.Two"], outcome.Evidence);
    }

    [Fact]
    public void MustExist_Failure_CarriesExpectedAndActualCounts()
    {
        var outcome = new MustExistAssertion(Template, _ => new StubSelector([])).Evaluate(TestModels.Type("Outer"), EmptyModel);

        Assert.False(outcome.Passed);
        Assert.Equal("at least one match", outcome.Expected);
        Assert.Equal("0 matches", outcome.Actual);
    }

    [Fact]
    public void MustAllMatch_Failure_AggregatesNestedEvidenceQualifiedByMatch()
    {
        var nested = new StubAssertion(candidate =>
        {
            var name = ((TypeModel)candidate).Name;
            return name == "Bad"
                ? AssertionOutcome.Failure("nested failed", "suffix Handler", name, [$"{name}.Extra"])
                : AssertionOutcome.Success();
        });
        var matches = new object[] { TestModels.Type("A.Good"), TestModels.Type("A.Bad") };
        var assertion = new MustAllMatchAssertion(Template, _ => new StubSelector(matches), [nested]);

        var outcome = assertion.Evaluate(TestModels.Type("Outer"), EmptyModel);

        Assert.False(outcome.Passed);
        Assert.Equal("suffix Handler", outcome.Expected);
        Assert.Equal("1 match(es) failed a nested assertion", outcome.Actual);
        Assert.Equal(["A.Bad: Bad.Extra"], outcome.Evidence);
    }

    [Fact]
    public void MustAllMatch_Failure_LeavesExpectedNullWhenNestedFailuresDisagree()
    {
        var nested = new StubAssertion(candidate =>
            AssertionOutcome.Failure("nested failed", $"expected for {((TypeModel)candidate).Name}", null, null));
        var matches = new object[] { TestModels.Type("A.One"), TestModels.Type("A.Two") };
        var assertion = new MustAllMatchAssertion(Template, _ => new StubSelector(matches), [nested]);

        var outcome = assertion.Evaluate(TestModels.Type("Outer"), EmptyModel);

        Assert.False(outcome.Passed);
        Assert.Null(outcome.Expected);
        Assert.Equal(["A.One", "A.Two"], outcome.Evidence);
    }

    [Fact]
    public void MustAnyMatch_Failure_CarriesEvaluatedMatchesAsEvidence()
    {
        var failing = new StubAssertion(_ => AssertionOutcome.Failure("nope"));
        var matches = new object[] { TestModels.Type("A.One"), TestModels.Type("A.Two") };
        var assertion = new MustAnyMatchAssertion(Template, _ => new StubSelector(matches), [failing]);

        var outcome = assertion.Evaluate(TestModels.Type("Outer"), EmptyModel);

        Assert.False(outcome.Passed);
        Assert.Equal("0 of 2 match(es) satisfied every nested assertion", outcome.Actual);
        Assert.Equal(["A.One", "A.Two"], outcome.Evidence);
    }

    [Fact]
    public void MustNoneMatch_Failure_CarriesOffendingMatchesAsEvidence()
    {
        var passing = new StubAssertion(_ => AssertionOutcome.Success());
        var matches = new object[] { TestModels.Type("A.One") };
        var assertion = new MustNoneMatchAssertion(Template, _ => new StubSelector(matches), [passing]);

        var outcome = assertion.Evaluate(TestModels.Type("Outer"), EmptyModel);

        Assert.False(outcome.Passed);
        Assert.Equal("1 offending match(es)", outcome.Actual);
        Assert.Equal(["A.One"], outcome.Evidence);
    }
}
