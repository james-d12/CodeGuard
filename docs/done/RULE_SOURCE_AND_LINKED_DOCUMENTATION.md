# CodeGuard Rule Sources

## Purpose

Allow CodeGuard rules to reference the original organisational policy or documentation they
implement, while providing deterministic tooling to detect when that source has changed or become
unavailable.

The goal is **traceability and maintenance**, not automatic synchronisation or policy interpretation.

## Relationship to `metadata.source`

CodeGuard already ships `metadata.source` (`document`/`section`/`statement` — see
`docs/IMPLEMENTATION_STATUS.md` and `docs/HIGH_LEVEL_AI_ASSISTING.md` §6/§19), a deliberately
free-text, never-resolved provenance note: "why does this rule exist," with no guarantee the named
document/section exists anywhere real. That design was intentional — an earlier, stricter field
(`RuleDefinition.Standard`) broke when two authoring conventions collided over what a "source"
string should mean, and free text closed off that failure mode.

**This feature extends `metadata.source`, it does not replace or duplicate it.** `document`/
`section`/`statement` keep their exact current meaning and behavior — a rule using only those three
fields is unaffected by anything below. Two new *optional* fields are added to the same object:

* `file` — a repo-relative path to the markdown document the rule was derived from.
* `fingerprint` — a `sha256:...` hash of the linked content, checked for drift.

Setting `file` is what opts a rule into machine-checkability. Real provenance isn't always a
linkable, hashable file in this repo (a verbal decision, an external standard, a page in another
system) — those rules keep using free-text-only `metadata.source` exactly as before; this is
additive, not a requirement.

Example:

```yaml
metadata:
  source:
    document: Architecture Standards
    section: Domain Layer
    statement: "Domain projects must not reference Infrastructure."
    file: docs/architecture.md
    fingerprint: sha256:...
```

`section`, when `file` is set, doubles as the exact-match heading CodeGuard looks up within that
file to scope the fingerprint to just that section (rather than the whole document) — this keeps
drift signal precise: editing an unrelated section of a large standards doc shouldn't warn every
rule linked to that file. If `section` is omitted, the fingerprint covers the whole file.

`statement` remains a paraphrase, never used to search the document verbatim — see "Not in v1"
below for why.

## Source Validation

Rather than a separate command, source checking is folded into `codeguard rules validate`: for
every rule whose `metadata.source` has both `file` and `fingerprint`, validate resolves the file,
locates the section (if `section` is set), recomputes the fingerprint, and **warns** if it no
longer matches. A rule with no `file` is skipped entirely — zero cost, same behavior as today.

Conditions detected:

* Source file no longer exists.
* Referenced section cannot be found.
* Referenced section name matches more than one heading (ambiguous).
* Source content has changed (fingerprint mismatch).
* `file`/`section` resolve fine but no `fingerprint` has been captured yet.

Console output, appended to `rules validate`'s existing summary only when at least one rule has an
issue to report (a clean, matching-fingerprint rule prints nothing at all - not even a confirmation
line; the section is entirely absent when every checked rule is clean):

```text
Checked 3 rule files: 3 passed, 0 failed.

Source checks:
  ⚠ events-must-be-past-tense - source content changed (docs/conventions.md § Event Naming)
      Recorded statement: Domain events are named in the past tense.
      Current content:    Domain events must use past-tense verb names, e.g. OrderPlaced.
      New fingerprint:    sha256:...
  ✗ service-ownership - source document no longer exists (docs/ownership.md)
```

## No Automatic Decisions

CodeGuard must **not** determine whether a source change means the rule itself is now incorrect. It
only reports that the relationship between the rule and its source requires review — and, in this
design, that report is always a **warning**: none of these conditions fail `rules validate`'s exit
code. Only the pre-existing schema/structural checks (schema conformance, known selector/assertion/
analyzer kinds, no duplicate ids) do that. A stale doc reference is a signal for a human to look at,
not a reason to block CI.

```text
  ⚠ domain-no-infrastructure - source content changed (docs/architecture.md § Domain Layer)
      Recorded statement: Domain projects must not reference Infrastructure.
      Current content:    Domain projects should not directly depend on Infrastructure.
      New fingerprint:    sha256:...
```

The user remains responsible for deciding whether to update the rule, update the documentation, or
retire the rule.

## Resolving a warning: `--update-fingerprints`

Once a human has looked at a `source content changed` warning and decided the rule doesn't need to
change, they need a way to accept the new fingerprint without hand-computing a sha256 hex string.
`codeguard rules validate --update-fingerprints` does exactly that: for every rule flagged as
"content changed" or "fingerprint not yet captured," it recomputes the fingerprint from the
already-resolved content and writes it back into that rule's YAML file — editing only the
`fingerprint` value in place (not regenerating the file), so comments, key order, and formatting
elsewhere are untouched. Off by default; this is the only CodeGuard command that mutates rule files
as a side effect of a check, so it requires the explicit flag. The resulting `git diff` is the
review surface — CodeGuard recomputes the mechanical hash, a human/reviewer decides via the diff
whether to keep it.

Rules with a genuinely broken link (missing file/section, or an ambiguous section) have nothing to
fingerprint and are left for a human to fix regardless of the flag.

## Normal Validation vs Source Validation

Both live under `rules validate` now, but stay conceptually distinct:

```text
codeguard rules validate
    -> schema conformance, known selector/assertion/analyzer kinds, no duplicate ids (fails the build)
    -> PLUS: are rules' linked documentation sources still fresh? (warns, never fails the build)

codeguard validate
    -> does the repository comply with the rules? (unaffected by any of the above)
```

Source checking must not unexpectedly change the result of the pre-existing structural validation,
and the top-level `codeguard validate` (repository evaluation) and `rules create` (scaffolding) are
both unaffected by this work — only `rules validate` gains behavior.

## Maintenance Workflow

```text
Organisation policy/documentation
          |
       CodeGuard rule (metadata.source.document/section/statement, human-readable)
          |
    file + fingerprint (opt-in, machine-checkable)
          |
     source changes
          |
 codeguard rules validate detects drift, warns
          |
      human review
          |
 --update-fingerprints once satisfied nothing else needs to change
```

## Not in v1

* **Moved-section detection** ("the heading was renamed, but the same text now lives elsewhere in
  the file"). This would need a verbatim-text field to search for — `statement` is deliberately a
  paraphrase, not a verbatim quote (so this repo's own rule content, derived from real company
  conventions, doesn't get more of that original text copied into git than exists today). Reusing
  `statement` for verbatim search would silently break that guarantee, so this is cut rather than
  adding a second, verbatim-text field. Revisit only if there's a concrete need.
* Fuzzy/case-insensitive heading matching; Setext headings (only ATX `#`-style headings are
  recognised in v1).
* Non-repo-local sources (Confluence, SharePoint, etc.) — scope stays repository-local Markdown for
  now; the model is extensible to these later but nothing requires it today.
* `--update-fingerprints` targeting a single rule rather than sweeping every eligible rule in the
  set.

## Design Principle

**CodeGuard should provide evidence that a rule's source has changed, not make a judgement about
what that change means.**
