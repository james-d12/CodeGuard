using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Selectors;

namespace RulesEngine.Evaluation.Tests.Selectors;

public class RecordSelectorTests
{
    [Fact]
    public void SelectCandidates_ReturnsOnlyRecordsMatchingNamespacePattern()
    {
        var matchingRecord = TestModels.Type("Contoso.Domain.Events.OrderPlaced", kind: TypeKind.Record);
        var wrongNamespace = TestModels.Type("Contoso.Application.Events.OrderPlaced", kind: TypeKind.Record);
        var wrongKind = TestModels.Type("Contoso.Domain.Events.OrderPlacedHandler", kind: TypeKind.Class);

        var project = TestModels.Project("Contoso.Domain", types: [matchingRecord, wrongNamespace, wrongKind]);
        var model = TestModels.Repository(project);

        var candidates = new RecordSelector("*.Domain.Events").SelectCandidates(model).Cast<TypeModel>().ToList();

        var record = Assert.Single(candidates);
        Assert.Equal("Contoso.Domain.Events.OrderPlaced", record.FullName);
    }

    [Fact]
    public void SelectCandidates_DefaultsToAllNamespaces()
    {
        var record = TestModels.Type("Contoso.Domain.Events.OrderPlaced", kind: TypeKind.Record);
        var project = TestModels.Project("Contoso.Domain", types: [record]);
        var model = TestModels.Repository(project);

        var candidates = new RecordSelector().SelectCandidates(model).ToList();

        Assert.Single(candidates);
    }
}
