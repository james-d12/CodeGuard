# Rule Validation Plan — validating the rules themselves

> Status: **done**. Implemented as designed below: `RuleFileIssue`/`RuleSetValidationReport`/
> `RuleFileLoader.TryLoadFromFile`/`ValidateDirectories` under
> `src/CodeGuard.Configuration/Validation/` and `Loading/`, `CliRepositoryContext.ValidateRules()`,
> the new `check-rules` command (`src/CodeGuard.Cli/Commands/CheckRulesCommand.cs`), the shared
> `RuleValidationReportWriter`, and the mandatory pre-flight gate in `ValidateCommand`. Covered by
> new tests in `RuleFileLoaderTests` and `CodeGuard.Cli.Tests` (`CheckRulesCommandTests`,
> `ValidateCommandPreflightTests`). See `docs/IMPLEMENTATION_STATUS.md` for the summary entry. Kept
> for design rationale.

## Context

Rule YAML files already go through two layers of correctness checking today —
`RuleSchemaValidator` (JSON Schema, structural) and `RuleDocumentParser`/the selector/assertion
registries (semantic: unknown `kind`, mutually-exclusive `analyzer` vs `target`/`assertions`, enum
parsing) — both invoked from `RuleFileLoader.LoadFromFile`
(`src/CodeGuard.Configuration/Loading/RuleFileLoader.cs`), plus a duplicate-rule-ID check across a
directory set in `LoadFromDirectoriesWithSource`. The problem is *how* these are surfaced:

- **Fail-fast, not aggregated**: the loader throws (`RuleSchemaValidationException` /
  `RuleParsingException` / `RuleLoadException`) on the *first* bad file. With 120+ rule files
  (`docs/done/STAGE_B_PROGRESS.md`), fixing a ruleset means one exception, one fix, one rerun,
  repeated — never a full picture of everything wrong.
- **Uncaught in the CLI**: no command catches these exceptions. `codeguard validate` against a
  repo with one bad rule file currently crashes with a raw, user-hostile .NET stack trace instead of
  a clean report — and it does so only *after* `CliRepositoryContext.Resolve` has already run, so a
  user can't tell "is my ruleset broken" from "is my repo broken" from the output.
- **No standalone entry point**: there is no way to check a folder of rule YAML in isolation — you
  can only discover problems by running the full `validate` pipeline (which needs a target repo,
  MSBuild, etc.) against rules wired into `.codeguard/config.yml`.

This plan makes rule-set correctness a first-class, always-run step: a new `check-rules` CLI command
that validates a folder of rule YAML directly, and `validate` running that same check as a mandatory
pre-flight gate before touching the analysis model, so a broken ruleset always produces a clean
aggregated report instead of a crash.

**Scope for v1 is structural correctness only** — JSON Schema conformance, selector/assertion/analyzer
`kind` resolution, `target`/`assertions` vs `analyzer` mutual exclusion, and duplicate rule IDs. Style
or best-practice linting (e.g. overly broad selectors, missing `description`/`remediation`) and any
cross-repo semantic checks (e.g. "does this namespace exist") are explicitly out of scope here.

## Design

### 1. Non-throwing aggregate validation in `CodeGuard.Configuration`

Add to `CodeGuard.Configuration/Validation/`:

- `RuleFileIssue` (record): `string SourceFile`, `IReadOnlyList<string> Errors`.
- `RuleSetValidationReport` (record): `IReadOnlyList<(RuleDefinition Rule, string SourceFile)> Rules`,
  `IReadOnlyList<RuleFileIssue> Issues`, `bool IsValid => Issues.Count == 0`.

Refactor `RuleFileLoader` (`src/CodeGuard.Configuration/Loading/RuleFileLoader.cs`) around a new
non-throwing core:

- `TryLoadFromFile(string filePath, out RuleDefinition? rule, out IReadOnlyList<string> errors)` —
  same schema-validate-then-parse logic as today's `LoadFromFile`, but catches
  `RuleSchemaValidationException`/`RuleParsingException`/`RuleLoadException` internally and returns
  them as an error list instead of throwing.
- `LoadFromFile` becomes a thin wrapper: call `TryLoadFromFile`, throw the first error (as today) if
  any — preserves existing behavior and every existing test in
  `tests/CodeGuard.Configuration.Tests/RuleFileLoaderTests.cs` unchanged (they assert
  `RuleParsingException`/`RuleSchemaValidationException`/`RuleLoadException` from
  `LoadFromFile`/`LoadFromDirectory`).
- New `ValidateDirectories(IEnumerable<string> directoryPaths) : RuleSetValidationReport` — walks
  files with the same discovery logic already in `LoadFromDirectoriesWithSource` (extract that
  extension-filter + ordering loop into a small shared private helper rather than duplicating it),
  calls `TryLoadFromFile` per file collecting both successes and `RuleFileIssue`s, then does one
  duplicate-ID pass over the successfully-parsed rules and appends duplicates as additional
  `RuleFileIssue`s (message format matching the existing "Duplicate rule id 'X' … already defined in
  'Y'" wording).
- `LoadFromDirectoriesWithSource`/`LoadFromDirectories` are reimplemented on top of
  `ValidateDirectories`: if `Issues` is non-empty, throw using the first issue (preserves the existing
  `RuleLoadException` duplicate-ID test); otherwise return `Rules`. This means `check-rules` and
  `validate`'s pre-flight gate share the exact same parsing pass as the real rule-loading path — no
  double-parsing, no drift between "what check-rules approves" and "what validate actually loads."

### 2. `CliRepositoryContext` gets a non-throwing accessor

Add `ValidateRules() : RuleSetValidationReport` to
`src/CodeGuard.Cli/Support/CliRepositoryContext.cs`, calling
`RuleFileLoader.CreateDefault().ValidateDirectories(Layout.RulesPaths)` — parallel to the existing
`LoadRules()`/`LoadRulesWithSource()`.

### 3. New `check-rules` CLI command

`src/CodeGuard.Cli/Commands/CheckRulesCommand.cs`, registered in `Program.cs` alongside the other
subcommands. Options, reusing `CommonOptions` (`src/CodeGuard.Cli/Support/CommonOptions.cs`) exactly
as `list-rules`/`list-standards` do, so the two usage modes both work with no new resolution code:

- No explicit rules folder → resolves via `CliRepositoryContext.Resolve` exactly like every other
  command (`--path`/`--config`/`--rules-source`/`--branch`), i.e. "check whatever ruleset this repo
  is currently configured to use."
- A directly-supplied folder → reuse `--rules-source` (already means "ad-hoc rules location, local
  directory or git URL, bypassing `.codeguard/config.yml`") rather than inventing a second
  "rules path" concept — this already does exactly what's needed ("point directly at a folder
  containing rules") and keeps one option name meaning one thing across every command.

```
codeguard check-rules                              # whatever this repo is configured to use
codeguard check-rules --rules-source ./some/rules   # ad-hoc folder, no repo/config needed
```

Add `--format console|json` (default `console`), matching `list-rules`'s pattern:

- console: per-file pass/fail, only printing detail for failures, e.g.:
  ```
  Checked 120 rule files: 118 passed, 2 failed.

  rules/ddd/entity.yml
    - Unknown assertion kind 'must_inhert_from'.
  rules/csharp/naming.yml
    - Duplicate rule id 'CS-NAMING-004' (already defined in 'rules/csharp/other.yml').
  ```
- json: serialize `RuleSetValidationReport` (camelCase, matching `ListRulesCommand`'s
  `JsonSerializerOptions` pattern) for tooling/CI consumption.

Exit code: `0` if `report.IsValid`, else `1`. No `--fail-on`/severity-threshold concept here —
these are authoring errors, not violations with severity.

### 4. `validate` runs the same check as a mandatory pre-flight gate

In `ValidateCommand.Build()`'s `SetAction` (`src/CodeGuard.Cli/Commands/ValidateCommand.cs`),
immediately after resolving `context` and before `SolutionFileLocator.Resolve`/building the
`AnalysisModelBuilder`:

```csharp
var ruleReport = context.ValidateRules();
if (!ruleReport.IsValid)
{
    RuleValidationReportWriter.WriteConsole(ruleReport, Console.Out); // shared with check-rules
    return 1;
}
var rules = ruleReport.Rules.Select(r => r.Rule).ToList();
```

replacing the current `context.LoadRules()` call — so `validate` never reaches
`MsBuildAnalysisProvider`/Buildalyzer at all if the ruleset itself is broken, and always produces the
same clean report `check-rules` would, instead of an unhandled exception. This gate is unconditional
(no `--skip-rule-validation` escape hatch) — the check is cheap (no MSBuild involved) and a broken
ruleset should never silently or crashily proceed.

Share the console/json report-formatting code between `CheckRulesCommand` and `ValidateCommand` via a
small `RuleValidationReportWriter` in `CodeGuard.Cli/Support/`, rather than duplicating it.

### 5. Tests

- `tests/CodeGuard.Configuration.Tests/RuleFileLoaderTests.cs`: keep all existing tests passing
  unchanged (they now exercise `ValidateDirectories` indirectly through the throwing wrappers). Add:
  - `ValidateDirectories_MultipleBadFiles_ReportsAllOfThem` — two independently-broken files in one
    directory → report has 2 issues, one per file, in one pass (proves aggregation, not fail-fast).
  - `ValidateDirectories_DuplicateId_ReportsAsIssueNotException` — duplicate ID surfaces in
    `report.Issues`, not a thrown exception.
  - `ValidateDirectories_AllValid_ReturnsRulesAndNoIssues`.
- `tests/CodeGuard.Cli.Tests/`: add `CheckRulesCommandTests` (valid folder → exit 0; folder with a
  bad rule file → exit 1 + issue text in output; `--format json` shape) and extend the `validate`
  command's tests with a case feeding a broken rules folder and asserting a clean report + exit 1 +
  **no** attempt to reach `MsBuildAnalysisProvider` (e.g. point `--path` at a nonexistent/garbage repo
  root — if it fails only due to the rule error and never gets to a Buildalyzer-related error, that
  proves the gate short-circuits correctly).

## Verification

- `dotnet build` — 0 errors/warnings (per current CLAUDE.md standard).
- `dotnet test` — full suite green, including new tests above.
- Manually: `dotnet run --project src/CodeGuard.Cli -- check-rules` against this repo's own
  `rules/` (should report 120 passed, 0 failed today) and against a scratch copy with one file edited
  to have an invalid `kind` and another with a duplicate `id`, confirming both are reported together
  in one run.
- Manually: `dotnet run --project src/CodeGuard.Cli -- validate` with one rule file temporarily
  broken, confirming a clean aggregated report and exit code 1 with no stack trace, and no attempt to
  invoke Buildalyzer/MSBuild.
