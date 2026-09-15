# Target selectors

A rule's `target` selects the set of code elements its `assertions` run against. `target.kind`
must be one of the kinds in the table below — do not invent any other kind, an unregistered `kind`
fails to parse. `rule-schema.json` only requires `kind` to be a non-empty string, so this table is
the authoritative list. It is **generated** from the engine's parser registry by
`scripts/sync-skill-references.sh`; run `codeguard rules discover` to print the same thing yourself.

The `selects` column names the candidate kind a selector yields, which is what an assertion's
"applies to" column has to match. `switch`, `throw_site`, `mutation_site`, `try_block`,
`method_body_shape`, `diagnostic` and `directory` select syntax facts and filesystem entries, and are
almost always used as the **nested** selector inside `must_exist` / `must_not_exist` /
`must_have_count` rather than as a top-level `target` — see "Global rule pattern" in `SKILL.md`.

<!-- BEGIN GENERATED selectors - do not edit by hand; run scripts/sync-skill-references.sh -->
| `kind` | params | selects |
|---|---|---|
| `call_site` | `site_kind` (invocation \| object_creation \| member_access, optional), `invoked_member` (glob, optional, default `*`), `target_type` (glob, optional, default `*`), `project` (glob, optional, default `*`), `containing_method` (glob, optional, default `*`), `containing_type` (glob, optional, default `*`), `argument_index` (int, optional), `argument_is_literal` (bool, optional), `enclosing_comparison` (string, optional) | Invocations, object creations and member accesses matching the given filters. |
| `class` | `namespace` (glob, required) | Classes in a matching namespace. |
| `constructor` | `declaring_type` (glob, optional, default `*`), `parameter_types` (string[], optional) | Constructors matching the given filters. |
| `diagnostic` | `id` (glob, optional, default `*`), `project` (glob, optional, default `*`) | Raw Roslyn compiler diagnostics by ID. Requires a real compilation, so unavailable in rule tests unless supplied directly. |
| `directory` | `path` (glob, optional, default `*`) | Repository directories by path. |
| `enum` | `namespace` (glob, optional, default `*`) | Enum types in a matching namespace. |
| `field` | `declaring_type` (glob, optional, default `*`), `is_readonly` (bool, optional), `is_static` (bool, optional) | Fields matching the given filters. |
| `file` | `path` (glob, optional, default `*`), `extension` (string, optional) | Repository files by path and/or extension. |
| `implements` | `interface` (glob, required) | Types implementing a matching interface. |
| `inherits_from` | `type` (glob, required) | Types deriving from a matching base type. |
| `method` | `namespace` (glob, optional, default `*`), `project` (glob, optional, default `*`), `declaring_type` (glob, optional, default `*`), `name` (glob, optional, default `*`), `accessibility` (public \| private \| protected \| internal \| protected_internal \| private_protected, optional), `is_async` (bool, optional), `is_static` (bool, optional) | Methods matching the given filters. |
| `method_body_shape` | `min_statement_count` (int, optional), `max_statement_count` (int, optional), `is_single_base_call_delegation` (bool, optional), `containing_type` (glob, optional, default `*`), `containing_method` (glob, optional, default `*`), `project` (glob, optional, default `*`) | Method bodies by statement count and shape. |
| `mutation_site` | `target_member` (glob, optional, default `*`), `containing_type` (glob, optional, default `*`), `containing_method` (glob, optional, default `*`), `project` (glob, optional, default `*`) | Assignments to, or mutations of, a matching member. |
| `project` | `name` (glob, required) | Projects with a matching name. |
| `property` | `namespace` (glob, optional, default `*`), `project` (glob, optional, default `*`), `declaring_type` (glob, optional, default `*`), `accessibility` (public \| private \| protected \| internal \| protected_internal \| private_protected, optional), `is_static` (bool, optional) | Properties matching the given filters. |
| `record` | `namespace` (glob, optional, default `*`) | Record types in a matching namespace. |
| `repository` | *(none)* | The repository as a whole - a single candidate. Pair with must_exist/must_not_exist/must_have_count and a nested selector to express a repo-wide rule. |
| `switch` | `containing_type` (glob, optional, default `*`), `containing_method` (glob, optional, default `*`), `project` (glob, optional, default `*`), `has_default_or_discard_arm` (bool, optional) | Switch statements and expressions matching the given filters. |
| `throw_site` | `exception_type` (glob, optional, default `*`), `is_first_statement_in_method` (bool, optional), `containing_type` (glob, optional, default `*`), `containing_method` (glob, optional, default `*`), `project` (glob, optional, default `*`) | Throw sites by thrown exception type. |
| `try_block` | `min_catch_clause_count` (int, optional), `max_catch_clause_count` (int, optional), `containing_type` (glob, optional, default `*`), `containing_method` (glob, optional, default `*`), `project` (glob, optional, default `*`) | Try blocks by catch-clause count. |
| `type` | `namespace` (glob, optional, default `*`), `name` (glob, optional, default `*`) | All types (class, record, struct, interface, enum) in a matching namespace. |
<!-- END GENERATED -->

## Enum values

`call_site.site_kind` is one of `invocation` | `object_creation` | `member_access`.

`accessibility` (on `method`/`property`, and on the `must_have_constructor` assertion) is one of
`public` | `private` | `protected` | `internal` | `protected_internal` | `private_protected`.

## Glob patterns

Pattern matching (namespaces, base types, project names, etc.) uses `*` as a wildcard only — no
`?`, `**`, character classes, or regex syntax. Roslyn renders a closed generic base type as
`Entity<int>`, not the open `Entity<TId>` placeholder used when authoring a rule, so target the
closed-generic shape with a wildcard: `Entity<*>`, not `Entity<TId>`.
