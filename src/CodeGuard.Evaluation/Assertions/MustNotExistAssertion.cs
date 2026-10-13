using System.Text.Json.Nodes;
using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Assertions;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Evaluation.Assertions;

public sealed class MustNotExistAssertion(JsonObject selectorTemplate, Func<JsonObject, ITargetSelector> selectorFactory) : IAssertion
{
    public string Kind => "must_not_exist";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        var selector = selectorFactory(SelectorTemplateResolver.Resolve(selectorTemplate, candidate));

        // Only a bounded sample is materialised: success needs just "is there any", and failure evidence
        // is capped, so the enumeration stops after one item more than the cap.
        var sample = selector.SelectCandidates(model)
            .Take(CandidateDescriptor.MaxEvidenceEntries + 1)
            .Select(CandidateDescriptor.Describe)
            .ToList();

        if (sample.Count == 0)
        {
            return AssertionOutcome.Success();
        }

        // The total is not known without enumerating everything, so the tail says "more" rather than a count.
        var truncated = sample.Count > CandidateDescriptor.MaxEvidenceEntries;
        var evidence = CandidateDescriptor.CapEvidence(sample.Take(CandidateDescriptor.MaxEvidenceEntries));
        if (truncated)
        {
            evidence.Add("...and more");
        }

        return AssertionOutcome.Failure(
            $"Expected no matches for a '{selector.Kind}' selector, but found at least one.",
            "no matches",
            truncated ? $"more than {CandidateDescriptor.MaxEvidenceEntries} match(es)" : $"{sample.Count} match(es)",
            evidence);
    }
}
