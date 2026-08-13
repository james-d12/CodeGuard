using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Assertions;

namespace CodeGuard.Evaluation.Tests.Assertions;

public class MustUsePackageVersionAssertionTests
{
    [Fact]
    public void Evaluate_Passes_WhenVersionMeetsAtLeastConstraint()
    {
        var project = TestModels.Project("Contoso.Domain", packageReferences: [new PackageReferenceModel("Contoso.Sdk", "8.1.0")]);

        var outcome = new MustUsePackageVersionAssertion("Contoso.Sdk", ">=8.0.0").Evaluate(project, TestModels.Repository());

        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenVersionBelowAtLeastConstraint()
    {
        var project = TestModels.Project("Contoso.Domain", packageReferences: [new PackageReferenceModel("Contoso.Sdk", "7.9.0")]);

        var outcome = new MustUsePackageVersionAssertion("Contoso.Sdk", ">=8.0.0").Evaluate(project, TestModels.Repository());

        Assert.False(outcome.Passed);
        Assert.Contains("Contoso.Sdk 7.9.0", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_WhenVersionAboveAtMostConstraint()
    {
        var project = TestModels.Project("Contoso.Domain", packageReferences: [new PackageReferenceModel("Contoso.Sdk", "9.0.0")]);

        var outcome = new MustUsePackageVersionAssertion("Contoso.Sdk", "<=8.0.0").Evaluate(project, TestModels.Repository());

        Assert.False(outcome.Passed);
    }

    [Fact]
    public void Evaluate_TreatsBareVersion_AsExactEquality()
    {
        var project = TestModels.Project("Contoso.Domain", packageReferences: [new PackageReferenceModel("Contoso.Sdk", "8.0.0")]);

        var outcome = new MustUsePackageVersionAssertion("Contoso.Sdk", "8.0.0").Evaluate(project, TestModels.Repository());

        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_StripsPrereleaseSuffix_BeforeComparing()
    {
        var project = TestModels.Project("Contoso.Domain", packageReferences: [new PackageReferenceModel("Contoso.Sdk", "8.0.0-rc.1")]);

        var outcome = new MustUsePackageVersionAssertion("Contoso.Sdk", ">=8.0.0").Evaluate(project, TestModels.Repository());

        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenPackageIsNotReferenced()
    {
        var project = TestModels.Project("Contoso.Domain");

        var outcome = new MustUsePackageVersionAssertion("Contoso.Sdk", ">=8.0.0").Evaluate(project, TestModels.Repository());

        Assert.False(outcome.Passed);
        Assert.Equal("Project 'Contoso.Domain' does not reference a package matching 'Contoso.Sdk'.", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustUsePackageVersionAssertion("Contoso.Sdk", ">=8.0.0").Evaluate(TestModels.Type("Contoso.Domain.Order"), TestModels.Repository());

        Assert.False(outcome.Passed);
        Assert.Equal("'must_use_package_version' can only be evaluated against projects.", outcome.Message);
    }
}
