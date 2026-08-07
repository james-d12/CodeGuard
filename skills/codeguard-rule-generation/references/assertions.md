# Assertions

Each entry in a rule's `assertions` array is a single-key map: the key is one of the following
thirty-four kinds, the value is that kind's params object. There is no free-text/narrative
assertion form (e.g. `{"check": "..."}`) — every assertion must be one of these exact kinds. This
list is kept in sync with `rule-schema.json` in this same folder.

| kind                          | params                                          |
|-------------------------------|---------------------------------------------------|
| `must_inherit_from`           | `type` (glob)                                      |
| `must_not_inherit_from`       | `type` (glob)                                      |
| `must_implement`              | `interface` (glob)                                 |
| `must_not_implement`          | `interface` (glob)                                 |
| `must_have_method`            | `name`                                             |
| `must_not_have_method`        | `name`                                             |
| `must_have_property`          | `name`                                             |
| `must_not_have_property`      | `name`                                             |
| `must_have_constructor`       | `accessibility` (array, e.g. `[public]`)           |
| `must_have_parameter_count`   | `min` (optional int), `max` (optional int)         |
| `must_have_modifier`          | `modifier` (see modifier table below)              |
| `must_not_have_modifier`      | `modifier` (see modifier table below)              |
| `must_have_attribute`         | `type`, `argument` (optional)                      |
| `must_not_have_attribute`     | `type`, `argument` (optional)                      |
| `must_match_name`             | `regex`                                            |
| `must_match_filename`         | *(none — compares the type name to its file name)* |
| `must_match_argument`         | `index` (int), `pattern` (regex)                   |
| `must_be_in_namespace`        | `pattern` (glob)                                   |
| `must_be_in_project`          | `pattern` (glob)                                   |
| `must_reference_package`      | `id` (glob)                                        |
| `must_not_reference_package`  | `id` (glob)                                        |
| `must_reference_project`      | `name` (glob)                                      |
| `must_not_reference_project`  | `name` (glob)                                      |
| `must_not_depend_on`          | `type` (glob)                                      |
| `must_have_msbuild_property`  | `name`, `value` (optional)                         |
| `must_have_file`              | `path`                                             |
| `must_not_have_file`          | `path`                                             |
| `must_have_directory`         | `path`                                             |
| `must_match_content`          | `pattern` (regex, matched against file content)    |
| `must_not_match_content`      | `pattern` (regex, matched against file content)    |
| `must_have_json_field`        | `path`, `equals` (optional)                        |
| `must_not_have_json_field`    | `path`, `equals` (optional)                        |
| `must_exist`                  | `selector` — a full nested `target`-style selector (any kind from `selectors.md`) |
| `must_not_exist`              | `selector` — a full nested `target`-style selector (any kind from `selectors.md`) |

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
