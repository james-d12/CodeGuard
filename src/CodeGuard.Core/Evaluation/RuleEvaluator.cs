using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Core.Results;
using CodeGuard.RuleModel.Assertions;
using CodeGuard.RuleModel.Rules;

namespace CodeGuard.Core.Evaluation;

public interface IRuleEvaluator
{
    ValidationResult Evaluate(IReadOnlyList<RuleDefinition> rules, RepositoryModel model);
}

public sealed class RuleEvaluator : IRuleEvaluator
{
    public ValidationResult Evaluate(IReadOnlyList<RuleDefinition> rules, RepositoryModel model)
    {
        var violations = new List<Violation>();
        var evaluationErrors = new List<RuleEvaluationError>();
        var rulesPassed = 0;
        var rulesFailed = 0;
        var rulesErrored = 0;
        var rulesEvaluated = 0;

        foreach (var rule in rules.Where(r => r.Enabled))
        {
            rulesEvaluated++;

            try
            {
                var ruleViolations = EvaluateRule(rule, model);
                if (ruleViolations.Count > 0)
                {
                    rulesFailed++;
                }
                else
                {
                    rulesPassed++;
                }

                violations.AddRange(ruleViolations);
            }
            catch (Exception ex)
            {
                // Isolation boundary: selectors/assertions/analyzers are user-authored (via YAML `kind`
                // lookups), so one rule's bug must not abort every other rule's evaluation. Any partial
                // violations already found for this rule are discarded - the rule couldn't be fully
                // evaluated, so its result is unreliable.
                rulesErrored++;
                evaluationErrors.Add(new RuleEvaluationError(
                    rule.Id, ex.GetType().FullName ?? ex.GetType().Name, ex.Message, ex.StackTrace));
            }
        }

        var status = evaluationErrors.Count > 0
            ? ValidationStatus.PartiallyEvaluated
            : violations.Count == 0 ? ValidationStatus.Passed : ValidationStatus.Failed;

        return new ValidationResult(
            status, rulesEvaluated, rulesPassed, rulesFailed, rulesErrored, violations, evaluationErrors, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Evaluates a single rule against <paramref name="model"/>, ignoring <see cref="RuleDefinition.Enabled"/> -
    /// a caller asking to evaluate a specific rule (e.g. `rules test`) wants that rule evaluated regardless of
    /// whether it's enabled for whole-repo <see cref="Evaluate"/> runs. Does not catch exceptions; callers that
    /// need per-rule isolation (like <see cref="Evaluate"/>) handle that themselves.
    /// </summary>
    public IReadOnlyList<Violation> EvaluateRule(RuleDefinition rule, RepositoryModel model)
    {
        var violations = new List<Violation>();

        if (rule.Analyzer is not null)
        {
            EvaluateAnalyzerRule(rule, model, violations);
        }
        else
        {
            EvaluateSelectorRule(rule, model, violations);
        }

        return violations;
    }

    private static void EvaluateAnalyzerRule(RuleDefinition rule, RepositoryModel model, List<Violation> violations)
    {
        foreach (var analyzerViolation in rule.Analyzer!.Analyze(model))
        {
            violations.Add(new Violation(
                rule.Id,
                rule.Severity,
                analyzerViolation.Message,
                analyzerViolation.FilePath,
                analyzerViolation.Line,
                analyzerViolation.Column,
                analyzerViolation.Symbol,
                analyzerViolation.ProjectName,
                rule.Remediation,
                rule.Documentation));
        }
    }

    private static void EvaluateSelectorRule(RuleDefinition rule, RepositoryModel model, List<Violation> violations)
    {
        IEnumerable<object> candidates = rule.Target!.SelectCandidates(model);
        if (rule.When is not null)
        {
            candidates = candidates.Where(candidate => rule.When.Evaluate(candidate, model));
        }

        foreach (var candidate in candidates)
        {
            foreach (var assertion in rule.Assertions!)
            {
                var outcome = assertion.Evaluate(candidate, model);
                if (outcome.Passed)
                {
                    continue;
                }

                violations.Add(CreateViolation(rule, candidate, outcome));
            }
        }
    }

    private static Violation CreateViolation(RuleDefinition rule, object candidate, AssertionOutcome outcome)
    {
        var (file, line, column, symbol, project) = ExtractLocation(candidate);

        return new Violation(
            rule.Id,
            rule.Severity,
            outcome.Message ?? $"Rule '{rule.Id}' failed.",
            file,
            line,
            column,
            symbol,
            project,
            rule.Remediation,
            rule.Documentation);
    }

    private static (string? File, int? Line, int? Column, string? Symbol, string? Project) ExtractLocation(object candidate) =>
        candidate switch
        {
            TypeModel type => (type.FilePath, type.Line, type.Column, type.FullName, type.ProjectName),
            ProjectModel project => (project.Path, null, null, project.Name, project.Name),
            FileModel file => (file.Path, null, null, file.RelativePath, null),
            RepositoryModel repository => (repository.RootPath, null, null, null, null),
            MethodModel method => (method.FilePath, method.Line, method.Column, $"{method.DeclaringType}.{method.Name}", method.ProjectName),
            PropertyModel property => (property.FilePath, property.Line, property.Column, $"{property.DeclaringType}.{property.Name}", property.ProjectName),
            ConstructorModel constructor => (constructor.FilePath, constructor.Line, constructor.Column, constructor.DeclaringType, constructor.ProjectName),
            FieldModel field => (field.FilePath, field.Line, field.Column, $"{field.DeclaringType}.{field.Name}", field.ProjectName),
            CallSiteModel callSite => (callSite.FilePath, callSite.Line, callSite.Column, callSite.InvokedMember, callSite.ProjectName),
            _ => (null, null, null, null, null)
        };
}
