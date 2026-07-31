using CodeGuard.Core.Results;

namespace CodeGuard.Reporting;

public interface IViolationReporter
{
    string Format { get; }

    Task WriteAsync(ValidationResult result, TextWriter writer, CancellationToken ct = default);
}
