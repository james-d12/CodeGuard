# Target selectors

A rule's `target` selects the set of code elements its `assertions` run against. `target.kind`
must be one of the following fourteen values — do not invent any other kind, an unregistered
`kind` fails to parse. This list, and every param below, is kept in sync with
`rule-schema.json` in this same folder.

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

## Enum values

`call_site.site_kind` is one of `invocation` | `object_creation` | `member_access`.

`accessibility` (on `method`/`property`, and on the `must_have_constructor` assertion) is one of
`public` | `private` | `protected` | `internal` | `protected_internal` | `private_protected`.

## Glob patterns

Pattern matching (namespaces, base types, project names, etc.) uses `*` as a wildcard only — no
`?`, `**`, character classes, or regex syntax. Roslyn renders a closed generic base type as
`Entity<int>`, not the open `Entity<TId>` placeholder used when authoring a rule, so target the
closed-generic shape with a wildcard: `Entity<*>`, not `Entity<TId>`.
