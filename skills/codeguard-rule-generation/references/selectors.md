# Target selectors

A rule's `target` selects the set of code elements its `assertions` run against. `target.kind`
must be one of the following twenty-one values — do not invent any other kind, an unregistered
`kind` fails to parse. `rule-schema.json` only requires `kind` to be a non-empty string, so this
table is the authoritative list; run `codeguard rules discover` to confirm it against the engine.

The first fourteen select declaration-level elements and are the usual choice for a rule's top-level
`target`. The last seven (`switch` … `directory`) select syntax facts and filesystem directories, and
are almost always used as the **nested** selector inside `must_exist` / `must_not_exist` /
`must_have_count` rather than as a top-level `target` — see "Global rule pattern" in `SKILL.md`.

| `kind`          | params                                                                                                                                                  | selects                                              |
|-----------------|----------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------|
| `class`         | `namespace` (glob)                                                                                                                                        | classes in a matching namespace                        |
| `type`          | `namespace` (glob, optional, default `*`)                                                                                                                  | all types in a matching namespace                      |
| `project`       | `name` (glob)                                                                                                                                              | projects with a matching name                          |
| `inherits_from` | `type` (glob)                                                                                                                                              | types deriving from a matching base type                |
| `implements`    | `interface` (glob)                                                                                                                                         | types implementing a matching interface                 |
| `record`        | `namespace` (glob, optional, default `*`)                                                                                                                  | record types in a matching namespace                    |
| `enum`          | `namespace` (glob, optional, default `*`)                                                                                                                  | enum types in a matching namespace                      |
| `file`          | `path` (glob, optional, default `*`), `extension` (optional)                                                                                              | repository files by path/extension                     |
| `repository`    | *(none)*                                                                                                                                                    | the repository as a whole — used with `must_exist`/`must_not_exist` (see "Global rule pattern" in `SKILL.md`) |
| `method`        | `namespace`, `project`, `declaring_type` (globs, optional, default `*`), `name` (glob, optional, default `*`), `accessibility` (optional), `is_async`/`is_static` (optional bool) | methods matching the given filters                      |
| `property`      | `namespace`, `project`, `declaring_type` (globs, optional, default `*`), `accessibility` (optional), `is_static` (optional bool)                          | properties matching the given filters                   |
| `constructor`   | `declaring_type` (glob, optional, default `*`), `parameter_types` (optional array of glob)                                                                | constructors matching the given filters                 |
| `field`         | `declaring_type` (glob, optional, default `*`), `is_readonly`/`is_static` (optional bool)                                                                 | fields matching the given filters                       |
| `call_site`     | `site_kind` (optional), `invoked_member`, `target_type`, `project`, `containing_method`, `containing_type` (globs, optional, default `*`), `argument_index` (optional int), `argument_is_literal` (optional bool), `enclosing_comparison` (optional) | call sites (invocations/object creations/member access) matching the given filters |
| `switch`        | `containing_type`, `containing_method`, `project` (globs, optional, default `*`), `has_default_or_discard_arm` (optional bool) | switch statements/expressions matching the filters |
| `throw_site`    | `exception_type` (glob, optional, default `*`), `is_first_statement_in_method` (optional bool), `containing_type`, `containing_method`, `project` (globs, optional, default `*`) | `throw` sites by thrown exception type |
| `mutation_site` | `target_member` (glob, optional, default `*`), `containing_type`, `containing_method`, `project` (globs, optional, default `*`) | assignments/mutations of a matching member |
| `try_block`     | `min_catch_clause_count`, `max_catch_clause_count` (optional int), `containing_type`, `containing_method`, `project` (globs, optional, default `*`) | `try` blocks by catch-clause count |
| `method_body_shape` | `min_statement_count`, `max_statement_count` (optional int), `is_single_base_call_delegation` (optional bool), `containing_type`, `containing_method`, `project` (globs, optional, default `*`) | method bodies by statement count/shape |
| `diagnostic`    | `id` (glob, optional, default `*`), `project` (glob, optional, default `*`) | raw Roslyn compiler diagnostics by ID (e.g. `CS1591`) |
| `directory`     | `path` (glob, optional, default `*`)                                                                                          | repository directories by path |

## Enum values

`call_site.site_kind` is one of `invocation` | `object_creation` | `member_access`.

`accessibility` (on `method`/`property`, and on the `must_have_constructor` assertion) is one of
`public` | `private` | `protected` | `internal` | `protected_internal` | `private_protected`.

## Glob patterns

Pattern matching (namespaces, base types, project names, etc.) uses `*` as a wildcard only — no
`?`, `**`, character classes, or regex syntax. Roslyn renders a closed generic base type as
`Entity<int>`, not the open `Entity<TId>` placeholder used when authoring a rule, so target the
closed-generic shape with a wildcard: `Entity<*>`, not `Entity<TId>`.
