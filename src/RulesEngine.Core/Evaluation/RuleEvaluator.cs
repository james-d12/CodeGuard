using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Core.Results;
using RulesEngine.RuleModel.Assertions;
using RulesEngine.RuleModel.Rules;

namespace RulesEngine.Core.Evaluation;

public interface IRuleEvaluator
{
    ValidationResult Evaluate(IReadOnlyList<RuleDefinition> rules, RepositoryModel model);
}

public sealed class RuleEvaluator : IRuleEvaluator
{
    public ValidationResult Evaluate(IReadOnlyList<RuleDefinition> rules, RepositoryModel model)
    {
        var violations = new List<Violation>();
        var rulesPassed = 0;
        var rulesFailed = 0;
        var rulesEvaluated = 0;

        foreach (var rule in rules.Where(r => r.Enabled))
        {
            rulesEvaluated++;

            IEnumerable<object> candidates = rule.Target.SelectCandidates(model);
            if (rule.When is not null)
            {
                candidates = candidates.Where(candidate => rule.When.Evaluate(candidate, model));
            }

            var ruleFailed = false;
            foreach (var candidate in candidates)
            {
                foreach (var assertion in rule.Assertions)
                {
                    var outcome = assertion.Evaluate(candidate, model);
                    if (outcome.Passed)
                    {
                        continue;
                    }

                    ruleFailed = true;
                    violations.Add(CreateViolation(rule, candidate, outcome));
                }
            }

            if (ruleFailed)
            {
                rulesFailed++;
            }
            else
            {
                rulesPassed++;
            }
        }

        var status = violations.Count == 0 ? ValidationStatus.Passed : ValidationStatus.Failed;
        return new ValidationResult(status, rulesEvaluated, rulesPassed, rulesFailed, violations, DateTimeOffset.UtcNow);
    }

    private static Violation CreateViolation(RuleDefinition rule, object candidate, AssertionOutcome outcome)
    {
        var (file, line, column, symbol, project) = ExtractLocation(candidate);

        return new Violation(
            rule.Id,
            rule.Standard,
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
            _ => (null, null, null, null, null)
        };
}
