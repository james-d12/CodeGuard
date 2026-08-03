# CodeGuard Rule Testing

## Purpose

Add `codeguard rules test` to allow rule authors to test the behaviour of rules without creating physical test repositories or writing C# test code.

Tests are defined **inside the rule YAML file** alongside the rule definition. Each test describes the virtual analysis state required by the rule and the expected result.

The feature is intended to make rules:

* Self-contained
* Easy to author and maintain
* Deterministically testable
* Safe to refactor
* Portable between rule repositories
* Suitable for CI and AI-agent workflows

## Commands

Run all embedded rule tests:

```bash
codeguard rules test
```

Optionally run tests for a specific rule:

```bash
codeguard rules test --rule DDD-ENTITY-001
```

The command exits with a non-zero status when one or more tests fail.

`rules test` is distinct from the other CodeGuard operations:

| Command                    | Purpose                                                 |
| -------------------------- | ------------------------------------------------------- |
| `codeguard rules validate` | Validate the structure and validity of rule definitions |
| `codeguard rules test`     | Verify that rules behave as expected                    |
| `codeguard validate`       | Evaluate an actual repository against its rules         |

## Rule structure

Tests live directly within the rule YAML file.

```yaml
id: ARCH-DOMAIN-001
name: Domain must not depend on Infrastructure

target:
  kind: project

assertions:
  - must_not_reference_project:
      name: Infrastructure

tests:
  - name: Domain without Infrastructure dependency
    setup:
      projects:
        - name: Domain
    expect: pass

  - name: Domain depending on Infrastructure
    setup:
      projects:
        - name: Domain
          projectReferences: [Infrastructure]
    expect: fail
```

(This example uses `must_not_reference_project`, one of the ~35 assertion kinds the engine actually
implements today — see `DefaultParsers.cs`. Earlier drafts of this document used an illustrative
`dependency`/`not` shorthand that doesn't correspond to any real selector or assertion kind; treat
any YAML in this document as the real, current syntax unless stated otherwise.)

A rule and its tests therefore form a single, self-contained specification.

No separate test file is required.

## Test schema

The rule schema gains an optional `tests` property:

```json
"tests": {
  "type": "array",
  "items": {
    "$ref": "#/$defs/testCase"
  }
}
```

Each test case has the following structure:

```json
"testCase": {
  "type": "object",
  "additionalProperties": false,
  "required": ["name", "setup", "expect"],
  "properties": {
    "name": {
      "type": "string",
      "minLength": 1
    },
    "setup": {
      "type": "object",
      "minProperties": 1,
      "additionalProperties": true
    },
    "expect": {
      "type": "string",
      "enum": ["pass", "fail"]
    }
  }
}
```

### `name`

A human-readable description of the behaviour being tested.

```yaml
name: Domain depending on Infrastructure
```

Test names should describe behaviour rather than implementation details.

### `setup`

Describes the virtual analysis state required by the test.

The setup is **not created on disk**.

The schema intentionally permits arbitrary properties:

```json
"setup": {
  "type": "object",
  "minProperties": 1,
  "additionalProperties": true
}
```

This is deliberate. CodeGuard's rule model is extensible and primitive-driven. The central rule schema should not need to know every possible analysis input.

The exact setup primitives are defined by the analysis abstractions used by CodeGuard —
concretely, `CodeGuard.Analysis`'s `RepositoryModel` (files, directories, solutions → projects →
types → members, and call sites).

### v1 setup scope

The initial implementation covers **files, directories, projects/types (with nested methods,
properties, constructors, fields, and attributes), and call sites**. Together these back the large
majority of the existing rule set's selector/assertion kinds (`class`, `type`, `record`, `enum`,
`method`, `property`, `constructor`, `field`, `inherits_from`, `implements`, `project`, `file`,
`repository`, `call_site`, and the ~35 `must_*` assertion kinds built on them).

Explicitly out of scope for v1 (produces a clear error if a rule needing them is given a test):
switch statements, throw sites, mutation sites, try blocks, method-body shapes, and raw compiler
diagnostics — the Roslyn syntax-fact records consumed only by a handful of bespoke `analyzer`-kind
rules (e.g. `exhaustive_switch`, `no_exceptions`, `catch_clause_count`). These require full
source-level Roslyn analysis to produce meaningfully, which is a larger, separate effort (see
"Future extensions" — "Rules that require source-level Roslyn setup"). Add support for a given
concept only once a real rule needs a test that exercises it.

### Setup shape

Setup uses a two-tier shape: flat shortcuts for the common case, and a full `projects` form when a
rule genuinely cares about project structure (e.g. project-reference or package-reference rules).

Flat shortcuts — for rules that don't care what project a type or file lives in, everything is
folded into one synthetic project/solution:

```yaml
setup:
  files:
    - path: appsettings.json
  types:
    - name: Order
      namespace: Contoso.Domain
      baseType: "Entity<Guid>"
      interfaces: [IAggregateRoot]
      methods:
        - name: Create
  callSites:
    - invokedMember: Result
      containingType: Contoso.Domain.Order
```

Full form — for rules whose behaviour depends on project identity or references:

```yaml
setup:
  projects:
    - name: Domain
    - name: Infrastructure
```
(as used in the `ARCH-DOMAIN-001` example above, where `must_not_reference_project` needs to see
the actual project graph).

Every field on every setup concept is optional and defaults to a sensible empty value (empty
string/list, public accessibility, line/column `0`, and so on) — a test only needs to specify the
handful of fields its rule actually inspects, per the "lowest useful abstraction" principle below.

A small number of existing assertions (`must_have_directory`, `must_match_content`,
`must_not_match_content`, `must_have_json_field`, `must_not_have_json_field`) originally read the
real filesystem directly (`Directory.Exists`, `File.ReadAllText`) rather than going through the
analysis model at all, which would have made any rule using them untestable without physical files.
`RepositoryModel` gained a `Directories` list and `FileModel` gained an optional `Content` field
specifically so these assertions can be satisfied virtually; real repository validation is
unaffected (`Directories` is still collected from the real filesystem, and `Content` is only read
from disk when a test hasn't supplied it directly).

The test framework should model setup at the **lowest useful abstraction consumed by the rule**, rather than forcing every test to construct a complete fake repository.

### `expect`

Defines the expected outcome of the rule.

```yaml
expect: pass
```

means the rule must produce **zero violations**.

```yaml
expect: fail
```

means the rule must produce **one or more violations**.

The initial implementation does not require assertions about exact messages, locations, or violation counts.

## Virtual analysis state

Tests describe a virtual repository or analysis state.

CodeGuard must not create a physical repository, files, directories, projects, or other resources on disk simply to execute a rule test.

The intended model is:

```text
Test YAML
    ↓
Virtual setup
    ↓
Analysis model
    ↓
Rule evaluator
    ↓
Test result
```

This keeps tests small and avoids maintaining fixture repositories.

A test should only describe the information required by the rule.

For example, a filesystem rule should not require a complete project structure if it only needs to know whether a particular file exists.

## Execution model

Rule tests must use the **same rule evaluator and rule semantics as normal repository validation**.

Normal validation:

```text
Real repository
      ↓
Analysis providers
      ↓
Analysis model
      ↓
Rule evaluator
      ↓
Violations
```

Rule testing:

```text
Test setup
      ↓
Virtual analysis data
      ↓
Same analysis model
      ↓
Same rule evaluator
      ↓
Test result
```

The test framework must not implement a separate version of rule evaluation.

This prevents tests from passing against behaviour that differs from real repository validation.

## Analysis abstraction

The main architectural work for this feature is defining how test setup becomes the same analysis data consumed by the production evaluator.

The test framework should not fake the entire filesystem or invoke a different rule implementation.

Instead, CodeGuard should identify the abstractions consumed by selectors, assertions, and analyzers and provide suitable virtual implementations or test data sources.

For example:

```text
Production:

Filesystem / MSBuild / Roslyn
            ↓
      Analysis abstractions
            ↓
           Rules


Tests:

YAML setup
    ↓
Virtual analysis providers
    ↓
      Analysis abstractions
            ↓
           Rules
```

The exact virtual providers should be determined from the existing CodeGuard analysis model rather than designed independently of it.

This is an important design constraint.

## Test isolation

Each test case is independent.

The virtual setup for one test must not affect another test.

Tests must not depend on:

* Execution order
* Other test cases
* Physical files created by another test
* Mutable global state
* The developer's working directory

Each test should receive a fresh test context.

## Determinism

Rule tests must be deterministic.

Running:

```bash
codeguard rules test
```

against the same rule source should produce the same result.

Tests should not require network access, external repositories, or other mutable external state.

## Test coverage expectations

A rule should normally contain tests covering both sides of its intended behaviour.

For example:

```yaml
tests:
  - name: Valid architecture
    setup:
      # valid virtual state
    expect: pass

  - name: Invalid architecture
    setup:
      # invalid virtual state
    expect: fail
```

The initial implementation should not enforce a minimum number of tests or require every rule to have tests.

A future policy could require tests for particular rule classifications if experience shows this is useful.

## Rule validation

`codeguard rules validate` remains responsible for validating the rule definition itself.

It should validate the structural correctness of embedded tests, including:

* Missing test name
* Missing setup
* Empty setup
* Invalid expected result
* Invalid test structure

The intended CI sequence for a rules repository is:

```bash
codeguard rules validate
codeguard rules test
```

A structurally invalid rule should therefore be rejected before its tests are executed.

## Output

The default output should provide a concise summary:

```text
Rule tests

✓ ARCH-DOMAIN-001
  ✓ Domain without Infrastructure dependency
  ✓ Domain depending on Infrastructure

✓ DDD-ENTITY-001
  ✓ Valid entity
  ✗ Entity without required base type

Tests: 4
Passed: 3
Failed: 1
```

The output should identify:

* Rule ID
* Test name
* Pass/fail status
* Failure reason when a test fails

Existing CodeGuard reporting infrastructure should be reused where appropriate.

Machine-readable output should be supported where practical so that CI systems and AI agents can consume test results.

## CLI options

The initial implementation should support:

```bash
codeguard rules test
codeguard rules test --rule DDD-ENTITY-001
```

Existing common rule-source options should follow the same behaviour as other `rules` commands, including:

* `--path`
* `--config`
* `--rules-source`
* `--branch`

Additional options should only be added when there is a concrete requirement.

## Design constraints

### Tests remain inside rule files

Do not introduce separate test files.

A rule should contain its definition and behavioural specification together.

### No test DSL

The feature should extend the existing YAML rule model rather than introduce a second language for writing tests.

### No mocking of the rule engine

Selectors, assertions, analyzers, and the evaluator should execute normally.

The test infrastructure provides virtual analysis input rather than mocking rule behaviour.

### No physical fixture repositories

Tests should not require repositories, directories, or source files to be created on disk.

### No separate rule execution path

The production evaluator must be reused.

### Keep the schema extensible

The central rule schema should not prematurely encode every possible form of analysis setup.

`setup` should remain extensible while the underlying analysis abstractions determine which setup primitives are supported.

## Future extensions

The initial implementation should leave room for:

* Exact violation assertions
* Expected violation counts
* Expected locations
* Expected severity
* Test tags/categories
* JSON test results
* Test coverage reporting
* `--test <name>` filtering
* Generated documentation from rule examples
* Rules that require source-level Roslyn setup
* Rule-specific setup primitives

These should not be implemented until actual rules demonstrate a need for them.

## Success criteria

The feature is successful when a rule author can:

1. Define a rule in one YAML file.
2. Add representative passing and failing cases to that same file.
3. Describe only the virtual analysis state relevant to the rule.
4. Run `codeguard rules test`.
5. Get deterministic results without creating files or repositories on disk.
6. Execute the rule through the same evaluation path used by `codeguard validate`.
7. Run the tests directly in the rules repository's CI.
8. Understand a failing test without needing to inspect generated fixture files.
