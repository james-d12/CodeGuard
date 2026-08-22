using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Analyzers;

namespace CodeGuard.Evaluation.Tests.Analyzers;

public class ImmutableMutationAnalyzerTests
{
    private static MutationSiteModel Mutation(string containingType, string containingMethod) => new(
        ContainingMethod: containingMethod,
        ContainingType: containingType,
        TargetMemberName: "Amount",
        ProjectName: "Contoso.Domain",
        FilePath: "Money.cs",
        Line: 15);

    [Fact]
    public void Analyze_Flags_MutationOfRecordOutsideConstructor()
    {
        var recordType = TestModels.Type("Contoso.Domain.Money", kind: TypeKind.Record);
        var project = TestModels.Project("Contoso.Domain", types: [recordType]);
        var model = TestModels.RepositoryWithFacts(
            projects: [project],
            mutationSites: [Mutation("Contoso.Domain.Money", "Recalculate")]);
        var analyzer = new ImmutableMutationAnalyzer("*");

        var violations = analyzer.Analyze(model).ToList();

        var violation = Assert.Single(violations);
        Assert.Equal("Money.cs", violation.FilePath);
        Assert.Contains("Contoso.Domain.Money.Amount", violation.Message);
    }

    [Fact]
    public void Analyze_DoesNotFlag_MutationOfRecordInsideConstructor()
    {
        var recordType = TestModels.Type("Contoso.Domain.Money", kind: TypeKind.Record);
        var project = TestModels.Project("Contoso.Domain", types: [recordType]);
        var model = TestModels.RepositoryWithFacts(
            projects: [project],
            mutationSites: [Mutation("Contoso.Domain.Money", ".ctor")]);
        var analyzer = new ImmutableMutationAnalyzer("*");

        var violations = analyzer.Analyze(model).ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Analyze_DoesNotFlag_MutationOfNonRecordType()
    {
        var classType = TestModels.Type("Contoso.Domain.OrderBuilder", kind: TypeKind.Class);
        var project = TestModels.Project("Contoso.Domain", types: [classType]);
        var model = TestModels.RepositoryWithFacts(
            projects: [project],
            mutationSites: [Mutation("Contoso.Domain.OrderBuilder", "Build")]);
        var analyzer = new ImmutableMutationAnalyzer("*");

        var violations = analyzer.Analyze(model).ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Analyze_DoesNotFlag_RecordOutsideConfiguredNamespace()
    {
        var recordType = TestModels.Type("Contoso.Reporting.Money", kind: TypeKind.Record, projectName: "Contoso.Reporting");
        var project = TestModels.Project("Contoso.Reporting", types: [recordType]);
        var model = TestModels.RepositoryWithFacts(
            projects: [project],
            mutationSites: [Mutation("Contoso.Reporting.Money", "Recalculate")]);
        var analyzer = new ImmutableMutationAnalyzer("Contoso.Domain.*");

        var violations = analyzer.Analyze(model).ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Name_IsImmutableMutation()
    {
        var analyzer = new ImmutableMutationAnalyzer("*");

        Assert.Equal("immutable-mutation", analyzer.Name);
    }

    [Fact]
    public void Analyze_AttributesCorrectly_WhenTwoProjectsShareATypeFullName()
    {
        // Regression test: this analyzer built its record-name set from bare FullName alone, so a
        // record type in one project and an identically-named non-record (or out-of-pattern) type
        // in another project could collide - not a crash (HashSet tolerates duplicates), but a
        // silent misattribution, since Contains() ignored which project a mutation site belonged
        // to. It's now keyed by (ProjectName, FullName).
        const string sharedFullName = "Contoso.Domain.Money";
        var recordType = TestModels.Type(sharedFullName, kind: TypeKind.Record, projectName: "Contoso.Domain");
        var classType = TestModels.Type(sharedFullName, kind: TypeKind.Class, projectName: "Contoso.Other");
        var model = TestModels.RepositoryWithFacts(
            projects: [TestModels.Project("Contoso.Domain", types: [recordType]), TestModels.Project("Contoso.Other", types: [classType])],
            mutationSites:
            [
                new MutationSiteModel(ContainingMethod: "Recalculate", ContainingType: sharedFullName, TargetMemberName: "Amount", ProjectName: "Contoso.Domain", FilePath: "Domain/Money.cs", Line: 10),
                new MutationSiteModel(ContainingMethod: "Recalculate", ContainingType: sharedFullName, TargetMemberName: "Amount", ProjectName: "Contoso.Other", FilePath: "Other/Money.cs", Line: 20)
            ]);
        var analyzer = new ImmutableMutationAnalyzer("*");

        var violations = analyzer.Analyze(model).ToList();

        var violation = Assert.Single(violations);
        Assert.Equal("Domain/Money.cs", violation.FilePath);
    }
}
