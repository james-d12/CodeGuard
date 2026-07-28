using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Analyzers;

namespace RulesEngine.Evaluation.Tests.Analyzers;

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
}
