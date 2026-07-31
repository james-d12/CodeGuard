# CodeGuard

A deterministic analysis/validation engine for enforcing an organisation's engineering standards
(DDD, architecture layering, C# conventions, etc.) against .NET repositories. Rules are authored
as declarative YAML (target selector + assertions), not C# code, so new standards can be added
without touching the engine. It's designed to sit alongside AI coding agents as a
machine-checkable guardrail: agents load applicable rules before generating code, then this
engine validates the result and reports structured violations for the agent to remediate.

## Requirements

- .NET SDK `10.0.100` or later (pinned in `global.json`, `rollForward: latestFeature`)

## Build & test

```bash
dotnet build
dotnet test
```

## Installation

The CLI is published to nuget.org as a [.NET tool](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools):

```bash
dotnet tool install -g CodeGuard
codeguard --help
```

This installs the `codeguard` command globally. See [CLI usage](#cli-usage) below (all
examples work the same whether invoked as `codeguard <command>` after a tool install, or as
`dotnet run --project src/CodeGuard.Cli -- <command>` from a checkout of this repo).

## CLI usage

If you've installed the tool (see [Installation](#installation)), run commands directly as
`codeguard <command>`. If you're working in a checkout of this repo, run them via
`dotnet run --project src/CodeGuard.Cli -- <command>` instead.

| Command | Description |
|---|---|
| `validate` | Validate a repository against configured rules |
| `list-rules` | List rules discovered from the configured rule directories |
| `explain-rule <ruleId>` | Print full metadata and source YAML for a single rule |
| `list-standards` | List standards referenced by the configured rules |
| `setup` | Configure the rules source (a directory or git repo) used across all repos |

Common options shared by `validate`/`list-rules`/`explain-rule`/`list-standards`: `--path` (repo
root, default cwd), `--config` (explicit `.codeguard/config.yml`), `--rules-source` (ad-hoc
rules directory or git URL, bypassing config entirely — see below), `--branch` (git branch for
`--rules-source`). `validate` additionally supports `--format console|json|sarif`, `--output
<file>`, `--rule <id>` (repeatable, restricts evaluation), `--solution <file>` (repeatable,
restricts analysis to specific `.sln` files), `--severity-threshold`, `--fail-on`.
`list-rules`/`list-standards` support `--format table|json`; `list-rules` also supports `--tag`,
`--standard`, and `--enabled-only`. `setup` supports `--source`, `--branch`, and `--type
directory|git` (see below).

Examples (installed tool):

```bash
codeguard list-rules
codeguard explain-rule DDD-ENTITY-001
codeguard validate --format json --output report.json
```

Examples (from a checkout of this repo):

```bash
dotnet run --project src/CodeGuard.Cli -- list-rules
dotnet run --project src/CodeGuard.Cli -- explain-rule DDD-ENTITY-001
dotnet run --project src/CodeGuard.Cli -- validate --format json --output report.json
```

### Configuring where rules come from

By default, `validate`/`list-rules`/etc. look for rules via `.codeguard/config.yml` in the
target repo. Two ways to point them somewhere else:

**One-time setup**, so every command works out of the box against any repo without per-repo
configuration — stored outside any repo, in the OS user/app-data directory:

```bash
# interactive - prompts for a directory path or git URL (and a branch, for git)
dotnet run --project src/CodeGuard.Cli -- setup

# non-interactive, a local directory of rules
dotnet run --project src/CodeGuard.Cli -- setup --source /home/jamie/rules-checkout

# non-interactive, a git repo - clones on first run, fetches/fast-forwards on later runs
dotnet run --project src/CodeGuard.Cli -- setup --source https://github.com/org/rules-repo.git --branch main
```

Re-running `setup` is the only thing that ever syncs a git rule source — `validate` and friends
never fetch on their own, so results stay reproducible between runs. Re-run `setup` whenever you
want the latest rules.

**Ad-hoc, one-off override** — skip `setup` entirely and point a single command straight at a
rules directory or git URL, without persisting anything:

```bash
dotnet run --project src/CodeGuard.Cli -- validate --path . --rules-source ../local-rules-checkout
dotnet run --project src/CodeGuard.Cli -- validate --path . --rules-source https://github.com/org/rules-repo.git --branch main
```

Resolution precedence (highest first): `--rules-source` > `--config` > the target repo's
`.codeguard/config.yml` > a prior `setup` run's global settings > the built-in default. See
`docs/done/SETUP_COMMAND_PLAN.md` for the full design.

> **Known limitation:** running `validate` against this repo's own solution
> (`CodeGuard.sln`) currently crashes during self-analysis (a Buildalyzer build-output
> collision) — this doesn't affect validating other repos. See `CLAUDE.md` and
> `docs/IMPLEMENTATION_STATUS.md` for details.

## Configuration

`.codeguard/config.yml` tells CodeGuard where to discover rules, skills, agents, source, and
tests in a given repository; missing paths are skipped silently. Rule files live under `rules/`
and are validated against `rules/schema/rule.schema.json`.

## Project layout

- `CodeGuard.Analysis` — pure model + analysis provider abstraction (no dependencies)
- `CodeGuard.RuleModel` — selector/assertion/condition interfaces
- `CodeGuard.Evaluation` / `CodeGuard.Core` — concrete selectors/assertions, and the rule evaluator
- `CodeGuard.Analyzers.Roslyn` / `.MSBuild` / `.Repository` — data providers (Roslyn, MSBuild via Buildalyzer, filesystem)
- `CodeGuard.Reporting` — Console/Json/Sarif violation reporters
- `CodeGuard.Configuration` — YAML rule parsing
- `CodeGuard.Cli` — the `codeguard` command-line tool, depends on everything above

## Further reading

- `docs/PRIMITIVES.md` — original design rationale
- `docs/done/SETUP_COMMAND_PLAN.md` — design of the `setup` command and rule-source resolution
- `CLAUDE.md` — contributor/agent guidance, architecture detail, and known gotchas
