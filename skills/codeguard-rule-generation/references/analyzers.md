# Custom analyzers

A rule may specify `analyzer: { kind: ..., ...params }` **instead of** `target`+`assertions`, for
checks no selector/assertion combination expresses (custom Roslyn walks). Only the `analyzer.kind`
values in the table below are registered — do not invent any other kind, an unregistered `kind`
fails to parse. This table is **generated** from the engine's parser registry by
`scripts/sync-skill-references.sh`; run `codeguard rules discover` to print the same thing yourself.

<!-- BEGIN GENERATED analyzers - do not edit by hand; run scripts/sync-skill-references.sh -->
| `kind` | params | checks |
|---|---|---|
| `catch-clause-count` | `namespace` (glob, optional, default `*`), `min_catches` (int, optional, default `1`), `max_catches` (int, optional, default `1`) | Flags try blocks whose catch-clause count falls outside the allowed range. |
| `companion-type-cardinality` | `marker_interface` (glob, required), `companion_suffix` (string, required) | Flags types implementing a marker interface that lack exactly one matching companion type. |
| `const-yaml-value-consistency` | `const_type` (glob, required), `const_name` (string, required), `yaml_file_pattern` (glob, required), `yaml_field_path` (string, required) | Cross-checks a C# const against a single field in a YAML file. The only YAML-aware check; there is no generic YAML field assertion. |
| `duplicate-attribute-argument` | `attribute_name` (glob, required), `argument_index` (int, optional, default `0`) | Flags a repeated argument value across all uses of an attribute. |
| `exhaustive-switch` | `namespace` (glob, optional, default `*`) | Flags switches over an enum that do not cover every member. |
| `immutable-mutation` | `namespace` (glob, optional, default `*`) | Flags mutations of types intended to be immutable. |
| `member-ordering` | `order` (string[], optional) | Flags types whose members are not declared in the configured order. |
| `no-exceptions` | `namespace` (glob, optional, default `*`), `allow_guard_clause` (bool, optional, default `false`) | Flags throw sites in a namespace. |
| `no-pure-delegation-override` | `base_type_pattern` (glob, optional, default `*`) | Flags overrides whose body only delegates to the base implementation. |
| `project-convention` | `project_pattern` (glob, required), `required_call_pattern` (glob, optional, default `*DeployChanges*`), `required_content_folder` (glob, optional, default `Scripts`) | Flags projects matching a pattern that lack a required call site or content folder. |
| `roslyn-diagnostic-passthrough` | `diagnostic_ids` (string[], required) | Surfaces raw Roslyn compiler diagnostics as rule violations. Requires a real compilation. |
<!-- END GENERATED -->

Reach for an analyzer only after confirming (via `selectors.md`/`assertions.md`) that no
`target`+`assertions` combination expresses the check — these cover checks that need custom
Roslyn logic (counting, cross-referencing, exhaustiveness, ordering) rather than a single
structural fact about one matched element.
