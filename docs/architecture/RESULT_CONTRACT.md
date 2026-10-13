# Validation result contract (v1)

`codeguard validate --format json` writes a versioned report. It is the public contract that CI
integrations, baselines, the MCP server and the reporting API consume. It is deliberately **not** a
serialisation of the internal `CodeGuard.Core.Results.ValidationResult`: the internal model can
change without changing this output.

| Artifact | Location |
|---|---|
| Output DTOs | `src/CodeGuard.Reporting/Contracts/ValidationReportV1.cs` |
| Mapper (`ValidationResult` to DTO) | `src/CodeGuard.Reporting/Contracts/ValidationReportMapper.cs` |
| JSON Schema (draft 2020-12, embedded resource) | `src/CodeGuard.Reporting/Schemas/validation-report.v1.schema.json` |
| Tests | `tests/CodeGuard.Core.Tests/Reporting/ValidationReport*Tests.cs` |

The schema `$id` is `https://codeguard.internal/schemas/validation-report.v1.schema.json`. Every
object has `additionalProperties: false`, so a consumer can rely on the set of keys.

## Shape

```json
{
  "schemaVersion": "1.0",
  "toolVersion": "0.3.1",
  "status": "failed",
  "evaluatedAtUtc": "2026-10-13T14:42:10.7202958+00:00",
  "durationMs": 6705,
  "metrics": {
    "rulesEvaluated": 11,
    "rulesPassed": 7,
    "rulesFailed": 4,
    "rulesErrored": 0,
    "violationCount": 4,
    "violationsBySeverity": { "info": 0, "warning": 0, "error": 3, "critical": 1 },
    "evaluationErrorCount": 0,
    "analysisWarningCount": 0
  },
  "violations": [
    {
      "ruleId": "DDD-ENTITY-001",
      "ruleVersion": 1,
      "severity": "error",
      "message": "'Contoso.Domain.Entities.LegacyThing' must inherit from 'Contoso.Domain.Entity<*>'.",
      "file": "Contoso.Domain/Entities/LegacyThing.cs",
      "line": 5,
      "column": 14,
      "symbol": "Contoso.Domain.Entities.LegacyThing",
      "project": "Contoso.Domain",
      "remediation": "Inherit from Contoso.Domain.Entity<TId>.",
      "documentationReferences": []
    }
  ],
  "evaluationErrors": [],
  "analysisWarnings": []
}
```

Optional fields (`file`, `line`, `column`, `symbol`, `project`, `remediation`, and the optional
fields of `evaluationErrors` and `analysisWarnings`) are omitted when unknown, not written as `null`.

## Top-level fields

| Field | Type | Meaning |
|---|---|---|
| `schemaVersion` | `"1.0"` | Version of this contract. A breaking change to the shape needs a new schema version. |
| `toolVersion` | string | Version of the CLI that produced the report. Taken from the assembly informational version with any `+commit` suffix removed. |
| `status` | enum | See below. |
| `evaluatedAtUtc` | date-time | When rule evaluation completed, ISO 8601 with offset. |
| `durationMs` | integer | Wall-clock milliseconds from the start of the `validate` command until the report is written. |
| `metrics` | object | Counts; see below. |
| `violations` | array | Violations that survived `--severity-threshold`. |
| `evaluationErrors` | array | Rules that threw during evaluation. |
| `analysisWarnings` | array | Signals that the analysis model is incomplete, e.g. a project that MSBuild could not fully load. |

### `status`

| Value | Meaning |
|---|---|
| `passed` | No violations reported and no evaluation errors. |
| `failed` | At least one violation reported and no evaluation errors. |
| `partiallyEvaluated` | At least one rule threw during evaluation, so results are incomplete. |

`status` describes the report, not the process exit code. It is computed after
`--severity-threshold`, so a report with only warnings is `failed` even when `--fail-on error`
makes the command exit 0. Exit `1` is returned when a violation at or above `--fail-on` is
reported, or when any evaluation error occurred.

### `durationMs`

An integer number of milliseconds, measured by the CLI. It covers rule loading, model building and
evaluation, and ends just before the report is written. A library caller that does not measure
time gets `0`.

### `metrics`

| Field | Meaning |
|---|---|
| `rulesEvaluated` | Enabled rules that were run (after `--rule` filtering). |
| `rulesPassed` / `rulesFailed` / `rulesErrored` | Per-rule outcome counts. |
| `violationCount` | Length of `violations`. |
| `violationsBySeverity` | Counts for `info`, `warning`, `error` and `critical`. All four keys are always present. |
| `evaluationErrorCount` | Length of `evaluationErrors`. |
| `analysisWarningCount` | Length of `analysisWarnings`. |

## Changes from the pre-v1 output

The earlier output was the internal `ValidationResult` serialised directly. Consumers of
`--format json` must update for v1:

- `rulesEvaluated`, `rulesPassed`, `rulesFailed` and `rulesErrored` moved under `metrics`.
- `schemaVersion`, `toolVersion`, `durationMs` and `metrics` are new.
- `status` keeps its values (`passed`, `failed`, `partiallyEvaluated`).
- Violation, evaluation error and analysis warning fields keep their names.

## Versioning policy

- Adding an optional field, or a new value to a documented enum, keeps `schemaVersion` `1.0`.
  Consumers must ignore unknown enum values. Note that the schema's `additionalProperties: false`
  means a new field also changes the schema, so the schema is updated in the same change.
- Removing or renaming a field, changing a type, or changing the meaning of a value requires a new
  `schemaVersion` and a new `validation-report.vN.schema.json`.

## Not yet in the contract

Planned by the roadmap but not part of v1: `rulesVersion`, `repository`, `commit`, `branch`, a
`timestamp` separate from `evaluatedAtUtc`, and per-run size metrics such as projects and types
analysed. These need the run context to be passed into the reporter.
