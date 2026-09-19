# Rule Versioning Plan

> Status: **in progress**. Tracked on `feature/rule-versioning`. Move to `docs/done/` only once
> every item below is verified shipped (found in `src/`/`tests/`, not just asserted here), with
> cross-references updated to the new path.

## Context

`docs/HIGH_LEVEL_ROADMAP.md` §8 ("Rule Identity") and §11 ("Versioning") call for rules to be
versioned so that violations/diagnostics can be traced back to the exact rule version that produced
them — a precondition for "the same source and rules should produce the same result" (§2, goal 1)
and for later work like rule-change-impact analysis. `docs/REFACTORING.md` §12 sketches this in more
detail (`id`/`version`/`status`/`severity`, lifecycle states, diagnostics carrying `ruleVersion`), but
that document is a separate, not-started architectural-evolution proposal (per CLAUDE.md) and must
stay untouched.

`docs/IMPLEMENTATION_STATUS.md` already records that half of §12 was evaluated and rejected: the
`status` lifecycle enum was scoped down and explicitly rejected for having no consumer. The `version`
+ diagnostic-stamping half was **never evaluated on its own merits** and is flagged as "a materially
bigger, cross-cutting change (touches the evaluator and every `IViolationReporter`)." This plan
proposes that half now, scoped to a concrete, minimal, additive slice with clear consumers, and
explicitly does not revisit `status`.

Research confirmed there is currently **no** version concept anywhere: `RuleDefinition` has no
`Version` field, `rule.schema.json` has no `version` key, `Violation`/`ValidationResult` carry no
version data, and SARIF's native `ReportingDescriptor.Version` support goes unused. Rule identity
(`id`) is already unique per configured rule set (enforced in `RuleFileLoader.ValidateDirectories`),
one rule = one YAML file. There's a documented precedent to follow closely: `metadata.source.file` +
`metadata.source.fingerprint` (`docs/done/RULE_SOURCE_AND_LINKED_DOCUMENTATION.md`) already implements
an opt-in, sha256-fingerprint-based drift check with a `rules validate --update-fingerprints` writer —
this plan reuses that exact shape for a *second*, independent fingerprint: one over the rule's own
enforceable body (`target`+`assertions`+`when`, or `analyzer`), rather than over external linked docs.

Per the user's decisions: version is a **plain positive integer** (`version: 2`, default `1`),
matching `docs/REFACTORING.md`'s own example and avoiding SemVer ambiguity for a declarative rule.
Drift detection (rule body changed since its last recorded version fingerprint) is a **hard failure**
in `rules validate` — i.e. it affects the exit code, unlike the source-drift check, which stays
warning-only. This gives the determinism guarantee real teeth: once a rule opts in, CI blocks silent
behavior changes made without updating that rule's fingerprint (and, by implication, without the
author considering whether to bump `version`).

**Known limitation, stated up front:** this cannot *enforce* that `version` itself was actually
incremented — only that the enforceable body still matches its last recorded fingerprint. Verifying
"version was bumped" would need persisted history (git diff against a base branch, or a repo-wide
lock file) which is out of scope here. This is analogous to the existing MSBuild self-analysis
"Known limitation" pattern in CLAUDE.md — document it, don't half-build around it.

## Design

### 1. Data model (`src/CodeGuard.RuleModel/Rules/RuleDefinition.cs`)

- Add `public int Version { get; init; } = 1;` to `RuleDefinition`.
- Add `public string? VersionFingerprint { get; init; }` to `RuleMetadata`, as a sibling to
  `Source` (not nested inside it — it's an independent concern with a single field, doesn't need
  `RuleSource`'s richer shape). Doc-comment it the same way `RuleSource.Fingerprint` is documented:
  format, how it's captured, what opts a rule in (its mere presence).

### 2. Schema (`src/CodeGuard.Configuration/Validation/Schemas/rule.schema.json`)

Purely additive, matching the established convention for every prior optional field:

- Top level: `"version": { "type": "integer", "minimum": 1 }`.
- Under `metadata.properties`: `"versionFingerprint": { "type": "string", "pattern": "^sha256:[0-9a-f]{64}$" }`
  (same pattern already used for `metadata.source.fingerprint`).
- No `dependentRequired` needed (unlike `source.fingerprint` → `source.file`) — `version` always has
  a value (defaults to 1), so there's nothing to require.
- After editing, run `scripts/sync-skill-references.sh` so
  `skills/codeguard-rule-generation/references/rule-schema.json` stays identical (CI diffs it).

### 3. Parsing (`src/CodeGuard.Configuration/Parsing/RuleDocumentParser.cs`)

- Parse optional `version` (default 1) alongside the other top-level scalar fields.
- Extend `ParseMetadata` to also read `metadata.versionFingerprint` into `RuleMetadata.VersionFingerprint`.

### 4. Shared body canonicalization (extract from `RuleSetAnalyzer`)

`RuleSetAnalyzer.FindExactDuplicates` (`src/CodeGuard.Configuration/Analysis/RuleSetAnalyzer.cs:158-212`)
already canonicalizes a rule's `target`+`assertions` (or `analyzer`) into an order-independent string
key for duplicate detection — but it does **not** currently include `when`, which is a real gap
(a `when` condition gates whether assertions apply, so it's part of the enforceable body). Extract
this into a shared, corrected utility so both duplicate-detection and version-fingerprinting use one
definition of "a rule's enforceable body":

- New internal static class, e.g. `RuleBodyCanonicalizer` in `CodeGuard.Configuration.Analysis`:
  - `ExtractEnforceableBody(JsonObject document) -> JsonObject` — pulls `analyzer` if present,
    otherwise `target` + `assertions` + `when` (fixing the missing-`when` gap as a deliberate, bundled
    correctness fix).
  - `Canonicalize(JsonNode?) -> string` — the existing key-sorted serialization logic, moved as-is.
  - `ComputeFingerprint(JsonObject document) -> string` — `"sha256:" + Convert.ToHexStringLower(SHA256.HashData(utf8 bytes of Canonicalize(ExtractEnforceableBody(document))))`.
- Update `RuleSetAnalyzer.FindExactDuplicates` to call the shared `ExtractEnforceableBody`/`Canonicalize`
  instead of its private copies. This changes behavior slightly (rules differing only in `when` will
  no longer be false-flagged as exact duplicates) — call this out explicitly since it's a real, if
  small, behavior change to an existing CI-gating check; verify against `examples/rules/` that no new
  duplicate-group regressions or omissions appear.

### 5. New checker: `RuleVersionChecker` (new file, e.g. `src/CodeGuard.Configuration/Versioning/RuleVersionChecker.cs`)

Mirrors `RuleSourceChecker`'s shape but simpler (only one failure state — there's no realistic
"opted in but never captured" state for this fingerprint, since nobody hand-writes a sha256):

```csharp
public sealed record RuleVersionIssue(string RuleId, string SourceFile, string RecordedFingerprint, string ComputedFingerprint);
public sealed record RuleVersionCheckReport(IReadOnlyList<RuleVersionIssue> Issues)
{
    public bool IsValid => Issues.Count == 0;
}

public static class RuleVersionChecker
{
    public static RuleVersionCheckReport Check(IReadOnlyList<(RuleDefinition Rule, string SourceFile)> rules)
    {
        // For each rule: skip if Metadata?.VersionFingerprint is null (not opted in, zero cost).
        // Otherwise re-read the raw document (RuleFileLoader.ReadDocument), compute the current
        // fingerprint via RuleBodyCanonicalizer.ComputeFingerprint, and record a RuleVersionIssue if
        // it doesn't match the recorded one.
    }
}
```

### 6. `--update-fingerprints` writer

- New `RuleVersionFingerprintWriter.WriteFingerprint(sourceFile, fingerprint)`, mirroring
  `RuleSourceFingerprintWriter` but targeting the `metadata.versionFingerprint` key in place.
- `ValidateCommand` (`src/CodeGuard.Cli/Commands/Rules/ValidateCommand.cs`): after computing
  `sourceReport`, also compute `versionReport = RuleVersionChecker.Check(report.Rules)`. Extend the
  existing `--update-fingerprints` handling (currently only touches `RuleSourceIssue`s) to also
  recompute and write mismatched version fingerprints, removing resolved issues before printing —
  same pattern as `UpdateFingerprints` already does for source issues. Update the option's help text
  to mention both fingerprint kinds.
- **Exit code — this is the hard-failure change:**
  `return Task.FromResult(report.IsValid && versionReport.IsValid ? 0 : 1);`
  (today it's just `report.IsValid ? 0 : 1`; `sourceReport` findings still never affect this).
- `RuleValidationReportWriter` (console + JSON): add a clearly-labeled section for version-fingerprint
  mismatches, alongside the existing source-issue section, in both output formats.

### 7. Evaluation & reporting — stamping `RuleVersion` onto every violation

- `src/CodeGuard.Core/Results/ValidationResult.cs`: add `int RuleVersion` to the `Violation` record
  (placed right after `RuleId` for logical grouping).
- `src/CodeGuard.Core/Evaluation/RuleEvaluator.cs`: pass `rule.Version` at both `Violation`
  construction sites (`EvaluateAnalyzerRule` and `CreateViolation`).
- `Json/JsonViolationReporter.cs`: no code change needed — it serializes the record directly, so
  `ruleVersion` appears automatically (confirmed this is how the existing fields already flow).
- `Sarif/SarifViolationReporter.cs`: set the native, currently-unused `ReportingDescriptor.Version`
  field per rule id group: `Version = g.First().RuleVersion.ToString()`. This is the "concrete
  consumer" for the SARIF surface — GitHub code scanning and other SARIF consumers already understand
  `ReportingDescriptor.Version` semantics for correlating findings across a rule's revisions.
- Console/HTML reporters: check `Console/ConsoleViolationReporter.cs` and `Html/HtmlViolationReporter.cs`
  during implementation and add the version alongside the rule id if it's a small, in-place change
  consistent with their existing layout; not a blocking requirement if it'd need restructuring.

### 8. CLI surfacing (`rules list`, `rules explain`)

- `ListCommand.cs`: add `Version` to the private `RuleSummary` record (JSON output) and add a
  `VERSION` column to the console table (`WriteTable`), positioned right after `ID`.
- `ExplainCommand.cs`: add `Version:` to `PrintSummary`'s console output (near `Id`/`Name`), and
  `["version"] = rule.Version` to `PrintJson`'s payload. If `Metadata?.VersionFingerprint` is set,
  surface it the same way `Source file`/fingerprint-captured status is already shown.

### 9. Documentation

- `docs/IMPLEMENTATION_STATUS.md`: add a "Post-v1 addition: rule versioning" section (matching the
  existing style of the `metadata.source` section), describing: the final shape (`version` int +
  `metadata.versionFingerprint`), that it's hard-enforced once opted in (unlike source drift), the
  shared `RuleBodyCanonicalizer` extraction and its `when`-inclusion fix, and the known limitation
  that this can't verify `version` itself was incremented. Explicitly supersede the earlier line
  noting `version` "was never evaluated on its own merits."
- Do **not** edit `docs/REFACTORING.md` — per CLAUDE.md it stays untouched regardless of how much
  shipped code resembles what it describes.
- `skills/codeguard-rule-generation/` reference material: document the two new optional fields for
  rule authors (exact location to be found during implementation — likely a field-reference doc
  alongside the schema copy), and re-run `scripts/sync-skill-references.sh`.

### Out of scope (explicitly deferred, not half-built)

- Verifying that `version` itself increased (needs git history or a lock file) — documented as a
  known limitation, not attempted.
- Any repo-level policy toggle in `.codeguard/config.yml` to make this configurable per-repo — not
  needed since enforcement is already opt-in per rule via `metadata.versionFingerprint`'s presence.
- Rolling versioning out across all 125 `examples/rules/` files — out of scope; only enable it on one
  or two example rules to prove the feature end-to-end (see Verification).
- `rules analyze` changes — this checker stays a `rules validate`-only concern, matching exactly
  where `RuleSourceChecker` already lives, and doesn't need folding into `RuleSetAnalyzer`'s report.
- Rule lifecycle `status` (experimental/active/deprecated/retired) — already evaluated and rejected;
  not being re-opened here.

## Verification

1. `dotnet build` — 0 errors/0 warnings.
2. `dotnet test` — full suite passes, including new tests for: `RuleDefinition.Version` defaulting
   and parsing; schema acceptance/rejection of `version`/`metadata.versionFingerprint`;
   `RuleBodyCanonicalizer` (including a regression test proving two rules differing only in `when`
   are no longer treated as exact duplicates); `RuleVersionChecker` (skip when not opted in, pass on
   match, issue on mismatch); `RuleEvaluator` stamping `Violation.RuleVersion`; SARIF reporter setting
   `ReportingDescriptor.Version`.
3. Opt in one existing example rule (pick a simple, stable one) by adding `version: 1` and running
   `dotnet run --project src/CodeGuard.Cli -- rules validate --rules-source examples/rules --update-fingerprints`
   to capture its initial `metadata.versionFingerprint`; confirm `rules validate` then passes cleanly.
   Then hand-edit that rule's `assertions` without bumping anything and re-run `rules validate` —
   confirm it now fails with a clear version-drift finding, and that running `--update-fingerprints`
   again clears it.
4. `dotnet run --project src/CodeGuard.Cli -- rules list --rules-source examples/rules` and
   `rules explain <that rule's id> --rules-source examples/rules` (both console and `--format json`) —
   confirm `version` is visible.
5. `dotnet run --project src/CodeGuard.Cli -- validate --format sarif` (or `json`) against this repo —
   confirm `ruleVersion`/`ReportingDescriptor.Version` appear in output.
6. `dotnet run --project src/CodeGuard.Cli -- rules validate --rules-source examples/rules` and
   `rules test --rules-source examples/rules` — confirm the other 124 rules (not opted in) are
   completely unaffected (zero new findings).
7. Run `scripts/sync-skill-references.sh` and confirm no diff is introduced beyond the intended
   schema mirror update; `dotnet format --verify-no-changes` clean.
