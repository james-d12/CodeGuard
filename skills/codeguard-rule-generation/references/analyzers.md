# Custom analyzers

A rule may specify `analyzer: { kind: ..., ...params }` **instead of** `target`+`assertions`, for
checks no selector/assertion combination expresses (custom Roslyn walks). Only the following
eleven `analyzer.kind` values are registered — do not invent any other kind, an unregistered
`kind` fails to parse. This list is kept in sync with `rule-schema.json` in this same folder.

| `kind`                            | params                                                                                          | checks                                                                 |
|------------------------------------|----------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------|
| `catch-clause-count`               | `namespace` (glob, optional, default `*`), `min_catches`/`max_catches` (optional int, default `1`) | number of `catch` clauses per try block in a namespace                    |
| `companion-type-cardinality`       | `marker_interface` (glob, required), `companion_suffix` (required)                                | every type implementing a marker interface has exactly one `{Type}{Suffix}` companion |
| `const-yaml-value-consistency`     | `const_type` (glob, required), `const_name` (required), `yaml_file_pattern` (required), `yaml_field_path` (required) | a C# `const`'s value matches a field in a YAML file (dotted field path)   |
| `duplicate-attribute-argument`     | `attribute_name` (glob, required), `argument_index` (optional int, default `0`)                    | no two applications of an attribute share the same value at an argument index |
| `exhaustive-switch`                | `namespace` (glob, optional, default `*`)                                                          | `switch` statements/expressions over an enum cover every enum member       |
| `immutable-mutation`               | `namespace` (glob, optional, default `*`)                                                          | no direct field/property assignment after construction on record types (must use `with`) |
| `member-ordering`                  | `order` (optional array of member-kind names)                                                       | type members appear in the declared order                                  |
| `no-exceptions`                    | `namespace` (glob, optional, default `*`), `allow_guard_clause` (optional bool, default `false`)     | no `throw` statements (optionally excluding argument-validation guard clauses) |
| `no-pure-delegation-override`      | `base_type_pattern` (glob, optional, default `*`)                                                   | no override whose body is only `base.Method(...)` with nothing added        |
| `project-convention`               | `project_pattern` (glob, required), `required_call_pattern` (optional, default `*DeployChanges*`), `required_content_folder` (optional, default `Scripts`) | a project follows a required call + content-folder convention              |
| `roslyn-diagnostic-passthrough`    | `diagnostic_ids` (array of strings, required)                                                       | none of the given Roslyn diagnostic IDs are reported anywhere               |

Reach for an analyzer only after confirming (via `selectors.md`/`assertions.md`) that no
`target`+`assertions` combination expresses the check — these cover checks that need custom
Roslyn logic (counting, cross-referencing, exhaustiveness, ordering) rather than a single
structural fact about one matched element.
