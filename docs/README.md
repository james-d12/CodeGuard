# CodeGuard documentation index

Every document under `docs/` falls into one of three groups. Outstanding work from these docs is
tracked as GitHub issues; the larger initiatives are tracked as epics, listed below. The
classification and issues come from a full docs-vs-code audit on 2026-10-04 (against `main` @ `2905358`).

## Architecture and reference

Evergreen scope documents and living reference docs, updated in place and never archived.

| Doc | Purpose |
|---|---|
| [`PRIMITIVES.md`](PRIMITIVES.md) | Original design and requirements doc. **Frozen**: do not edit or move. Unbuilt primitive families are tracked in [#85](https://github.com/james-d12/CodeGuard/issues/85). |
| [`REFACTORING.md`](REFACTORING.md) | A separate, larger architectural-evolution proposal. Not started as an initiative; do not edit or move. Phases are tracked in [#84](https://github.com/james-d12/CodeGuard/issues/84). |
| [`architecture/CORE_RULES.md`](architecture/CORE_RULES.md) | Product scope: the six policy pillars, non-goals, enforcement levels and waivers. |
| [`architecture/IMPLEMENTATION_STATUS.md`](architecture/IMPLEMENTATION_STATUS.md) | Build history, decisions and gotchas. Read before making non-trivial changes. Some stale statements are tracked in [#80](https://github.com/james-d12/CodeGuard/issues/80). |

## Roadmap (outstanding)

Documents describing work that is at least partly not yet built.

| Doc | What's left |
|---|---|
| [`roadmap/HIGH_LEVEL_ROADMAP.md`](roadmap/HIGH_LEVEL_ROADMAP.md) | Phase 2 (binaries, JSON/SARIF, exit codes) is mostly done. Still open: rule distribution ([#82](https://github.com/james-d12/CodeGuard/issues/82)), the result contract and CI governance (#66–#78), and server/portal/registry ([#83](https://github.com/james-d12/CodeGuard/issues/83)). |
| [`roadmap/HIGH_LEVEL_AI_ASSISTING.md`](roadmap/HIGH_LEVEL_AI_ASSISTING.md) | Phases 0–3 and 5 are done. Still open: the MCP server ([#62](https://github.com/james-d12/CodeGuard/issues/62)) and Phase 6 ([#86](https://github.com/james-d12/CodeGuard/issues/86)). |
| [`roadmap/SUPPORTING_TOOLS.md`](roadmap/SUPPORTING_TOOLS.md) | validate/explain/test/lint shipped as `codeguard rules …`. Still open: `inspect` (#59) and `generate` (#60, #61). |
| [`roadmap/REPORTING_API_PROMPT.md`](roadmap/REPORTING_API_PROMPT.md) | A prompt for the Reporting API MVP design. Nothing is built yet ([#83](https://github.com/james-d12/CodeGuard/issues/83)). |

## Done (archived design records)

Single-initiative plans that have fully shipped. They are kept because code comments still reference them.

| Doc | Shipped in |
|---|---|
| [`done/RULE_VALIDATION_PLAN.md`](done/RULE_VALIDATION_PLAN.md) | `rules validate` |
| [`done/SETUP_COMMAND_PLAN.md`](done/SETUP_COMMAND_PLAN.md) | `codeguard setup` |
| [`done/RULES_TEST_DESIGN.md`](done/RULES_TEST_DESIGN.md) | `rules test` with embedded `tests:` |
| [`done/RULE_COVERAGE_PLAN.md`](done/RULE_COVERAGE_PLAN.md), [`done/RULE_COVERAGE_STAGE_A_RULES.md`](done/RULE_COVERAGE_STAGE_A_RULES.md), [`done/STAGE_B_PROGRESS.md`](done/STAGE_B_PROGRESS.md) | Rule coverage stages A and B |
| [`done/RULE_SOURCE_AND_LINKED_DOCUMENTATION.md`](done/RULE_SOURCE_AND_LINKED_DOCUMENTATION.md) | `metadata.source.file` and source-drift detection |
| [`done/FILE_FOLDER_RULES_PLAN.md`](done/FILE_FOLDER_RULES_PLAN.md) | PR #36: path-aware globs and directory rules |
| [`done/RULE_VERSIONING_PLAN.md`](done/RULE_VERSIONING_PLAN.md) | PR #38: mandatory rule versioning. The body describes the superseded opt-in design; see its status banner. |
| [`done/INSTALL_SCRIPTS_PLAN.md`](done/INSTALL_SCRIPTS_PLAN.md) | PR #39: one-liner installers |
| [`done/RULE_FUZZING_PLAN.md`](done/RULE_FUZZING_PLAN.md) | PR #40: rule-engine fuzz testing |

## Epics

| Epic | Scope |
|---|---|
| [#82](https://github.com/james-d12/CodeGuard/issues/82) | Rule distribution: packages, git sources, lock file, governance |
| [#83](https://github.com/james-d12/CodeGuard/issues/83) | Reporting server, portal and registry |
| [#84](https://github.com/james-d12/CodeGuard/issues/84) | REFACTORING.md phases |
| [#85](https://github.com/james-d12/CodeGuard/issues/85) | Remaining PRIMITIVES.md primitive families |
| [#86](https://github.com/james-d12/CodeGuard/issues/86) | Advanced AI-assisted authoring (Phase 6) |
| [#87](https://github.com/james-d12/CodeGuard/issues/87) | Cross-system governance providers |

Concrete, individually scoped issues are #41–#81. Filter them by the `area:*` and `size:*` labels.

## Deliberately not tracked

These items were already decided against, or are out of scope until someone has a concrete need.
Re-raise them only with a consumer in mind.

- **Rule lifecycle `status`** (experimental/active/deprecated): evaluated and rejected. See `architecture/IMPLEMENTATION_STATUS.md`.
- **`rules create`**: removed.
- **Glob character classes `[abc]`, brace expansion `{a,b}`, and a case-insensitivity toggle**: out of scope in `done/FILE_FOLDER_RULES_PLAN.md`.
- **The "Future extensions" in `done/RULES_TEST_DESIGN.md`** (expected counts/locations, test tags, `--test` filter, coverage): waiting on demand.
- **IDE0005, IDE0011 and IDE0161 passthrough** (`done/STAGE_B_PROGRESS.md`): only CS1591 passthrough works today.
- **The non-.NET and execution-dependent rules** excluded in `done/RULE_COVERAGE_PLAN.md`: these exclusions are permanent.
