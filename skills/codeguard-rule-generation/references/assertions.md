# Assertions

Each entry in a rule's `assertions` array is a single-key map: the key is one of the kinds in the
table below, the value is that kind's params object. There is no free-text/narrative assertion form
(e.g. `{"check": "..."}`) — every assertion must be one of these exact kinds. The schema only
requires each entry to be a single-key object, so this table is the authoritative list. It is
**generated** from the engine's parser registry by `scripts/sync-skill-references.sh`; run
`codeguard rules discover` to print the same thing yourself.

The `applies to` column is the candidate kind the assertion accepts, and must match what the rule's
`target` produces (see `selectors.md`) — pairing `must_inherit_from` with a `project` target, say, is
a rule that can only ever fail. "any candidate" means the assertion ignores the outer candidate and
works entirely through its own nested selector.

<!-- BEGIN GENERATED assertions - do not edit by hand; run scripts/sync-skill-references.sh -->
| `kind` | params | applies to |
|---|---|---|
| `must_all_match` | `selector` (nested selector, required), `assertions` (nested assertions, required) | any candidate |
| `must_any_match` | `selector` (nested selector, required), `assertions` (nested assertions, required) | any candidate |
| `must_be_in_namespace` | `pattern` (glob, required) | Type |
| `must_be_in_project` | `pattern` (glob, required) | Type |
| `must_depend_on` | `type` (glob, required) | Project |
| `must_exist` | `selector` (nested selector, required) | any candidate |
| `must_have_attribute` | `type` (glob, required), `argument` (string, optional) | Type, Method, Property, Constructor, Field |
| `must_have_constructor` | `accessibility` (string[], required) | Type |
| `must_have_count` | `selector` (nested selector, required), `min` (int, optional), `max` (int, optional), `exactly` (int, optional) | any candidate |
| `must_have_directory` | `path` (glob, required) | Repository |
| `must_have_field` | `name` (glob, required) | Type |
| `must_have_file` | `path` (glob, required) | Repository |
| `must_have_json_field` | `path` (string, required), `equals` (string, optional) | File |
| `must_have_method` | `name` (glob, required) | Type |
| `must_have_modifier` | `modifier` (record \| sealed \| abstract \| static \| partial \| virtual \| override \| async \| const \| readonly \| required \| init, required) | Type, Method, Field, Property |
| `must_have_msbuild_property` | `name` (string, required), `value` (string, optional) | Project |
| `must_have_parameter_count` | `min` (int, optional), `max` (int, optional) | Method, Constructor |
| `must_have_property` | `name` (glob, required) | Type |
| `must_implement` | `interface` (glob, required) | Type |
| `must_inherit_from` | `type` (glob, required) | Type |
| `must_match_argument` | `index` (int, required), `pattern` (regex, required) | CallSite |
| `must_match_content` | `pattern` (regex, required) | File |
| `must_match_filename` | *(none)* | Type |
| `must_match_name` | `regex` (regex, required) | Type, Project, Method, Property, Field, File |
| `must_match_namespace_pattern` | `regex` (regex, required) | Type |
| `must_none_match` | `selector` (nested selector, required), `assertions` (nested assertions, required) | any candidate |
| `must_not_be_in_namespace` | `pattern` (glob, required) | Type |
| `must_not_depend_on` | `type` (glob, required) | Project |
| `must_not_exist` | `selector` (nested selector, required) | any candidate |
| `must_not_have_attribute` | `type` (glob, required), `argument` (string, optional) | Type, Method, Property, Constructor, Field |
| `must_not_have_field` | `name` (glob, required) | Type |
| `must_not_have_file` | `path` (glob, required) | Repository |
| `must_not_have_json_field` | `path` (string, required), `equals` (string, optional) | File |
| `must_not_have_method` | `name` (glob, required) | Type |
| `must_not_have_modifier` | `modifier` (record \| sealed \| abstract \| static \| partial \| virtual \| override \| async \| const \| readonly \| required \| init, required) | Type, Method, Field, Property |
| `must_not_have_property` | `name` (glob, required) | Type |
| `must_not_implement` | `interface` (glob, required) | Type |
| `must_not_inherit_from` | `type` (glob, required) | Type |
| `must_not_match_content` | `pattern` (regex, required) | File |
| `must_not_reference_package` | `id` (glob, required) | Project |
| `must_not_reference_project` | `name` (glob, required) | Project |
| `must_only_depend_on` | `types` (string[], required) | Project |
| `must_reference_package` | `id` (glob, required) | Project |
| `must_reference_project` | `name` (glob, required) | Project |
| `must_use_package_version` | `package` (glob, required), `constraint` (string, required) | Project |
<!-- END GENERATED -->

## Modifier values

`must_have_modifier`/`must_not_have_modifier`'s `modifier` value, and `must_have_constructor`'s
`accessibility` array, are keyed by what the target actually is:

| target kind | valid `modifier` values                                  |
|-------------|-------------------------------------------------------------|
| type        | `record`, `sealed`, `abstract`, `static`, `partial`         |
| method      | `static`, `abstract`, `virtual`, `override`, `async`        |
| field       | `static`, `const`, `readonly`                                |
| property    | `static`, `required`, `init`                                 |

## `must_exist` / `must_not_exist`

These two are the odd ones out: instead of checking a property of the matched target directly,
their `selector` param is a *nested*, independent target selector (any kind from
`selectors.md`, including another `call_site`, `file`, etc.). They ask "does at least one thing
matching this nested selector exist (or not) anywhere the outer target scopes to?" — this is what
lets a `repository`-targeted rule make an assertion about the codebase as a whole. See "Global
rule pattern" in `SKILL.md` for when to reach for this.

## Quantifiers — `must_all_match` / `must_any_match` / `must_none_match` / `must_have_count`

`and`/`or`/`not` (in `when`) combine conditions about *one* candidate. These four instead quantify
over a *set*: they take a nested `selector` and run a nested `assertions:` list against every match
of it.

```yaml
assertions:
  - must_all_match:
      selector:
        kind: property
        declaring_type: "Contoso.Domain.Order"
      assertions:
        - must_have_modifier:
            modifier: init
```

`must_have_count` is the cardinality form — it asserts how *many* things the nested selector matches,
and needs at least one of `min`, `max`, `exactly`:

```yaml
assertions:
  - must_have_count:
      selector:
        kind: class
        namespace: "Contoso.Domain.Aggregates"
      exactly: 1
```

## Dependency assertions

`must_depend_on`, `must_not_depend_on` and `must_only_depend_on` evaluate against **projects** only
(use `target: { kind: project }`), and walk every type-reference site: base types, interfaces,
attributes, and member return/parameter/property/field types.

`must_only_depend_on` is an allow-list and has **no implicit framework exemption**. Roslyn renders
primitives with their C# keyword alias (`string`, `int`, `void`, …), so an allow-list must name the
primitive and framework types the project legitimately uses alongside its own namespaces — otherwise
every project fails it.

`must_use_package_version`'s `constraint` is a comparator (`>=`, `<=`, `>`, `<`, `==`, `!=`; bare
version means `==`) followed by a dotted version, e.g. `">=8.0.0"`. Comparison is numeric-segment
only — any `-prerelease` suffix is stripped — so it is not full SemVer precedence.

## `when` — conditional assertions

`when` is an optional sibling of `target`/`assertions` on a rule (not valid alongside `analyzer`)
that gates whether the rule's assertions apply to a given matched target. It's a single-key node:
`and: [<when-node>, ...]`, `or: [<when-node>, ...]`, `not: <when-node>`, or any one of the
assertion kinds above used as a predicate — e.g. only assert on records whose name matches a
pattern:

```yaml
when:
  must_match_name:
    regex: ".*EntityData$"
```

Nest `and`/`or`/`not` freely; the leaves are always assertion kinds.
