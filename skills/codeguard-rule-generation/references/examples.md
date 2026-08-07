# Worked examples

Four examples below, each demonstrating a different rule shape. All four use the fictional
`Contoso.*` namespace and are marked `illustrative: true` — that's this skill's own worked-example
convention (and matches how CodeGuard's own repository tags its starter rule set), **not** the
default for real generated rules. When generating rules from a real organisation's real standards
documentation, set `illustrative: false` (see `SKILL.md`) and swap `Contoso.*` for that
organisation's actual namespaces.

Don't copy the concrete values below (`Guid`, `EntityData`, `HttpClient`, ...) into unrelated
rules — they're illustrating a *shape*, not a library of reusable values.

## `tests` (optional)

A rule may include a `tests` array to self-document expected pass/fail behaviour:

```yaml
tests:
  - name: <description>
    setup:
      <freeform fixture data — shape depends on what the rule's target/analyzer needs>
    expect: pass   # or: fail
```

Include this when you can express a clear minimal pass case and fail case; omit it otherwise —
most existing example rules in this repository don't have it, and it isn't required by the schema.
Example 1 below shows it in use.

## 1. `target` + `assertions` + `tests`

```yaml
id: DDD-ENTITY-001
name: Domain entities must inherit from Entity
description: >
  All domain entities must inherit from the approved Entity<TId> base class.
severity: error
enforcement:
  classification: deterministic
tags:
  - ddd
  - domain
  - entity
remediation: >
  Inherit from Contoso.Domain.Entity<TId>.
illustrative: true

target:
  kind: class
  namespace: "Contoso.Domain.Entities"

assertions:
  - must_inherit_from:
      type: "Contoso.Domain.Entity<*>"

tests:
  - name: Entity inheriting from Entity<TId>
    setup:
      types:
        - name: Order
          namespace: Contoso.Domain.Entities
          baseType: "Contoso.Domain.Entity<Guid>"
    expect: pass

  - name: Entity not inheriting from Entity<TId>
    setup:
      types:
        - name: LegacyThing
          namespace: Contoso.Domain.Entities
    expect: fail
```

## 2. `analyzer`-only

```yaml
id: SKILL-DOMAIN-IMMUTABLE-MUTATION-001
name: Entity state mutated only via immutable with-updates
description: >
  Entity state must be mutated only via immutable `with` updates on
  EntityData, never a direct field/property assignment after construction -
  record types are meant to be immutable value objects.
severity: error
enforcement:
  classification: deterministic
tags:
  - domain
remediation: >
  Replace the direct assignment with a `with` expression that produces a new
  EntityData instance.
illustrative: true

analyzer:
  kind: immutable-mutation
  namespace: "Contoso.Domain.*"
```

## 3. `target` + `when` + `assertions`

```yaml
id: SKILL-DOMAIN-ENTITY-DATA-SHAPE-001
name: Domain entity data shape
description: >
  *EntityData types must be record with required+init properties, including
  CreatedDateTime and UpdatedDateTime.
severity: error
enforcement:
  classification: deterministic
tags:
  - domain
remediation: >
  Declare the type as a record with CreatedDateTime and UpdatedDateTime
  properties (required init).
illustrative: true

target:
  kind: record
  namespace: "Contoso.Domain.Entities"

when:
  must_match_name:
    regex: ".*EntityData$"

assertions:
  - must_have_property:
      name: "CreatedDateTime"
  - must_have_property:
      name: "UpdatedDateTime"
```

## 4. `repository` target + nested `must_not_exist` selector (the "global rule pattern")

```yaml
id: SKILL-APPLICATION-NO-RAW-HTTPCLIENT-001
name: No raw HttpClient in application HTTP calls
description: >
  Application-layer HTTP calls must use IHttpRequestSender/HttpRequestBuilder,
  never raw HttpClient.
severity: error
enforcement:
  classification: deterministic
tags:
  - http
remediation: >
  Replace the raw HttpClient with an IHttpRequestSender/HttpRequestBuilder
  typed client.
illustrative: true

target:
  kind: repository

assertions:
  - must_not_exist:
      selector:
        kind: call_site
        site_kind: object_creation
        invoked_member: "HttpClient"
        project: "*.Application*"
```

## Field order

Match this field order when emitting a rule: `id`, `name`, `description`, `severity`,
`enforcement`, `tags`, `remediation`, `illustrative`, blank line, `target` (or `analyzer`), blank
line, `when` if present, blank line, `assertions` if present, blank line, `tests` if present.
