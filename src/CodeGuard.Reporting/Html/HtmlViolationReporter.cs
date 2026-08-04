using System.Text;
using System.Text.Encodings.Web;
using CodeGuard.Core.Results;
using CodeGuard.RuleModel.Rules;

namespace CodeGuard.Reporting.Html;

public sealed class HtmlViolationReporter : IViolationReporter
{
    public string Format => "html";

    public async Task WriteAsync(ValidationResult result, TextWriter writer, CancellationToken ct = default)
    {
        var html = Build(result);
        await writer.WriteLineAsync(html.AsMemory(), ct);
    }

    private static string Build(ValidationResult result)
    {
        var violations = result.Violations.OrderByDescending(v => v.Severity).ToList();
        var sb = new StringBuilder();

        sb.Append("<!DOCTYPE html>\n");
        sb.Append("<html lang=\"en\">\n<head>\n<meta charset=\"utf-8\">\n");
        sb.Append("<title>CodeGuard Validation Report</title>\n");
        sb.Append("<style>\n").Append(Css).Append("\n</style>\n");
        sb.Append("</head>\n<body>\n");

        AppendHeader(sb, result, violations);
        AppendEvaluationErrors(sb, result.EvaluationErrors);
        AppendFilters(sb);
        AppendTable(sb, violations);

        sb.Append("<script>\n").Append(Js).Append("\n</script>\n");
        sb.Append("</body>\n</html>\n");

        return sb.ToString();
    }

    private static void AppendHeader(StringBuilder sb, ValidationResult result, IReadOnlyList<Violation> violations)
    {
        var statusClass = result.Status == ValidationStatus.Passed ? "status-passed" : "status-failed";

        sb.Append("<header>\n");
        sb.Append("<h1>Validation Report</h1>\n");
        sb.Append($"<div class=\"status {statusClass}\">Status: {Esc(result.Status.ToString())}</div>\n");
        sb.Append("<div class=\"summary\">");
        sb.Append(
            $"Rules evaluated: {result.RulesEvaluated}, passed: {result.RulesPassed}, " +
            $"failed: {result.RulesFailed}, errored: {result.RulesErrored}");
        sb.Append($" &mdash; generated {Esc(result.EvaluatedAtUtc.ToString("O"))}");
        sb.Append("</div>\n");

        sb.Append("<div class=\"severity-counts\">\n");
        foreach (var severity in new[] { Severity.Info, Severity.Warning, Severity.Error, Severity.Critical })
        {
            var count = violations.Count(v => v.Severity == severity);
            sb.Append($"<span class=\"badge badge-{severity.ToString().ToLowerInvariant()}\">{severity}: {count}</span>\n");
        }

        sb.Append("</div>\n");
        sb.Append("</header>\n");
    }

    private static void AppendEvaluationErrors(StringBuilder sb, IReadOnlyList<RuleEvaluationError> errors)
    {
        if (errors.Count == 0)
        {
            return;
        }

        sb.Append("<section class=\"evaluation-errors\">\n");
        sb.Append("<h2>Rules that could not be evaluated</h2>\n<ul>\n");
        foreach (var error in errors.OrderBy(e => e.RuleId, StringComparer.Ordinal))
        {
            sb.Append("<li>");
            sb.Append($"<strong>{Esc(error.RuleId)}</strong>: {Esc(error.ExceptionType)}: {Esc(error.Message)}");
            sb.Append("</li>\n");
        }

        sb.Append("</ul>\n</section>\n");
    }

    private static void AppendFilters(StringBuilder sb)
    {
        sb.Append("<section class=\"filters\">\n");
        foreach (var severity in new[] { Severity.Info, Severity.Warning, Severity.Error, Severity.Critical })
        {
            sb.Append(
                $"<label><input type=\"checkbox\" class=\"sev-toggle\" value=\"{severity}\" checked> {severity}</label>\n");
        }

        sb.Append("<input type=\"text\" id=\"rule-id-filter\" placeholder=\"Filter by rule ID...\">\n");
        sb.Append("<select id=\"project-filter\"><option value=\"\">All projects</option></select>\n");
        sb.Append("<input type=\"text\" id=\"message-search\" placeholder=\"Search messages...\">\n");
        sb.Append("<span id=\"visible-count\"></span>\n");
        sb.Append("</section>\n");
    }

    private static void AppendTable(StringBuilder sb, IReadOnlyList<Violation> violations)
    {
        sb.Append("<table id=\"violations\">\n<thead><tr>");
        sb.Append("<th>Severity</th><th>Rule</th><th>Message</th><th>Location</th><th>Project</th>");
        sb.Append("</tr></thead>\n<tbody>\n");

        foreach (var violation in violations)
        {
            var location = violation.File is null
                ? violation.Project ?? violation.Symbol ?? "<unknown>"
                : $"{violation.File}({violation.Line ?? 0},{violation.Column ?? 0})";

            var severityClass = violation.Severity.ToString().ToLowerInvariant();
            var project = violation.Project ?? string.Empty;

            sb.Append("<tr class=\"violation\" ");
            sb.Append($"data-severity=\"{Esc(violation.Severity.ToString())}\" ");
            sb.Append($"data-rule-id=\"{Esc(violation.RuleId)}\" ");
            sb.Append($"data-project=\"{Esc(project)}\">\n");
            sb.Append($"<td class=\"severity severity-{severityClass}\">{Esc(violation.Severity.ToString())}</td>\n");
            sb.Append($"<td>{Esc(violation.RuleId)}</td>\n");
            sb.Append("<td class=\"message\">").Append(Esc(violation.Message));
            if (violation.Remediation is not null)
            {
                sb.Append($"<div class=\"remediation\">remediation: {Esc(violation.Remediation.Trim())}</div>");
            }

            sb.Append("</td>\n");
            sb.Append($"<td>{Esc(location)}</td>\n");
            sb.Append($"<td>{Esc(project)}</td>\n");
            sb.Append("</tr>\n");
        }

        sb.Append("</tbody>\n</table>\n");
        sb.Append("<p class=\"empty-state\"");
        if (violations.Count > 0)
        {
            sb.Append(" hidden");
        }

        sb.Append(">No violations match the current filters.</p>\n");
    }

    private static string Esc(string? value) => value is null ? string.Empty : HtmlEncoder.Default.Encode(value);

    private const string Css = """
        :root { color-scheme: light dark; font-family: system-ui, sans-serif; }
        body { margin: 0; padding: 1.5rem; }
        header h1 { margin: 0 0 0.5rem; font-size: 1.4rem; }
        .status { display: inline-block; padding: 0.15rem 0.6rem; border-radius: 0.3rem; font-weight: bold; margin-bottom: 0.5rem; }
        .status-passed { background: #1a7f37; color: #fff; }
        .status-failed { background: #cf222e; color: #fff; }
        .summary { margin-bottom: 0.75rem; opacity: 0.85; }
        .severity-counts { margin-bottom: 1rem; }
        .badge { display: inline-block; padding: 0.15rem 0.5rem; border-radius: 0.3rem; margin-right: 0.4rem; font-size: 0.85rem; color: #fff; }
        .badge-info { background: #0969da; }
        .badge-warning { background: #9a6700; }
        .badge-error { background: #cf222e; }
        .badge-critical { background: #8250df; font-weight: bold; }
        .evaluation-errors { border: 1px solid #cf222e; border-radius: 0.3rem; padding: 0.5rem 1rem; margin-bottom: 1rem; }
        .evaluation-errors h2 { font-size: 1rem; margin: 0 0 0.4rem; }
        .evaluation-errors ul { margin: 0; padding-left: 1.2rem; }
        .filters { display: flex; flex-wrap: wrap; gap: 0.75rem; align-items: center; margin-bottom: 1rem; }
        .filters label { white-space: nowrap; }
        table { border-collapse: collapse; width: 100%; }
        th, td { text-align: left; padding: 0.4rem 0.6rem; border-bottom: 1px solid rgba(128, 128, 128, 0.3); vertical-align: top; }
        .severity { font-weight: bold; }
        .severity-info { color: #0969da; }
        .severity-warning { color: #9a6700; }
        .severity-error { color: #cf222e; }
        .severity-critical { color: #8250df; }
        .remediation { opacity: 0.75; font-size: 0.85rem; margin-top: 0.2rem; }
        .empty-state { opacity: 0.7; font-style: italic; }
        """;

    private const string Js = """
        (function () {
          var rows = Array.prototype.slice.call(document.querySelectorAll('#violations tbody tr'));
          var sevToggles = document.querySelectorAll('.sev-toggle');
          var ruleIdFilter = document.getElementById('rule-id-filter');
          var projectFilter = document.getElementById('project-filter');
          var messageSearch = document.getElementById('message-search');
          var visibleCount = document.getElementById('visible-count');
          var emptyState = document.querySelector('.empty-state');

          Object.keys(
            rows.reduce(function (set, row) { if (row.dataset.project) { set[row.dataset.project] = true; } return set; }, {})
          ).sort().forEach(function (p) {
            var option = document.createElement('option');
            option.value = p;
            option.textContent = p;
            projectFilter.appendChild(option);
          });

          function apply() {
            var activeSeverities = Array.prototype.filter.call(sevToggles, function (t) { return t.checked; })
              .map(function (t) { return t.value; });
            var ruleIdQuery = ruleIdFilter.value.trim().toLowerCase();
            var project = projectFilter.value;
            var messageQuery = messageSearch.value.trim().toLowerCase();

            var visible = 0;
            rows.forEach(function (row) {
              var matches =
                activeSeverities.indexOf(row.dataset.severity) !== -1 &&
                (!ruleIdQuery || row.dataset.ruleId.toLowerCase().indexOf(ruleIdQuery) !== -1) &&
                (!project || row.dataset.project === project) &&
                (!messageQuery || row.querySelector('.message').textContent.toLowerCase().indexOf(messageQuery) !== -1);

              row.hidden = !matches;
              if (matches) { visible++; }
            });

            visibleCount.textContent = visible + ' of ' + rows.length + ' shown';
            if (rows.length > 0) {
              emptyState.hidden = visible !== 0;
            }
          }

          Array.prototype.forEach.call(sevToggles, function (t) { t.addEventListener('change', apply); });
          ruleIdFilter.addEventListener('input', apply);
          projectFilter.addEventListener('change', apply);
          messageSearch.addEventListener('input', apply);
          apply();
        })();
        """;
}
