# Rule Engine Supporting Tooling Design

## Purpose

The rule engine provides deterministic validation of software repositories against organisational engineering standards.

To make the rule engine scalable and maintainable, it should be supported by a set of developer-facing tools that help create, review, test, understand, and consume rules.

The supporting tooling should make rules easy to evolve while preventing drift, invalid definitions, and unexpected behaviour.

---

# Tooling Overview

The rule engine ecosystem consists of the following commands:

```text
codeguard
    |
    ├── validate
    ├── explain
    ├── test
    ├── lint
    ├── generate
    └── inspect
```

---

# 1. Validate

## Purpose

Execute rules against a target repository and produce validation results.

## Usage

```bash
codeguard validate
```

or:

```bash
codeguard validate --path ./src
```

## Responsibilities

* Load rule definitions
* Discover analysis providers
* Execute deterministic validators
* Evaluate rule composition
* Produce structured results

## Example Output

```
Validation Summary

Rules evaluated: 120
Passed: 115
Failed: 5

Failures:

DDD-001
Domain entities must be located in Domain/Entities

File:
src/Application/Payment.cs

Expected:
src/Domain/Entities/Payment.cs

Recommendation:
Move the entity into the Domain project.
```

## Output Formats

Support:

* Console
* JSON
* SARIF

JSON output enables integration with:

* AI agents
* CI/CD pipelines
* Developer tooling

---

# 2. Explain

## Purpose

Provide human-readable explanations of rules.

Developers should understand:

* What the rule checks
* Why it exists
* How it is evaluated
* How to fix violations

## Usage

```bash
codeguard explain DDD-001
```

## Example

```
Rule:
DDD-001

Name:
Domain entities must be located in Domain/Entities

Purpose:
Ensures domain entities remain within the domain layer.

Validation:
- Finds classes implementing IEntity
- Determines owning project
- Checks directory location

Failure Example:
Payment.cs located in Application project.

Remediation:
Move Payment.cs into Domain/Entities.
```

## Benefits

* Improves developer adoption
* Provides AI agents with remediation context
* Generates documentation automatically

---

# 3. Test

## Purpose

Allow rules to have automated tests.

Rules should be treated as production code and require confidence when changed.

## Usage

```bash
codeguard test
```

## Test Structure

Example:

```
rules/
    ddd-001.yaml

tests/
    ddd-001/
        valid/
        invalid/
```

Example:

```
tests/
 └── ddd-001/
      ├── valid/
      │    └── PaymentEntity.cs
      │
      └── invalid/
           └── PaymentEntity.cs
```

Expected:

```
PASS

DDD-001

Valid example:
  Passed

Invalid example:
  Correctly detected violation
```

---

# 4. Lint

## Purpose

Validate the rules themselves.

Rules are critical business assets and should have quality controls.

## Usage

```bash
codeguard lint
```

## Checks

### Schema Validation

Ensure rules conform to the rule schema.

Example:

```
✓ Required fields exist
✓ Primitive types are valid
✓ Assertion structure is valid
```

---

### Reference Validation

Ensure dependencies exist.

Example:

```
✓ Primitive exists:
  must_inherit_from

✓ Referenced rule exists:
  DDD-001
```

---

### Dependency Checks

Detect:

* Circular rule references
* Duplicate rule IDs
* Missing metadata
* Invalid severity values

---

# 5. Generate

## Purpose

Generate supporting artefacts from rule definitions.

The rule definition should become the single source of truth.

## Possible Outputs

## Documentation

Generate:

```
Engineering Standards Catalogue
```

from:

```yaml
rules/
```

---

## AI Context

Generate:

```
skills/generated/
agents/generated/
```

containing:

* Applicable rules
* Rule descriptions
* Remediation guidance

Example:

```
Agent:
domain-developer

Applicable Standards:

DDD-001
Entities must be located in Domain/Entities.

DDD-002
Domain must not reference Infrastructure.
```

This prevents duplicated standards existing in agents and skills.

---

# 6. Inspect

## Purpose

Understand what the analysis engine has discovered.

Useful for developing new rules.

## Usage

```bash
codeguard inspect
```

## Example

```
Repository Facts

Projects:
- Payments.Api
- Payments.Domain
- Payments.Infrastructure

Classes:
- Payment
  Implements:
    IEntity<Guid>

Dependencies:
Payments.Domain
    References:
        Company.DomainKernel
```

This allows rule authors to understand available facts before creating rules.

---

# 7. Rule Development Workflow

Recommended workflow:

```
1. Identify requirement

        ↓

2. Create rule definition

        ↓

3. Validate rule schema

        ↓

4. Create test cases

        ↓

5. Run rule tests

        ↓

6. Run against repositories

        ↓

7. Publish rule
```

---

# 8. Integration With AI Agents

The rule engine should expose machine-readable results.

Example:

```json
{
  "rule": "DDD-001",
  "status": "failed",
  "severity": "error",
  "target": "Payment.cs",
  "message": "Entity is located outside Domain/Entities",
  "remediation": "Move entity into Domain project"
}
```

Agents can then:

1. Execute code changes
2. Run validation
3. Consume failures
4. Apply fixes
5. Re-run validation

The AI does not decide whether the implementation complies; the rule engine provides deterministic evidence.

---

# Design Principles

## Rules Are First-Class Assets

Rules should have:

* Versioning
* Tests
* Documentation
* Ownership
* Review process

---

## Single Source of Truth

Requirements should exist once.

Rules define:

* What must be true

Skills define:

* How to achieve it

Agents define:

* Who performs the work

---

## Deterministic First

Prefer deterministic validation wherever possible.

Examples:

```
File exists
Class inherits from type
Package referenced
Method called
Namespace matches
Project dependency exists
```

AI validation should only be used where interpretation is required.

---

## Developer Experience Matters

The tooling should make rules:

* Easy to discover
* Easy to understand
* Easy to modify
* Safe to change

The goal is to make organisational standards executable.
