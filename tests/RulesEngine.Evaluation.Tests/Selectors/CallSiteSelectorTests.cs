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
        string projectName = "Contoso.Domain",
        IReadOnlyList<CallSiteArgument>? arguments = null,
        string? enclosingComparisonOperator = null) =>
        new(kind, invokedMember, targetType, containingMethod, containingType, projectName, arguments ?? [], "Logger.cs", 1, 1, enclosingComparisonOperator);

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

    [Fact]
    public void SelectCandidates_ReturnsEmpty_WhenRepositoryHasNoCallSites()
    {
        var model = BuildModel();

        var candidates = new CallSiteSelector().SelectCandidates(model).ToList();

        Assert.Empty(candidates);
    }

    [Fact]
    public void SelectCandidates_MatchesNullTargetType_WhenTargetTypePatternIsBareWildcard()
    {
        var model = BuildModel(CallSite(targetType: null));

        var candidates = new CallSiteSelector(targetTypePattern: "*").SelectCandidates(model).ToList();

        Assert.Single(candidates);
    }

    [Fact]
    public void SelectCandidates_ExcludesNullTargetType_WhenTargetTypePatternIsNotBareWildcard()
    {
        var model = BuildModel(CallSite(targetType: null));

        var candidates = new CallSiteSelector(targetTypePattern: "System.Console").SelectCandidates(model).ToList();

        Assert.Empty(candidates);
    }

    [Fact]
    public void SelectCandidates_MatchesNonNullTargetType_AgainstActualValue_NotAnEmptyString()
    {
        var model = BuildModel(CallSite(targetType: "System.Console"));

        var candidates = new CallSiteSelector(targetTypePattern: "System.Console").SelectCandidates(model).ToList();

        Assert.Single(candidates);
    }

    [Fact]
    public void SelectCandidates_SkipsArgumentLiteralnessFilter_WhenOnlyArgumentIndexIsSet()
    {
        var model = BuildModel(CallSite(arguments: [new CallSiteArgument(0, null, false)]));

        var candidates = new CallSiteSelector(argumentIndex: 0).SelectCandidates(model).ToList();

        Assert.Single(candidates);
    }

    [Fact]
    public void SelectCandidates_MatchesArgumentLiteralness_WhenArgumentAtIndexIsLiteral()
    {
        var model = BuildModel(CallSite(arguments: [new CallSiteArgument(0, "42", true)]));

        var candidates = new CallSiteSelector(argumentIndex: 0, argumentIsLiteral: true).SelectCandidates(model).ToList();

        Assert.Single(candidates);
    }

    [Fact]
    public void SelectCandidates_ExcludesArgumentLiteralness_WhenArgumentAtIndexIsNotLiteral()
    {
        var model = BuildModel(CallSite(arguments: [new CallSiteArgument(0, null, false)]));

        var candidates = new CallSiteSelector(argumentIndex: 0, argumentIsLiteral: true).SelectCandidates(model).ToList();

        Assert.Empty(candidates);
    }

    [Fact]
    public void SelectCandidates_ExcludesArgumentLiteralness_WhenArgumentAtIndexNotFound()
    {
        var model = BuildModel(CallSite(arguments: [new CallSiteArgument(1, "42", true)]));

        var candidates = new CallSiteSelector(argumentIndex: 0, argumentIsLiteral: true).SelectCandidates(model).ToList();

        Assert.Empty(candidates);
    }

    [Fact]
    public void SelectCandidates_MatchesAnyArgument_WhenArgumentIndexAndIsLiteralAreNull()
    {
        var model = BuildModel(CallSite(arguments: []));

        var candidates = new CallSiteSelector().SelectCandidates(model).ToList();

        Assert.Single(candidates);
    }

    [Fact]
    public void SelectCandidates_FiltersByEnclosingComparisonOperator()
    {
        var model = BuildModel(
            CallSite(enclosingComparisonOperator: "=="),
            CallSite(enclosingComparisonOperator: "!="));

        var candidates = new CallSiteSelector(enclosingComparisonOperator: "==").SelectCandidates(model).Cast<CallSiteModel>().ToList();

        var callSite = Assert.Single(candidates);
        Assert.Equal("==", callSite.EnclosingComparisonOperator);
    }

    [Fact]
    public void SelectCandidates_MatchesAnyComparisonOperator_WhenFilterIsNull()
    {
        var model = BuildModel(CallSite(enclosingComparisonOperator: "=="), CallSite(enclosingComparisonOperator: null));

        var candidates = new CallSiteSelector().SelectCandidates(model).ToList();

        Assert.Equal(2, candidates.Count);
    }

    [Fact]
    public void SelectCandidates_CombinesMultipleFilters_RequiringAllToMatch()
    {
        var model = BuildModel(
            CallSite(kind: CallSiteKind.ObjectCreation, containingType: "Contoso.Domain.Order"),
            CallSite(kind: CallSiteKind.Invocation, containingType: "Contoso.Domain.Order"),
            CallSite(kind: CallSiteKind.ObjectCreation, containingType: "Contoso.Application.Handler"));

        var candidates = new CallSiteSelector(siteKind: CallSiteKind.ObjectCreation, containingTypePattern: "Contoso.Domain.*")
            .SelectCandidates(model).Cast<CallSiteModel>().ToList();

        var callSite = Assert.Single(candidates);
        Assert.Equal(CallSiteKind.ObjectCreation, callSite.Kind);
        Assert.Equal("Contoso.Domain.Order", callSite.ContainingType);
    }
}
