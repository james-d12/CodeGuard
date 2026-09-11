# CodeGuard — AI-Assisted Rule Authoring & MCP

**Status:** Proposed
**Version:** 1.1
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

Example:

Everything below **except the `metadata:` block** is current, valid syntax. `metadata` does not
exist yet: the rule schema is `additionalProperties: false` at the root, so adding it to a rule today
is a hard validation failure. Implementing this section therefore means a schema change first.

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

# PROPOSED — not yet supported by the schema.
metadata:
  source:
    document: architecture-standards.md
    section: Layering Rules
    statement: >
      Domain projects must not depend on Infrastructure projects.
  generation:
    method: ai

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
* Globs support `*` only — no `**`, no path semantics.
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
codeguard rules validate  # structural validation of a rule set (--format console|json)
codeguard rules test      # run rules' embedded tests: cases (--format console|json)
codeguard rules list      # (--format table|json)
codeguard rules explain   # console only today; see §13
codeguard rules create    # interactive rule scaffolder
codeguard setup           # configure the rule source
codeguard info            # show the resolved rule source and counts
```

**Proposed** — these do not exist yet:

```bash
codeguard rules discover  # §12
codeguard rules analyze   # §14
codeguard rules explain --format json   # §13
```

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

`codeguard rules validate --format json` exists today, but every error is a **bare string**:

```json
{
  "filesChecked": 125,
  "filesPassed": 124,
  "isValid": false,
  "issues": [
    {
      "sourceFile": "/abs/path/ddd-042.yml",
      "errors": ["/assertions/0: Unknown assertion kind 'business-logic-quality'."]
    }
  ]
}
```

The proposed change is to make each error structured:

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

This is cheap: `RuleSchemaValidator` already computes the JSON-pointer path
(`detail.InstanceLocation`) and then concatenates it into the message. The work is widening
`RuleFileIssue.Errors` from `IReadOnlyList<string>` to a record, and assigning codes at the throw
sites in the selector/assertion/analyzer registries.

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

**Prerequisite, and the largest single item in this document.** Selectors, assertions and their
parsers are purely behavioural today — `ISelectorParser`/`IAssertionParser`/`IAnalyzerParser` expose
a `Kind` string and a `Parse` method, and each parser reads its params imperatively. There is no
parameter metadata anywhere, so `discover` can list kind *names* today but cannot describe a single
parameter. Delivering §12, §13 and parts of §14 therefore requires first adding a declarative
capability-descriptor layer across all 77 parser classes. That work is not optional and is not small;
§27 must sequence it before those sections.

Once it exists, the same descriptors should **generate** `skills/codeguard-rule-generation/references/*.md`.
Those files are hand-maintained today and have already drifted 18 primitives behind the engine,
which is precisely the hallucination problem this section exists to solve.

Example:

```bash
codeguard rules discover
```

Output should be the engine's **actual** vocabulary — currently 21 target selectors, 45 assertions
and 11 analyzers — with each kind's parameters, not an abstract taxonomy:

```text
Target selectors (21)
  class              namespace (glob)
  project            name (glob)
  throw_site         exception_type (glob, default *), containing_type, containing_method, project
  directory          path (glob, default *)
  ...

Assertions (45)
  must_inherit_from        type (glob)                     [types]
  must_not_reference_project  name (glob)                  [projects]
  must_have_count          selector + one of min|max|exactly
  ...

Analyzers (11)
  exhaustive-switch
  const-yaml-value-consistency
  ...
```

Machine-readable output should also be supported:

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

`codeguard rules explain <id>` exists today but is console-only: it prints a metadata summary
followed by the rule's raw YAML. The proposal is a `--format json` mode.

Assertion **parameter values** cannot be recovered from the in-memory model (`IAssertion` exposes
only `Kind`; constructor arguments vanish into private fields — see §12). Rather than adding
introspection to every assertion, `explain --format json` should emit the rule's metadata plus the
**source document converted YAML→JSON**. The loader already parses YAML into a `JsonNode`, and
`explain` already resolves the source file path, so this is faithful and cheap:

```json
{
  "id": "ARCHITECTURE-DOMAIN-NO-INFRASTRUCTURE-001",
  "name": "Domain projects must not reference Infrastructure",
  "severity": "error",
  "description": "...",
  "enforcement": { "classification": "deterministic" },
  "tags": ["architecture"],
  "enabled": true,
  "illustrative": false,
  "sourceFile": "/abs/path/architecture-domain-no-infrastructure-001.yml",
  "testCount": 2,
  "document": {
    "target": { "kind": "project", "name": "*.Domain" },
    "assertions": [ { "must_not_reference_project": { "name": "*Infrastructure*" } } ]
  }
}
```

`source` (document/section) appears here only once §6/§19's `metadata` block exists.

This is useful both for developers and AI agents.

---

# 14. `codeguard rules analyze`

This capability should analyse a rule collection for mechanically detectable problems.

These checks are **not** of comparable cost, and listing them as one bullet list has led to them
being treated as one piece of work. They split into three tiers:

**Tier 1 — mechanically provable, cheap. Implement first.**

* Invalid rules (reuses the existing rule-set validation)
* Duplicate IDs (already detected during loading)
* Missing tests
* One-sided tests — a rule with a `pass` case but no `fail` case. This is the cheap structural proxy
  for "tests don't exercise the intended assertion", and worth more than it looks: a `fail` case is
  what stops a mis-specified `pass` case from passing vacuously.
* Missing provenance (requires §6/§19's `metadata`)
* Disabled rules; `illustrative: true` rules in a production rule set

**Tier 2 — needs the §12 descriptor layer.**

* Exact-duplicate rules (same target kind + params + assertion set)
* Unreachable rules — an assertion kind that cannot apply to the target's candidate type

**Tier 3 — research, not scheduled work. Do not promise these.**

* Overlapping selectors and conflicting assertions. Proving that two glob-scoped selectors overlap,
  or that two assertions contradict, needs glob subsumption plus an assertion negation algebra.
* "Rules whose tests don't exercise the intended assertion" in its full form is mutation testing
  (§22), not a static check.

Example:

```text
CodeGuard Rule Analysis

Rules: 87

Valid:                         84
Invalid:                        3
Rules without tests:            7
Duplicate selectors:            2
Potential conflicts:            1
Missing provenance:             4
```

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

**Not implemented.** The rule schema is `additionalProperties: false` at the root, so `metadata:` is
currently rejected outright rather than merely unused. The existing `documentation` field is
`array<string>` and is used by none of the 125 example rules — too weak to carry document/section/
statement provenance. Implementing this section means adding `metadata` to the schema, the rule
model, and the parser first.

Proposed shape:

```yaml
metadata:
  source:
    document: architecture-standards.md
    section: "4.2 Layering"
    statement: "Domain projects must not depend on Infrastructure."
```

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

Future capability:

```text
Source documentation changed
        ↓
Potentially affected rules
        ↓
Review required
```

## CodeGuard becomes too complex

Mitigation:

Keep semantic interpretation outside CodeGuard.

---

# 27. Recommended Implementation Order

### Phase 0 — Correct what is already broken

Before adding anything: this document's examples must parse (§6), the skill's reference docs must
match the engine's actual vocabulary, rule tests must not be able to pass vacuously, and CI must
actually run the shipped rule set. None of this depends on new architecture.

### Phase 1 — Capability descriptors, then the authoring primitives

`rules validate`, `rules test`, `rules list` and `rules explain` already exist (§9); only their
gaps need closing. The genuinely new work is the descriptor layer:

1. Add declarative parameter descriptors to the selector/assertion/analyzer parsers (§12). This
   gates `rules discover`, `rules explain --format json`, and §14's Tier 2 — it must come first.
2. `codeguard rules discover` (§12), including a `markdown` format that regenerates the skill's
   reference docs so they can no longer drift.
3. Structured validation errors (§10).
4. `codeguard rules explain --format json` (§13).

Ensure all have excellent JSON output.

### Phase 2 — Rule metadata

Introduce:

* provenance
* lifecycle state
* test metadata
* deterministic capability metadata

### Phase 3 — Rule analysis

Implement:

```bash
codeguard rules analyze
```

with the Tier 1 and Tier 2 checks from §14. Tier 3 is explicitly out of scope.

### Phase 4 — MCP

Seven of the eight tools below (everything except `validate_repository`) need no MSBuild and no
Roslyn — they read rule files and run the evaluator against virtual models. The MCP server should
therefore depend only on the configuration/core/evaluation projects and stay fast to start.
`validate_repository` is the exception: it loads real solutions through MSBuild, must register
`MSBuildLocator` exactly once per process, and takes minutes on a large repository. Give it a
separate host or run it out-of-process rather than making the whole server heavy.

Expose the deterministic capabilities:

```text
validate_rule
test_rule
discover_capabilities
explain_rule
analyze_rules
validate_repository
```

### Phase 5 — Improve the existing AI skill

Update the rule-authoring skill to:

1. discover CodeGuard capabilities
2. analyse documentation
3. generate candidates
4. generate tests
5. validate
6. test
7. iterate
8. report non-deterministic requirements
9. report conflicts
10. produce reviewable rules

### Phase 6 — Advanced capabilities

Only after the workflow is proven:

* rule mutation testing
* policy conflict detection
* documentation-to-rule impact analysis
* stale-rule detection
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
