using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Assertions;

namespace CodeGuard.Evaluation.Tests.Assertions;

public class PackageReferenceAssertionTests
{
    private static readonly RepositoryModel EmptyModel = new(".", [], [], [], [], [], [], [], [], []);

    [Fact]
    public void MustReferencePackage_Passes_WhenPackagePresent()
    {
        var project = TestModels.Project("Contoso.Infrastructure",
            packageReferences: [new PackageReferenceModel("Microsoft.EntityFrameworkCore", "8.0.0")]);

        var outcome = new MustReferencePackageAssertion("Microsoft.EntityFrameworkCore").Evaluate(project, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void MustReferencePackage_Fails_WhenPackageMissing()
    {
        var project = TestModels.Project("Contoso.Infrastructure");
        var outcome = new MustReferencePackageAssertion("Microsoft.EntityFrameworkCore").Evaluate(project, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal("Project 'Contoso.Infrastructure' must reference package matching 'Microsoft.EntityFrameworkCore'.", outcome.Message);
    }

    [Fact]
    public void MustNotReferencePackage_Passes_WhenPackageAbsent()
    {
        var project = TestModels.Project("Contoso.Domain");
        var outcome = new MustNotReferencePackageAssertion("Microsoft.EntityFrameworkCore").Evaluate(project, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void MustNotReferencePackage_Fails_WhenPackagePresent()
    {
        var project = TestModels.Project("Contoso.Domain",
            packageReferences: [new PackageReferenceModel("Microsoft.EntityFrameworkCore", "8.0.0")]);

        var outcome = new MustNotReferencePackageAssertion("Microsoft.EntityFrameworkCore").Evaluate(project, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal("Project 'Contoso.Domain' must not reference package 'Microsoft.EntityFrameworkCore'.", outcome.Message);
    }

    [Fact]
    public void MustReferencePackage_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustReferencePackageAssertion("Microsoft.EntityFrameworkCore").Evaluate(42, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal("'must_reference_package' can only be evaluated against projects.", outcome.Message);
    }

    [Fact]
    public void MustNotReferencePackage_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustNotReferencePackageAssertion("Microsoft.EntityFrameworkCore").Evaluate(42, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal("'must_not_reference_package' can only be evaluated against projects.", outcome.Message);
    }
}
