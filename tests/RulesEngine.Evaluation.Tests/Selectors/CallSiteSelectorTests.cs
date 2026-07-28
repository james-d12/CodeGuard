using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Selectors;

namespace RulesEngine.Evaluation.Tests.Selectors;

public class CallSiteSelectorTests
{
    private static CallSiteModel CallSite(
        CallSiteKind kind = CallSiteKind.Invocation,
        string invokedMember = "System.Console.WriteLine",
        string? targetType = "System.Console",
        string containingMethod = "Log",
        string containingType = "Contoso.Domain.Logger",
        string projectName = "Contoso.Domain") =>
        new(kind, invokedMember, targetType, containingMethod, containingType, projectName, [], "Logger.cs", 1, 1);

    private static RepositoryModel BuildModel(params CallSiteModel[] callSites) =>
        new(".", [], [], callSites, [], [], [], [], [], []);

    [Fact]
    public void SelectCandidates_FiltersBySiteKind()
    {
        var model = BuildModel(
            CallSite(kind: CallSiteKind.Invocation),
            CallSite(kind: CallSiteKind.ObjectCreation));

        var candidates = new CallSiteSelector(siteKind: CallSiteKind.ObjectCreation).SelectCandidates(model).Cast<CallSiteModel>().ToList();

        var callSite = Assert.Single(candidates);
        Assert.Equal(CallSiteKind.ObjectCreation, callSite.Kind);
    }

    [Fact]
    public void SelectCandidates_FiltersByInvokedMemberPattern()
    {
        var model = BuildModel(
            CallSite(invokedMember: "System.Console.WriteLine"),
            CallSite(invokedMember: "System.Guid.NewGuid"));

        var candidates = new CallSiteSelector(invokedMemberPattern: "*.WriteLine").SelectCandidates(model).Cast<CallSiteModel>().ToList();

        var callSite = Assert.Single(candidates);
        Assert.Equal("System.Console.WriteLine", callSite.InvokedMember);
    }

    [Fact]
    public void SelectCandidates_FiltersByContainingType()
    {
        var model = BuildModel(
            CallSite(containingType: "Contoso.Domain.Order"),
            CallSite(containingType: "Contoso.Application.Handler"));

        var candidates = new CallSiteSelector(containingTypePattern: "Contoso.Domain.*").SelectCandidates(model).Cast<CallSiteModel>().ToList();

        var callSite = Assert.Single(candidates);
        Assert.Equal("Contoso.Domain.Order", callSite.ContainingType);
    }
}
