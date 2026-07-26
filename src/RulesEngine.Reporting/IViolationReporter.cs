using RulesEngine.Core.Results;

namespace RulesEngine.Reporting;

public interface IViolationReporter
{
    string Format { get; }

    Task WriteAsync(ValidationResult result, TextWriter writer, CancellationToken ct = default);
}
