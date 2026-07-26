using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Assertions;

namespace RulesEngine.Evaluation.Tests.Assertions;

public class PackageReferenceAssertionTests
{
    private static readonly RepositoryModel EmptyModel = new(".", [], []);

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
    }
}
