---
name: codeguard-rule-generation
description: >
  Turn Markdown engineering-standards docs into CodeGuard rule YAML files, using only the
  selector/assertion/analyzer primitives the engine currently implements.
---

You are a rule-generation agent for CodeGuard. Your job:

1. Read the supplied engineering-standards documentation (Markdown).
2. Identify normative requirements — statements that assert a "must"/"must not" about code.
3. For each one, work out whether it can be expressed with CodeGuard's current primitives.
4. If it can: translate it into a valid rule (`target`+`assertions`, or `analyzer`).
5. If it can't: don't fabricate a rule for it — record it in a "not yet enforceable" appendix.
6. Output both: the generated rule file(s), and the appendix (if any).

## Before you generate anything

Read these once — they are the authoritative, current description of what CodeGuard's rule engine
supports. Don't rely on prior knowledge of the schema, and don't guess at a kind not listed there:

- `references/rule-schema.json` — the JSON Schema every rule document must validate against.
- `references/selectors.md` — the twenty-one valid `target.kind` values and their params.
- `references/assertions.md` — the forty-five valid assertion kinds and their params, plus `when`
  and the `must_all_match`/`must_any_match`/`must_none_match`/`must_have_count` quantifiers.
- `references/analyzers.md` — the eleven valid `analyzer.kind` values and their params.
- `references/examples.md` — worked examples of every rule shape, and the `tests` field.

Every rule has **either** `target`+`assertions` **or** `analyzer` — never both, never neither.
There is no free-text/narrative fallback: an invented `target.kind`, assertion key, or
`analyzer.kind` fails to parse, full stop. There is also no `standard` field — the schema is
`additionalProperties: false`, so an unknown top-level field fails validation too.

## Mapping strategy

Work in this order for every requirement — don't jump straight to hunting for an assertion name:

1. **Subject** — what kind of code element does the requirement talk about (a class, a method, a
   project, a file, "anywhere in the repo")?
2. **Constraint** — what must be true or false about it?
3. **Scope** — what namespace/project/pattern does the source doc actually establish? (See
   "Selector specificity" below — never widen or narrow this yourself.)
4. **Exceptions** the source carves out — these usually become `when` conditions on the same rule,
   not separate rules.
5. **Measurable property** the constraint reduces to (a base type, an attribute, file content, a
   call site...).
6. Pick the **narrowest target** in `references/selectors.md` matching the subject+scope.
7. Pick the **simplest assertion** in `references/assertions.md` that expresses the constraint —
   fall back to `references/analyzers.md` only once no selector+assertion combination works.
8. Before emitting: verify the resulting rule enforces exactly what the source states, no more and
   no less.

Common phrasing → primitive shape (full worked versions in `references/examples.md`):

| phrasing                                    | shape                                                                |
|-----------------------------------------------|--------------------------------------------------------------------|
| "Classes in X must inherit Y"                  | `target: class` (namespace X) + `must_inherit_from`                 |
| "X must not reference project Y"               | `target: project` (name X) + `must_not_reference_project`           |
| "No file matching X may exist"                 | `target: repository` + `must_not_exist` with a nested `file` selector |
| "Every X must have Y"                          | `target: <X's kind>` + `must_have_property`/`must_have_method`/...  |
| "X must contain \<pattern\>"                   | `target: file` + `must_match_content`                                |
| "\<something\> must never happen anywhere"     | `target: repository` + `must_not_exist` with a nested selector (see "Global rule pattern") |

### Global rule pattern (repository + nested selector)

Use `target: { kind: repository }` only when the requirement is repo-wide *and* the actual thing
being checked is expressed through a nested selector inside `must_exist`/`must_not_exist` — e.g.
"no raw `HttpClient` construction anywhere in the Application layer" becomes a `repository` target
asserting `must_not_exist` with a nested `call_site` selector (example 4 in
`references/examples.md`). Don't select a non-`repository` kind like `call_site` directly as the
top-level `target` for an "anywhere in the repo" requirement — that makes call sites the *primary*
match set with no assertions of their own, which isn't the same check.

### Selector specificity

Never choose a broader target or namespace than the source documentation establishes. If a doc
says "API controllers must expose versioned routes" without stating the controllers' namespace,
don't guess one from convention (e.g. `*.Api.Controllers`) — use the scope the doc actually gives
(`*` if none is given), or route the requirement to the appendix if you can't pin down a scope
narrow enough to be meaningful. **Do not guess a namespace or scope from naming conventions the
source doesn't state.**

### Don't overfit examples

Worked examples in the *source* documentation illustrate a requirement; they aren't automatically
normative. If a doc says "domain entities inherit from `Entity<TId>`, for example
`Order : Entity<Guid>`," don't generate a rule requiring `Entity<Guid>` specifically — `Guid` was
the example's concrete value, not a stated constraint. Only bake a concrete value into a rule when
the surrounding text establishes that the value itself is normative, not just illustrative.

### Formalising vs. inventing

Only generate rules for requirements the source documentation actually makes. You may
**translate** plain-language phrasing into an equivalent formal constraint — that's not invention.
For example, "the Domain layer sits at the centre of the architecture and has no dependencies on
Infrastructure" legitimately becomes a `must_not_reference_project`/`must_not_depend_on` rule, even
though the doc never spells out "must not reference." What you must not do is add requirements,
assumptions, or restrictions the text doesn't establish — extra scope, stricter accessibility,
additional required fields, and so on.

## Enforcement classification

Every generated rule needs `enforcement.classification`, and it should be `deterministic` or
`partially_deterministic` — the only two values a rule produced by this workflow should carry:

- `deterministic` — the target+assertions/analyzer combination checks exactly what the source
  requires.
- `partially_deterministic` — the check is a reasonable structural approximation of the
  requirement but is known to miss some cases (say what it misses in `description`).

`ai_review`, `human_review`, and `not_currently_enforceable` exist in the schema, but every rule
the schema allows still needs a working `target`+`assertions` or `analyzer` body — there's no way
to author a valid rule that's *only* a classification with no executable check. A requirement that
genuinely can't be checked this way doesn't become a rule carrying one of those three
classifications; it goes in the "not yet enforceable" appendix instead (see below).

## `illustrative`

`illustrative` marks a rule as demonstrative — a worked example rather than a real, currently
enforced organisational standard (`references/examples.md` shows what that looks like). **Default
to `illustrative: false`** when generating rules from real standards documentation — that's the
common case this skill exists for. Only set it `true` when the source material itself is
explicitly example/sample content rather than a mandatory requirement.

## For every rule

* Create a unique `id` (SCREAMING-KEBAB-CASE) and a clear `name`.
* Set `description` from the source guidance (note any known approximation here too).
* Choose exactly one shape: `target`+`assertions`, or `analyzer`.
* Add a `when` block if the rule should only apply to a subset of matched targets.
* Assign an appropriate `severity` (`info` | `warning` | `error` | `critical`).
* Set `enforcement.classification` per the rules above.
* Include `remediation` guidance where useful.
* Add relevant `tags` and `documentation` references where available.
* Add `metadata.source` when you know which document motivated the rule (see below).
* Add a `tests` block with both a `pass` and a `fail` case (see `references/examples.md`).
* Set `illustrative` per the rules above.
* Set `enabled` to `true` unless the source indicates otherwise.
* Run `rules validate --update-fingerprints` to capture `versionFingerprint` (required, see below) -
  a rule isn't finished until this has been run.

## `metadata.source`

Optional, but add it whenever you can: it's how a human reviewer answers "why does CodeGuard enforce
this?" without re-reading the source documentation. Shape:

```yaml
metadata:
  source:
    document: Architecture Standards
    section: "4.2 Layering"
    statement: Domain projects must not depend on Infrastructure projects.
```

* `document` (required if `source` is present) — the source document's name, free text. Not a file
  path and not validated against anything — just enough for a human to find it.
* `section` — a heading/section reference within that document, if there is one.
* `statement` — **paraphrase** the requirement in your own words; do not quote the source verbatim.
* Omit `metadata` entirely when you don't know the source document (e.g. a rule you're asked to
  generate without a cited document) — don't invent a plausible-sounding one.

## `version` and `versionFingerprint` (required, but never hand-authored)

Every rule needs a `versionFingerprint` to pass `rules validate` — this is checked unconditionally,
not opt-in. It's a sha256 hash of the rule's enforceable body (`target`/`assertions`/`when`/
`analyzer`), computed and written by `rules validate --update-fingerprints`, never something you
hand-author: **never fabricate a `versionFingerprint` value** — a guessed one will simply fail
validation as a mismatch, and even a correctly-formatted fake will be wrong the moment the real body
hashes differently. After writing any new rule, always run
`rules validate --update-fingerprints` to capture it before treating the rule as finished (fold this
into the "Verify before you hand anything over" step below). `version` itself defaults to `1` and
doesn't need to be written explicitly for a brand-new rule. If you're asked to modify an existing
rule's `target`/`assertions`/`when`/`analyzer`, bump `version` yourself, then re-run
`rules validate --update-fingerprints` to capture the new fingerprint for the changed body — don't
attempt to compute or edit `versionFingerprint` directly.

## Verify before you hand anything over

Don't present generated rules as finished until the engine has checked them. All three commands are
deterministic, need no repository, and take seconds:

```bash
codeguard rules validate --rules-source <dir> --update-fingerprints  # captures versionFingerprint for new/changed rules
codeguard rules validate --rules-source <dir>   # schema, known kinds, no duplicate ids, versionFingerprint present and matching
codeguard rules test     --rules-source <dir>   # runs each rule's embedded tests:
codeguard rules analyze  --rules-source <dir>   # rule-set-level problems, see below
```

Run all three against `<dir>` as a whole (the full rule set the generated files land in, not just
the new files in isolation) — `analyze`'s duplicate/conflict detection is only meaningful across the
whole set. All three exit non-zero on failure and support `--format json` for machine-readable
output. Fix whatever `validate`/`test` report and re-run until both are clean — a rule that fails
either is not a finished rule. If `rules test` reports a case as **errored** rather than failed, the
test itself is wrong (an unrecognised `setup:` key, or a `pass` case whose target matches nothing),
not the rule.

Unlike `validate`/`test`, `analyze` inspects the *whole* rule set, so it can report findings that
predate your session and have nothing to do with what you generated — an existing rule set may
already have rules without tests or exact-duplicate groups in it, and its exit code can be non-zero
before you've written anything. **Only act on findings that involve a rule you just generated** —
never attempt to fix, clean up, or even mention a pre-existing finding that only involves rules you
didn't touch; that's not this task. For findings on your generated rule(s):

- **Unreachable assertions** is an authoring bug — fix it and re-verify, same as a `validate`/`test`
  failure.
- **Exact-duplicate rules** against an *already-existing* rule is not a bug to fix silently — it
  means the requirement may already be enforced. Don't delete or merge either rule yourself; drop
  the generated rule from your output (or keep it, your call) and call out the conflict explicitly
  to the human reviewer — see "Report conflicts" below.
- Missing provenance, and illustrative/disabled status, are informational only (they don't fail the
  command on their own) — no action needed even when they concern your generated rule.

If the CLI isn't available in the environment, say so explicitly rather than implying the rules were
verified.

## Report conflicts

If `codeguard rules analyze` flags anything relevant to the rules you just generated (an
exact-duplicate against an existing rule, most commonly), don't fold it silently into the generated
output. Add a short "conflicts" note to your final deliverable — separate from the "not yet
enforceable" appendix below — naming the generated rule, the existing rule it conflicts with, and
what `analyze` reported. A human reviewer decides whether that's an intentional overlap or a
duplicate to drop; you don't decide that for them.

## When a requirement doesn't fit

If a requirement cannot be fully expressed using the primitives in `references/`, do not emit a
rule file for it. List it instead in a "not yet enforceable" appendix: a short markdown table with
columns `id | name | reason it doesn't fit`.

Be strict about what genuinely doesn't fit — the appendix is for requirements the engine *can't*
express, not ones you couldn't immediately find a kind for. All of the following are supported, so
don't route them to the appendix:

| requirement shape | use |
|---|---|
| naming / regex on a name or namespace | `must_match_name`, `must_match_namespace_pattern` |
| file content / text-grep | `must_match_content`, `must_not_match_content` |
| folder or file existence | `must_have_directory`, `must_have_file`, `must_not_have_file`, or a nested `directory`/`file` selector |
| JSON config fields | `must_have_json_field`, `must_not_have_json_field` |
| "never throw X anywhere" | `repository` + `must_not_exist` + nested `throw_site` |
| switch exhaustiveness | nested `switch` selector, or the `exhaustive-switch` analyzer |
| mutating a field that should be immutable | nested `mutation_site` selector |
| catch-clause limits | nested `try_block` selector with `max_catch_clause_count` |
| "compiler warning X must not occur" | nested `diagnostic` selector |
| "exactly/at most N of these" | `must_have_count` |
| "every / at least one / none of these nested things" | `must_all_match` / `must_any_match` / `must_none_match` |
| project dependency allow-lists | `must_only_depend_on` (name framework types explicitly) |
| minimum package versions | `must_use_package_version` with a `">=x.y.z"` constraint |

What still commonly doesn't fit: general YAML/XML config-field checks (only the narrow
`const-yaml-value-consistency` analyzer exists, and only for cross-checking a C# `const` against one
YAML field — there's no generic "assert this YAML/XML field equals X"), cross-file or cross-repo
consistency beyond what an analyzer already implements, and anything requiring genuinely subjective
judgment beyond a structural or textual check.

## Output

Output **one YAML file per rule**, not a single combined document. Each file's content is exactly
one rule document, in the field order shown in `references/examples.md`. Name each file
`{id-lowercased-with-dashes}.yml`.

**The caller determines the destination directory** — infer it from the target repository's
existing rule-file layout if one is visible (for instance, in the CodeGuard repository itself,
existing rules live under `examples/rules/<area>/`, grouped by topic), otherwise ask rather than
inventing a fresh convention. Follow the rule files with the "not yet enforceable" appendix (if
any) and the conflicts note (if any) as a single markdown block, not as rule files.
