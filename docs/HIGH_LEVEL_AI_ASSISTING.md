# CodeGuard — AI-Assisted Rule Authoring & MCP

**Status:** Proposed (Phases 1, 3 and 5 of §27 implemented, plus provenance from Phase 2 — its
lifecycle-state item was considered and rejected, not deferred; see §6, §9-§14, §19, §27)
**Version:** 1.5
**Scope:** CodeGuard rule authoring, validation, testing and AI integration

> **All YAML and JSON in this document is real, current CodeGuard syntax**, checked against the
> engine. Earlier drafts used illustrative shapes (a `rule:` wrapper, `metadata:`, `type`/`value`
> assertion pairs, `**/*.csproj` globs) that do not parse. An LLM reading this document will copy
> what it sees, so anything invented here becomes a hallucinated rule later. If you extend this
> doc, verify new examples with `codeguard rules validate` first — and where a section describes
> something that does not exist yet, say so explicitly rather than showing plausible syntax for it.

---

## 1. Overview

CodeGuard is a deterministic engineering policy engine. Rules are defined declaratively using YAML and executed against software repositories to determine whether those repositories comply with organisational engineering standards.

The primary challenge is no longer the execution of rules. It is the **creation and maintenance of high-quality rules**.

Organisational engineering standards are generally expressed in natural language across:

* Architecture documentation
* Coding standards
* ADRs
* Confluence pages
* Markdown documentation
* Developer guides
* Security standards
* Testing standards
* Platform documentation

Large Language Models (LLMs) are well suited to interpreting this documentation and identifying potential deterministic requirements.

However, CodeGuard should **not become an AI-powered policy engine**.

Instead, the proposed architecture separates responsibilities:

> **LLMs understand and author policy. CodeGuard deterministically validates, tests, executes and enforces policy.**

CodeGuard will expose its rule-authoring and validation capabilities through both its CLI and an MCP interface, allowing AI agents to interact directly with the deterministic engine.

---

# 2. Goals

The system should:

1. Allow LLMs to efficiently create CodeGuard rules from organisational documentation.
2. Ensure generated rules use only capabilities actually supported by CodeGuard.
3. Validate generated rules deterministically.
4. Allow rules to contain executable tests.
5. Provide deterministic feedback to an LLM when a generated rule is invalid or its tests fail.
6. Provide provenance between a rule and the organisational documentation that motivated it.
7. Allow AI agents to discover CodeGuard's available rule capabilities.
8. Expose these capabilities through MCP.
9. Preserve CodeGuard's deterministic nature.
10. Make human approval of organisational policy explicit.
11. Detect mechanically identifiable rule conflicts, duplication and ambiguity.
12. Avoid coupling CodeGuard to a specific LLM provider.

---

# 3. Non-Goals

CodeGuard will not:

* Parse organisational documentation using an embedded LLM.
* Choose which LLM provider is used.
* Manage LLM API keys.
* Implement an autonomous AI agent.
* Decide organisational policy.
* Treat an LLM's interpretation as authoritative.
* Replace GitHub Actions or Azure DevOps.
* Replace Sonar, Stryker, Wiz, SOOS or other specialist tools.
* Become a general-purpose workflow engine.

The LLM exists **outside the deterministic CodeGuard engine**.

---

# 4. Core Architectural Principle

The central architectural principle is:

> **Natural language is interpreted by AI; executable policy is interpreted by CodeGuard.**

The resulting pipeline is:

```text
                  Organisation
                   Documentation
                        |
                        v
              +-------------------+
              |    AI / Agent     |
              |                   |
              | Understands text  |
              | Extracts policy   |
              | Authors rules     |
              +---------+---------+
                        |
                  Candidate Rule
                        |
                        v
              +-------------------+
              |     CodeGuard     |
              |                   |
              | Validate          |
              | Test              |
              | Explain           |
              | Execute           |
              +---------+---------+
                        |
                        v
                Deterministic
                   Results
                        |
                        v
              +-------------------+
              |    AI / Agent     |
              +-------------------+
```

The AI can iterate based on CodeGuard's deterministic feedback.

---

# 5. Rule Authoring Lifecycle

A recommended rule lifecycle is:

```text
Documentation
     |
     v
AI extraction
     |
     v
Rule Candidate
     |
     +---- Source provenance
     |
     +---- Determinism assessment
     |
     +---- Proposed rule
     |
     +---- Tests
     |
     v
CodeGuard validation
     |
     +---- FAIL ---> AI revises
     |
     v
Rule tests
     |
     +---- FAIL ---> AI revises
     |
     v
Human review
     |
     v
Approved Rule
     |
     v
Repository enforcement
```

This deliberately distinguishes between a **candidate rule** and an **approved organisational rule**.

---

# 6. Rule Candidates

AI-generated rules should contain sufficient metadata to explain where they came from.

**Implemented, in a deliberately narrower shape than earlier drafts of this section proposed.**
`metadata.source` is real schema now — but it's `{document, section, statement}` only, with no
`generation` block, and singular (`source`, not a `sources` list). Both cuts were deliberate design
decisions, not omissions: a prior field with the same intent
(`RuleDefinition.Standard`, see `docs/IMPLEMENTATION_STATUS.md`) was removed after two incompatible
authoring conventions collided across hand-authored vs. generated rules, so this one gives `document`/
`section` **no structure to be inconsistent about** — they're free text, never resolved against a real
file or used for grouping/lookup by the engine. `statement` is a paraphrase in the rule author's own
words, not a verbatim quote — matching the register `description`/`remediation` already use, and
deliberately not adding more of a source company's original standards-doc prose to what's committed
in `examples/rules/` (see CLAUDE.md on that directory's content). `generation.method` (ai vs. human)
and multi-source support were both considered and deferred rather than bundled in — each is its own
convention to get right, and folding them in here risked the exact "one field, several ideas of what
it means" problem this was designed to avoid.

Example (real, valid syntax end to end):

```yaml
id: ARCHITECTURE-DOMAIN-NO-INFRASTRUCTURE-001
name: Domain projects must not reference Infrastructure
description: >
  Prevent Domain projects from directly referencing Infrastructure projects.
severity: error

enforcement:
  classification: deterministic

tags:
  - architecture

illustrative: false

metadata:
  source:
    document: Architecture Standards
    section: Layering Rules
    statement: >
      Domain projects must not depend on Infrastructure projects.

target:
  kind: project
  name: "*.Domain"

assertions:
  - must_not_reference_project:
      name: "*Infrastructure*"

tests:
  - name: Domain project without an Infrastructure reference
    setup:
      projects:
        - name: Contoso.Domain
    expect: pass

  - name: Domain project referencing Infrastructure
    setup:
      projects:
        - name: Contoso.Domain
          projectReferences: [Contoso.Infrastructure]
        - name: Contoso.Infrastructure
    expect: fail
```

Note the shapes that differ from earlier drafts of this document, because they are the ones an LLM
is most likely to get wrong:

* There is **no `rule:` wrapper** — `target` and `assertions` are top-level keys.
* A target is `{ kind: project, name: "<glob>" }`, not a list of `.csproj` paths.
* Globs support `*` (one path segment), `**` (zero or more full path segments, used as a whole
  segment — e.g. `**/Foo.cs`), and `?` (one character). No character classes (`[abc]`) or brace
  expansion (`{a,b}`).
* Each assertion is a **single-key map** (`must_not_reference_project:` → its params). The schema
  enforces exactly one key per entry, so a `{type:, value:}` pair fails validation.
* `must_not_reference_project` is one of the 45 real assertion kinds; `forbidden-reference` is not a
  kind. Run `codeguard rules discover` (once implemented — §12) or read
  `skills/codeguard-rule-generation/references/assertions.md` for the current list.

The important principle is that the rule retains **provenance**.

---

# 7. Deterministic vs Non-Deterministic Requirements

The AI rule-authoring process should classify extracted requirements.

For example:

### Deterministic

> Domain projects must not reference Infrastructure.

This can directly map to a CodeGuard assertion.

### Potentially deterministic

> All HTTP APIs must use the organisation's standard error response.

This may be deterministic if CodeGuard has a suitable representation of API responses.

### Non-deterministic

> Business logic should remain clean and maintainable.

This is currently not a deterministic CodeGuard rule.

The AI should therefore produce something similar to the following. Note this is **not a rule file**
— it is an entry in the agent's own "not yet enforceable" report (see §8 and §17). It is not valid
rule YAML and must never be written to the rules directory:

```yaml
statement:
  text: Business logic should remain clean and maintainable.

assessment:
  deterministic: false
  reason: >
    "Clean" and "maintainable" are not currently represented by
    deterministic CodeGuard assertions.
```

The rule schema's `enforcement.classification` field already exists and carries the in-rule half of
this distinction (`deterministic`, `partially_deterministic`, `ai_review`, `human_review`,
`not_currently_enforceable`). A requirement classified as non-deterministic does not become a rule
carrying one of the latter values — every valid rule still needs an executable `target`+`assertions`
or `analyzer` body — so it belongs in the report above instead.

The system should **not invent an executable rule merely because the source statement sounds important**.

---

# 8. Ruleability Analysis (an AI activity, not an engine command)

This belongs to the **rule-authoring skill's output**, not to CodeGuard. Classifying prose
requirements means reading Confluence pages and architecture documents, which the engine cannot and
should not do — per §3, CodeGuard never parses organisational documentation. Do not implement this
as a `codeguard` command; specify it as a report the agent produces alongside the generated rules
(see §17).

The skill should analyse organisational standards and determine how much of them can currently be
enforced.

Example output:

```text
Engineering Standards Analysis

Statements analysed: 143

Deterministically enforceable:       61
Potentially enforceable:             27
Currently non-deterministic:         55

Potential rule candidates:            61
Potential policy conflicts:            4
Potential duplicate requirements:      7
```

This creates a useful secondary capability:

> CodeGuard can identify gaps between organisational engineering policy and machine-enforceable policy.

---

# 9. CodeGuard CLI

The CLI should remain the primary local interface.

Existing commands should remain compatible.

**Already implemented** — these ship today and must stay backward-compatible:

```bash
codeguard validate        # evaluate a repository (--format console|json|sarif|html)
codeguard rules validate  # structural validation of a rule set, plus a non-fatal "Rule analysis:"
                          # section (--format console|json; Tier 1 + opportunistic Tier 2 findings,
                          # see §14 - this used to be a separate `rules analyze` command)
codeguard rules test      # run rules' embedded tests: cases (--format console|json)
codeguard rules list      # (--format table|json)
codeguard rules explain   # --format console|json; see §13
codeguard rules discover  # --format console|json|markdown; see §12
codeguard setup           # configure the rule source
codeguard info            # show the resolved rule source and counts
```

**Proposed** — nothing left in this document remains fully unimplemented in the CLI itself; the
remaining gaps are the rule `metadata` schema (§6/§19) and MCP (§15).

Note the command group is nested (`codeguard rules <verb>`), not flat.

---

# 10. `codeguard rules validate`

Validates a rule set's structure and semantics: schema conformance, known selector/assertion/
analyzer kinds, required parameters, and no duplicate rule IDs. It does **not** evaluate rules
against a repository — that is the top-level `codeguard validate`.

This exists today. Like `rules test`, it operates on a rule **source directory**, not a single file:

```bash
codeguard rules validate --rules-source examples/rules
```

Actual output:

```text
Checked 125 rule files: 125 passed, 0 failed.
```

and on failure, the offending file followed by its errors:

```text
Checked 125 rule files: 124 passed, 1 failed.

/abs/path/ddd-042.yml
  - /assertions/0: Unknown assertion kind 'business-logic-quality'.
```

Accepting a single file path would be a reasonable addition, since that is the natural granularity
for an agent iterating on one generated rule.

Failures should be structured and machine-readable.

**Implemented.** `codeguard rules validate --format json` emits each error as a structured object,
not a bare string — `RuleFileIssue.Errors` is `IReadOnlyList<RuleValidationError>`
(`src/CodeGuard.Configuration/Validation/RuleValidationError.cs`), a
`record(Code, Path, Message)` with a stable, closed set of codes (`RuleErrorCodes`:
`SCHEMA_VIOLATION`, `UNKNOWN_SELECTOR_KIND`, `UNKNOWN_ASSERTION_KIND`, `UNKNOWN_ANALYZER_KIND`,
`INVALID_PARAMETER`, `DUPLICATE_RULE_ID`, `UNREADABLE_RULE_FILE`, `PARSE_ERROR`):

```json
{
  "sourceFile": "/abs/path/ddd-042.yml",
  "errors": [
    {
      "code": "UNKNOWN_ASSERTION_KIND",
      "path": "/assertions/0",
      "message": "Assertion kind 'business-logic-quality' is not supported."
    }
  ]
}
```

`RuleSchemaValidator` assigns `SCHEMA_VIOLATION` from the JSON-pointer path it already computes
(`detail.InstanceLocation`); `RuleParsingException.ToValidationError()` and `RuleFileLoader` assign
the rest at their respective throw sites. Console output still renders the old `"path: message"`
prose via `RuleValidationError.ToString()`, so this was purely additive for JSON consumers.

This is particularly important for AI consumption.

---

# 11. `codeguard rules test`

**This already exists.** It is designed in `docs/RULES_TEST_DESIGN.md` and implemented end-to-end:
each `tests:` case's `setup:` builds a virtual analysis model (no disk, no Roslyn, no MSBuild) which
runs through the same `RuleEvaluator` as `codeguard validate`. 117 of this repo's 125 example rules
carry tests; the 8 that don't are analyzer-backed, which the virtual setup path can't drive.

It operates on a rule **source directory**, not a single file path:

```bash
codeguard rules test --rules-source examples/rules
codeguard rules test --rules-source examples/rules --rule DDD-ENTITY-001   # repeatable
```

Actual output:

```text
Rule tests

✓ ARCH-DOMAIN-001  2/2 passed
✗ DDD-ENTITY-001  1/2 passed
  ✗ Entity without required base type
      Expected at least one violation but the rule passed.

Tests: 4  Passed: 3  Failed: 1
```

Machine-readable output (`--format json`) is a flat result list, not grouped per rule:

```json
{
  "total": 4,
  "passed": 3,
  "failed": 1,
  "errored": 0,
  "results": [
    { "ruleId": "DDD-ENTITY-001", "testName": "Valid entity", "outcome": "passed", "failureReason": null },
    { "ruleId": "DDD-ENTITY-001", "testName": "Entity without required base type", "outcome": "failed",
      "failureReason": "Expected at least one violation but the rule passed." }
  ]
}
```

A third outcome, `errored`, means the test itself is malformed rather than the rule being wrong —
an unrecognised `setup:` key, or an `expect: pass` case whose target matches no candidates (which
would otherwise pass vacuously without running any assertion).

Remaining optional work: accepting a single file path, and grouping JSON output per rule.

This creates a deterministic feedback loop for AI rule generation.

---

# 12. `codeguard rules discover`

This command exposes the capabilities available to a rule author.

**Implemented**, including its prerequisite — this was the largest single item in this document.
`ISelectorParser`/`IAssertionParser`/`IAnalyzerParser` each now expose a
`CapabilityDescriptor Descriptor` (`src/CodeGuard.Configuration/Capabilities/CapabilityDescriptor.cs`:
`Kind`, `Summary`, `Parameters` — each a `ParameterDescriptor` with `Name`/`Type`/`Required`/
`Summary`/`Default`/`AllowedValues` — plus `Produces`/`AppliesTo` `CandidateKind` metadata used by
§14's Tier 2 checks), not just a bare `Kind` string. All 77 parser classes (21 selectors, 45
assertions, 11 analyzers) implement it. `CapabilityCatalog.Create()` aggregates every registry's
descriptors, and `tests/CodeGuard.Configuration.Tests/Capabilities/CapabilityCatalogTests.cs` fails
the build if a parser and its descriptor ever drift apart (registered kinds must exactly match
descriptor kinds).

The same descriptors **do generate** `skills/codeguard-rule-generation/references/{selectors,assertions,analyzers}.md`
via `scripts/sync-skill-references.sh` (which calls `rules discover --format markdown --section <x>`
and splices the result between generated-marker comments), enforced in CI (`ci.yml` re-runs the
script and diffs `skills/`). **One exception**: `references/examples.md` is not derivable from
descriptors and remains hand-maintained.

```bash
codeguard rules discover
```

```text
Target selectors (21)
  call_site  [yields CallSite]
    Invocations, object creations and member accesses matching the given filters.
      site_kind: invocation | object_creation | member_access
      invoked_member: glob
      ...
  class  [yields Type]
    Classes in a matching namespace.
      namespace: glob
  ...

Assertions (45)
  ...

Analyzers (11)
  ...
```

`--format json` and `--format markdown --section selectors|assertions|analyzers` are both
supported (`src/CodeGuard.Cli/Commands/Rules/DiscoverCommand.cs`,
`src/CodeGuard.Cli/Support/CapabilityReportWriter.cs`):

```bash
codeguard rules discover --format json
```

The purpose is to give AI agents a reliable description of the **actual CodeGuard rule vocabulary**.

This reduces hallucinated assertions and unsupported rule concepts.

---

# 13. `codeguard rules explain`

Provides structured information about an existing rule.

Example:

```bash
codeguard rules explain DDD-042
```

**Implemented.** `codeguard rules explain <id> --format json` (`src/CodeGuard.Cli/Commands/Rules/ExplainCommand.cs`)
emits the rule's metadata plus the **source document converted YAML→JSON**, exactly as this section
originally proposed: since assertion parameter values can't be recovered from the in-memory model
(`IAssertion` exposes only `Kind`; constructor arguments vanish into private fields — see §12), the
`document` field is produced via a new `RuleFileLoader.ReadDocument`, reusing the existing
`YamlDocumentReader` rather than adding introspection to every assertion. Real output:

```json
{
  "id": "DDD-ENTITY-001",
  "name": "Domain entities must inherit from Entity",
  "description": "All domain entities must inherit from the approved Entity<TId> base class.",
  "severity": "error",
  "enforcement": { "classification": "deterministic" },
  "tags": ["ddd", "domain", "entity"],
  "remediation": "Inherit from Contoso.Domain.Entity<TId>.",
  "documentation": [],
  "enabled": true,
  "illustrative": true,
  "shape": "declarative",
  "testCount": 2,
  "sourceFile": "/abs/path/examples/rules/ddd/ddd-entity-001.yml",
  "document": {
    "target": { "kind": "class", "namespace": "Contoso.Domain.Entities" },
    "assertions": [ { "must_inherit_from": { "type": "Contoso.Domain.Entity<*>" } } ],
    "tests": [ "..." ]
  }
}
```

`source` (document/section provenance) still doesn't appear here — that's blocked on §6/§19's
`metadata` block, which remains unimplemented (see below).

This is useful both for developers and AI agents.

---

# 14. `codeguard rules analyze`

**Implemented — Tier 1 and Tier 2, exactly as scoped below.** Tier 3 remains explicitly out of
scope, per this section's own original guidance.

**Note:** the standalone `rules analyze` command described in this section was later folded into
`rules validate` as an additional, always-non-fatal "Rule analysis:" report section — see §9/§10
and `docs/IMPLEMENTATION_STATUS.md`'s corresponding entry for why (the same "fold non-fatal
supplementary checks into `validate`" precedent `RuleSourceChecker` set — see
`docs/done/RULE_SOURCE_AND_LINKED_DOCUMENTATION.md`). The Tier 1/2/3 design below is otherwise
still accurate; it now describes a report section rather than a separate command, and the
underlying `RuleSetAnalyzer`/`RuleAnalysisReport` types are unchanged.

This capability analyses a rule collection for mechanically detectable problems, beyond what `rules
validate`'s own structural checks cover — a rule set can be 100% structurally valid and still have
these findings, so a non-empty "Rule analysis:" section isn't the same signal as `rules validate`
failing its exit code.

These checks are **not** of comparable cost, and listing them as one bullet list would have led to
them being treated as one piece of work. They split into three tiers:

**Tier 1 — mechanically provable, cheap.**

* Invalid rules and duplicate IDs — reused directly from `RuleFileLoader.ValidateDirectories`
  (`context.ValidateRules()`), split back apart by `RuleErrorCodes.DuplicateRuleId`.
* Missing tests — `rule.Tests.Count == 0`.
* One-sided tests — a rule with only `pass` cases or only `fail` cases (distinct from the existing,
  narrower vacuous-test guard in `RuleTestRunner`). As of this writing all 117 example rules that
  carry `tests:` already have both, so this check has found nothing yet in this repo's own rule set
  — it's there for the next rule that gets it wrong.
* Missing provenance — implemented now that §6/§19's `metadata.source` exists
  (`RuleAnalysisReport.RulesMissingProvenance`). Also excluded from `HasFindings`, same reasoning as
  disabled/illustrative below — `metadata.source` is optional, additive documentation, not a
  requirement, so all 125 of this repo's own example rules currently lacking it isn't a problem.
* Disabled rules; `illustrative: true` rules — counted and listed, but deliberately excluded from
  what makes the command exit non-zero (`RuleAnalysisReport.HasFindings`), since a rule set
  legitimately containing them — like this repo's own `examples/rules/`, all illustrative — isn't
  itself a problem.

**Tier 2 — needed the §12 descriptor layer, which has since shipped.**

* Unreachable rules — a top-level assertion whose `CapabilityDescriptor.AppliesTo` doesn't include
  the target selector's `Produces` kind. Assertions with an empty `AppliesTo` (the quantifier/
  existence kinds — `must_all_match`/`must_any_match`/`must_none_match`/`must_have_count`/
  `must_exist`/`must_not_exist`, which evaluate a nested selector rather than the outer candidate)
  are never flagged, by design. **Known limitation, not fixed**: this only checks the rule's
  top-level assertions — it doesn't recurse into those quantifier assertions' own nested
  `assertions:`, because the nested selector/assertion objects aren't recoverable from the parsed
  model (only `Kind` survives parsing, the same limitation §13 works around for parameter values).
* Exact-duplicate rules (identical `target`+`assertions`, or identical `analyzer`) — reads the raw
  source document via `RuleFileLoader.ReadDocument` rather than the parsed model, for the same
  reason, and canonicalizes (recursively key-sorts) it first so two rules that differ only in
  parameter order still compare equal. Found real, true-positive duplicates in this repo's own
  `examples/rules/` on first run (e.g. `ARCH-DEPENDENCY-002`/`ARCH-DEPENDENCY-005` share an
  identical enforceable body despite different names/tests/descriptions — each demonstrates the
  same `must_not_depend_on` rule through a different code shape).

**Tier 3 — research, not scheduled work. Do not promise these.**

* Overlapping selectors and conflicting assertions. Proving that two glob-scoped selectors overlap,
  or that two assertions contradict, needs glob subsumption plus an assertion negation algebra.
* "Rules whose tests don't exercise the intended assertion" in its full form is mutation testing
  (§22), not a static check.

Real output against this repo's own `examples/rules/`:

```text
CodeGuard Rule Analysis

Rules:                    125
Invalid:                  0
Duplicate ids:            0
Rules without tests:      8
One-sided tests:          0
Unreachable assertions:   0
Exact-duplicate rules:    5
Disabled rules:           0
Illustrative rules:       125
Missing provenance:       125
```

(The 8 without tests are the analyzer-backed rules the virtual test-setup path can't drive — see
CLAUDE.md's rules-directory notes; expected, not a defect.)

This is **not** intended to replace semantic AI analysis.

It identifies problems that CodeGuard can prove mechanically.

---

# 15. MCP Interface

CodeGuard should expose these capabilities through MCP.

The initial MCP surface should be intentionally small.

Potential tools:

```text
codeguard.validate_repository
codeguard.list_rules
codeguard.get_rule
codeguard.validate_rule
codeguard.test_rule
codeguard.discover_capabilities
codeguard.explain_rule
codeguard.analyze_rules
```

The exact MCP naming should follow the conventions of the implementation.

The important principle is that the MCP server exposes **CodeGuard's deterministic capabilities**, not an AI agent.

---

# 16. AI Rule Authoring Through MCP

An AI agent can then perform the following workflow:

```text
Agent
 |
 | discover capabilities
 v
CodeGuard
 |
 | supported assertions
 v
Agent
 |
 | analyse documentation
 |
 | generate candidate rule
 v
CodeGuard
 |
 | validate rule
 v
Agent
 |
 | fix errors
 v
CodeGuard
 |
 | test rule
 v
Agent
 |
 | fix failing tests
 v
Human approval
```

The AI therefore has access to a deterministic compiler/test cycle.

---

# 17. Example Agent Workflow

The rule-authoring skill could instruct the agent:

```text
1. Analyse the supplied engineering documentation.

2. Identify normative statements.

3. For each statement, determine whether it can be
   represented using CodeGuard's available capabilities.

4. Use CodeGuard capability discovery before generating
   assertions.

5. Generate candidate rules.

6. Include source provenance.

7. Generate tests for each rule.

8. Run CodeGuard rule validation.

9. Correct any validation failures.

10. Run CodeGuard rule tests.

11. Correct failing tests.

12. Identify requirements that remain non-deterministic.

13. Identify potential policy conflicts.

14. Produce the resulting candidate rules for human review.
```

The skill remains responsible for the semantic interpretation.

CodeGuard remains responsible for deterministic verification.

---

# 18. Human Approval

AI-generated rules should not automatically become organisational policy.

The recommended lifecycle is:

```text
Generated
    ↓
Validated
    ↓
Tested
    ↓
Review
    ↓
Approved
    ↓
Enforced
```

Possible metadata (depends on the same unimplemented `metadata` block as §19):

```yaml
metadata:
  lifecycle:
    status: proposed
```

followed by:

```yaml
metadata:
  lifecycle:
    status: approved
```

The precise approval mechanism can be implemented later.

For an MVP, Git pull requests are sufficient.

---

# 19. Rule Provenance

Every rule should ideally be traceable to its source.

**Implemented** — see §6 for the settled shape and the reasoning behind cutting it down from earlier
drafts. `RuleDefinition.Metadata?.Source` (`CodeGuard.RuleModel.Rules.RuleMetadata`/`RuleSource`)
carries `Document`/`Section`/`Statement`, all optional except `Document`. The existing
`documentation` field (`array<string>`) is unrelated and still unused by any example rule — it was
always too weak to carry structured document/section/statement provenance, which is why this is a
new field rather than a reinterpretation of that one.

Real shape:

```yaml
metadata:
  source:
    document: Architecture Standards
    section: "4.2 Layering"
    statement: Domain projects must not depend on Infrastructure.
```

`document`/`section` are free text — never resolved against a real file, never used by the engine
for grouping or lookup (see §6 for why). `codeguard rules explain --format json` surfaces it under
`metadata.source`, and `codeguard rules validate`'s "Rule analysis:" section counts rules missing
it (informational only — see §14; a rule set legitimately having none, like this repo's own
`examples/rules/`, isn't a problem).

This provides an answer to:

> Why does CodeGuard enforce this?

The answer becomes:

> Because the organisation's Architecture Standards document explicitly requires it.

This is important for trust and adoption.

---

# 20. Policy Conflicts

CodeGuard should eventually detect mechanically identifiable conflicts.

For example:

```text
Architecture Standard A:
Domain → Infrastructure is prohibited.

Architecture Standard B:
Domain services may use Infrastructure helpers.
```

The AI may identify this semantic conflict, while CodeGuard can potentially identify some structural conflicts between the resulting rules.

The responsibilities should remain:

```text
AI:
  semantic policy conflict detection

CodeGuard:
  mechanically provable rule conflict detection
```

---

# 21. Rule Testing as a First-Class Concept

**This is already the case** — see §11. Embedded `tests:` are implemented, and 117 of this repo's
125 example rules carry them, each with both a `pass` and a `fail` case. What follows describes the
existing model rather than a proposal.

Rules should be treated similarly to production code.

A rule without tests should be considered lower confidence.

The rule authoring workflow therefore becomes:

```text
Rule
 +
Tests
 +
Provenance
 =
Candidate policy
```

The existing embedded rule-test approach is particularly valuable here because an AI can generate both the implementation and its evidence.

---

# 22. Future: Rule Mutation Testing

A potential future capability is mutation testing for rules.

The objective is to determine whether tests actually demonstrate that a rule behaves as intended.

For example:

```text
Rule:
Domain projects cannot reference Infrastructure.

Test:
Infrastructure reference → violation
```

CodeGuard could mutate the fixture:

```text
Infrastructure reference removed
        ↓
Expected: pass

Infrastructure reference added
        ↓
Expected: violation
```

This could provide a higher level of confidence in AI-generated rules.

The existing rule-test architecture does make this inexpensive — a test case's `setup:` is held as a
raw JSON object, so mutating a fixture and re-evaluating is straightforward.

It should still not be part of the initial implementation. The one-sided-test check in §14's Tier 1
catches most of the same failures for a fraction of the cost, and should be shown to be insufficient
before this is scheduled.

---

# 23. Architecture

The proposed architecture is:

```text
                     +----------------------+
                     |   AI Coding Agent    |
                     |                      |
                     | Claude / Codex / etc |
                     +----------+-----------+
                                |
                         MCP / CLI
                                |
                                v
                 +---------------------------+
                 |         CodeGuard         |
                 |                           |
                 | Rule Discovery            |
                 | Rule Validation           |
                 | Rule Testing              |
                 | Rule Explanation          |
                 | Rule Analysis             |
                 | Repository Validation     |
                 +-------------+-------------+
                               |
              +----------------+----------------+
              |                |                |
              v                v                v
          Rule Engine       Rule Tests      Repository
              |                                |
              v                                v
          Findings                         Findings
```

No LLM dependency exists inside CodeGuard.

---

# 24. Security and Trust

This architecture provides an important security property.

The organisation does not need to trust an LLM to make runtime compliance decisions.

Instead:

```text
LLM
 ↓
proposes policy
 ↓
human approves
 ↓
CodeGuard
 ↓
deterministic enforcement
```

The LLM can be replaced without changing the enforcement engine.

The organisation can also run CodeGuard completely offline or without access to an LLM.

---

# 25. Benefits

### Determinism

The enforcement mechanism remains deterministic.

### AI compatibility

Any capable AI agent can author rules.

### Provider independence

CodeGuard does not depend on OpenAI, Anthropic, Google or another provider.

### Better rule quality

AI gets deterministic feedback from the actual rule engine.

### Reduced hallucination

Capability discovery tells the AI what CodeGuard actually supports.

### Traceability

Rules can retain their source documentation.

### Testability

Generated rules can be tested before adoption.

### Maintainability

CodeGuard remains a focused engineering tool rather than becoming an AI platform.

### Extensibility

New AI agents can consume CodeGuard without CodeGuard itself changing.

---

# 26. Risks

## AI generates poor rules

Mitigation:

* Rule validation
* Embedded tests
* Provenance
* Human approval
* Determinism classification

## AI invents unsupported assertions

Mitigation:

* `rules discover`
* Machine-readable capability schema
* Strict rule validation

## Too many low-value rules

Mitigation:

* Require explicit source provenance
* Human approval
* Rule analysis
* Rule quality criteria

## Rules become stale

**Implemented** (`docs/done/RULE_SOURCE_AND_LINKED_DOCUMENTATION.md`, `docs/IMPLEMENTATION_STATUS.md`
"Post-v1 addition: `metadata.source.file`/`fingerprint` + `rules validate` drift warnings") - the
workflow originally sketched here:

```text
Source documentation changed
        ↓
Potentially affected rules
        ↓
Review required
```

ships as: a rule opts in with `metadata.source.file` (+ optional `section`, to scope the check to one
heading rather than the whole document) and a captured `fingerprint`; `codeguard rules validate`
resolves the link on every run and warns (never fails the build - see "No Automatic Decisions" in the
design doc) when the fingerprint no longer matches, printing the recorded statement next to the
current content as evidence for a human to review; `rules validate --update-fingerprints` resolves a
reviewed warning by recomputing and writing the new fingerprint back into the rule file, editing only
that value in place. Cut from this pass: moved-section detection (relocating a renamed heading by
searching for the recorded statement verbatim - would require a verbatim-text field, and `statement`
is deliberately a paraphrase, not a verbatim quote, for reasons the design doc covers) and non-
repo-local sources (Confluence/SharePoint etc. - scope stays repository-local Markdown for now).

## CodeGuard becomes too complex

Mitigation:

Keep semantic interpretation outside CodeGuard.

---

# 27. Recommended Implementation Order

### Phase 0 — Correct what is already broken

Before adding anything: this document's examples must parse (§6), the skill's reference docs must
match the engine's actual vocabulary, rule tests must not be able to pass vacuously, and CI must
actually run the shipped rule set. None of this depends on new architecture.

### Phase 1 — Capability descriptors, then the authoring primitives — **DONE**

`rules validate`, `rules test`, `rules list` and `rules explain` already existed (§9). All four
items below have since landed:

1. ✅ Declarative parameter descriptors on all 77 selector/assertion/analyzer parsers (§12), guarded
   against drift by `CapabilityCatalogTests`.
2. ✅ `codeguard rules discover` (§12), including a `markdown` format wired into
   `scripts/sync-skill-references.sh` and CI so the skill's reference docs can no longer drift
   (except `examples.md`, still hand-maintained — see §12).
3. ✅ Structured validation errors (§10) — `RuleValidationError(Code, Path, Message)`.
4. ✅ `codeguard rules explain --format json` (§13).

One consequence worth noting for sequencing Phase 3: descriptors carrying `Produces`/`AppliesTo`
means §14's Tier 2 "unreachable rules" check is now cheap and doesn't itself need anything from
Phase 2 — only "missing provenance" and "exact-duplicate rules" (the latter for an unrelated reason,
see §14) have real dependencies left.

### Phase 2 — Rule metadata

Introduce:

* provenance — **done**. `metadata.source: {document, section, statement}`, deliberately narrower
  than this document originally sketched (no `generation` block, no multi-source list) — see §6/§19
  for the shape and the reasoning. No backfill of the 125 existing example rules.
* lifecycle state — **considered and deliberately rejected**, not merely deferred.
  `docs/REFACTORING.md` §12 proposed `status: experimental|active|deprecated|retired` (plus a
  separate `version` field with diagnostic-level version stamping, itself a materially bigger,
  cross-cutting change touching the evaluator and every `IViolationReporter` — that half was never
  in scope for this decision either way). Once scoped down to "just `status`, purely informational,
  no `rules analyze` check yet" — the same discipline `metadata.source` went through — it became
  clear the field would have **zero consumers**: nothing would read it, filter by it, or surface it
  anywhere, unlike `metadata.source` which `rules explain --format json` and `rules analyze` both
  make use of. `enabled` and `illustrative` already cover the on/off and real-vs-demonstrative axes;
  a third "maturity" axis with no consumer wasn't worth the schema surface. Don't re-propose this
  without first identifying a concrete consumer (a command that reads it, a check that flags on it)
  — the shape questions (field location, whether it affects evaluation) are secondary to that.
* test metadata, deterministic capability metadata — not started; this document never specified a
  concrete shape for either, so there's nothing yet to implement.

### Phase 3 — Rule analysis — **DONE**

```bash
codeguard rules analyze
```

Tier 1 and Tier 2 from §14 both implemented, ahead of Phase 2 as anticipated — only the "missing
provenance" line item still needs it, and remains skipped. Tier 3 is explicitly out of scope.

**Later folded into `rules validate`** as a "Rule analysis:" report section — see §14's note and
`docs/IMPLEMENTATION_STATUS.md`. The `codeguard rules analyze` command shown above no longer
exists; the checks themselves are unchanged.

### Phase 4 — MCP

Seven of the eight tools below (everything except `validate_repository`) need no MSBuild and no
Roslyn — they read rule files and run the evaluator against virtual models. The MCP server should
therefore depend only on the configuration/core/evaluation projects and stay fast to start.
`validate_repository` is the exception: it loads real solutions through MSBuild, must register
`MSBuildLocator` exactly once per process, and takes minutes on a large repository. Give it a
separate host or run it out-of-process rather than making the whole server heavy.

Expose the deterministic capabilities:

```text
list_rules
get_rule
validate_rule
test_rule
discover_capabilities
explain_rule
analyze_rules
validate_repository
```

(`analyze_rules` would now be exposed as part of `validate_rule`'s output rather than a separate
tool, per the `rules analyze` → `rules validate` merge described in §14's note — naming here is
still provisional, per the note below.)

(This previously listed only six of the eight tools named in §15 while still saying "the eight
tools below" — `list_rules`/`get_rule` were missing. Both are as cheap as the other non-
`validate_repository` tools: `list_rules` is `rules list`, needs no MSBuild/Roslyn. `get_rule` likely
overlaps heavily with `explain_rule` — `rules explain` already returns a rule's full metadata plus
its source document — so whoever implements this should confirm whether `get_rule` earns a separate
tool or `explain_rule` alone covers it before building both.)

### Phase 5 — Improve the existing AI skill — **DONE**

Update the rule-authoring skill to:

1. ✅ discover CodeGuard capabilities — via the CI-synced `references/*.md` tables (Phase 1), not a
   live `rules discover` call per invocation; the skill's own vocabulary can't drift from the engine.
2. ✅ analyse documentation — "Mapping strategy" section.
3. ✅ generate candidates — same section, plus "Formalising vs. inventing"/"Selector specificity"
   guardrails.
4. ✅ generate tests — "For every rule" requires a `tests` block with both a `pass` and `fail` case.
5. ✅ validate — `codeguard rules validate` in "Verify before you hand anything over".
6. ✅ test — `codeguard rules test`, same section.
7. ✅ iterate — "fix whatever they report and re-run until clean", now extended to all three
   verification commands (see item 9).
8. ✅ report non-deterministic requirements — the "not yet enforceable" appendix, plus
   `enforcement.classification`.
9. ✅ report conflicts — was the one gap: the skill verified with `validate`/`test` but never ran
   `codeguard rules analyze`, so it could produce rules that were exact duplicates of existing ones
   or contained unreachable assertions without ever finding out. Now wired in as a third
   verification step, with explicit handling per finding kind (fix unreachable assertions; surface
   exact-duplicate rules to the human reviewer via a new "Report conflicts" section rather than
   resolving them itself).
10. ✅ produce reviewable rules — "Output" section (one YAML file per rule, appendix/conflicts note
    as a separate markdown block).

### Phase 6 — Advanced capabilities

Only after the workflow is proven:

* rule mutation testing
* policy conflict detection
* documentation-to-rule impact analysis / stale-rule detection — **DONE**, ahead of the stated order
  (Phase 4/MCP isn't started yet). `metadata.source.file`/`fingerprint` + `rules validate` drift
  warnings — see "Rules become stale" above and
  `docs/IMPLEMENTATION_STATUS.md` ("Post-v1 addition: `metadata.source.file`/`fingerprint` + `rules
  validate` drift warnings").
* rule confidence scoring
* policy coverage reporting

---

# 28. Success Criteria

The system should be considered successful if an AI agent can take:

```text
Organisation engineering documentation
```

and autonomously produce:

```text
Candidate CodeGuard rules
+
Tests
+
Source provenance
```

where:

```text
100% of generated rules
        ↓
CodeGuard validation
        ↓
CodeGuard tests
```

and the resulting output is suitable for human review.

The key success metric is **not the number of rules generated**.

It is:

> **How much organisational engineering policy can be converted into reliable, deterministic, tested executable policy?**

---

# 29. Final Architecture Principle

The long-term CodeGuard architecture should preserve three distinct layers:

```text
┌─────────────────────────────────────────┐
│             HUMAN / ORGANISATION        │
│                                         │
│ Defines engineering policy              │
└────────────────────┬────────────────────┘
                     │
                     │ natural language
                     ↓
┌─────────────────────────────────────────┐
│                AI / AGENT               │
│                                         │
│ Understands documentation                │
│ Extracts requirements                    │
│ Authors candidate rules                  │
│ Generates tests                          │
│ Reasons about ambiguity                  │
└────────────────────┬────────────────────┘
                     │
                     │ executable policy
                     ↓
┌─────────────────────────────────────────┐
│                CODEGUARD                 │
│                                         │
│ Validates rules                         │
│ Tests rules                             │
│ Executes rules                          │
│ Analyses rules                          │
│ Produces deterministic findings         │
│ Enforces approved policy                │
└─────────────────────────────────────────┘
```

This separation should be considered a core architectural principle of CodeGuard.

**CodeGuard should not become an AI rule generator.**

It should become the **deterministic policy runtime that AI agents can author against**.

That gives CodeGuard a stable core while allowing the intelligence around it to evolve rapidly.
