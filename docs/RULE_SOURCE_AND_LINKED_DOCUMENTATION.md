# CodeGuard Rule Sources

## Purpose

Allow CodeGuard rules to reference the original organisational policy or documentation they implement, while providing deterministic tooling to detect when that source has changed or become unavailable.

The goal is **traceability and maintenance**, not automatic synchronisation or policy interpretation.

## Rule Source

Rules should optionally contain a `source` section identifying the policy/documentation from which the rule originated.

Example:

```yaml
source:
  file: docs/architecture.md
  heading: Domain Layer
  text: "Domain projects must not reference Infrastructure."
  fingerprint: sha256:...
```

The source should provide:

* A human-readable reference to the original policy.
* Enough information to locate the relevant section/content.
* A fingerprint or equivalent mechanism to detect changes.

The exact schema and fingerprint implementation should be determined based on the existing CodeGuard architecture.

## Source Validation

Add tooling to validate rule sources independently from normal rule validation.

For example:

```bash
codeguard rules check-sources
```

Validation should detect conditions such as:

* Source file/document no longer exists.
* Referenced section cannot be found.
* Source content has changed.
* Source reference is ambiguous.
* Source remains valid but has moved.

Where possible, CodeGuard should report the current location when content has moved.

Example:

```text
✓ domain-no-infrastructure
  Source unchanged

⚠ events-must-be-past-tense
  Source content changed

✗ service-ownership
  Source document no longer exists
```

## No Automatic Decisions

CodeGuard must **not** determine whether a source change means the rule itself is now incorrect.

It should only report that the relationship between the rule and its source requires review.

For example:

```text
Source changed since rule was created.

Previously:
  Domain projects must not reference Infrastructure.

Currently:
  Domain projects should not directly depend on Infrastructure.

Action: Review rule.
```

The user remains responsible for deciding whether to update the rule, update the documentation, or retire the rule.

## Normal Validation vs Source Validation

Keep the concerns separate:

```text
codeguard validate
    → Does the repository comply with the rules?

codeguard rules check-sources
    → Are the rules' documented sources still valid?
```

Source checking should not unexpectedly change the result of normal rule validation.

## Maintenance Workflow

The intended workflow is:

```text
Organisation policy/documentation
          ↓
       CodeGuard rule
          ↓
    source reference
          ↓
     source changes
          ↓
 CodeGuard detects drift
          ↓
      human review
```

This reduces the need for users to manually discover stale rules while avoiding automatic interpretation of organisational policy.

## Scope

This design should initially focus on repository-local documentation such as Markdown files.

External systems such as Confluence, SharePoint, or other knowledge bases should not be required for the initial implementation. The source model should remain extensible enough to support them later.

## Design Principle

**CodeGuard should provide evidence that a rule's source has changed, not make a judgement about what that change means.**
