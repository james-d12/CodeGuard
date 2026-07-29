# RuleEngine

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

## CLI usage

The CLI (`rules-engine`) is run via `dotnet run --project src/RulesEngine.Cli --`.

| Command | Description |
|---|---|
| `validate` | Validate a repository against configured rules |
| `list-rules` | List rules discovered from the configured rule directories |
| `explain-rule <ruleId>` | Print full metadata and source YAML for a single rule |
| `list-standards` | List standards referenced by the configured rules |
| `setup` | Configure the rules source (a directory or git repo) used across all repos |

Common options shared by `validate`/`list-rules`/`explain-rule`/`list-standards`: `--path` (repo
root, default cwd), `--config` (explicit `.rulesengine/config.yml`), `--rules-source` (ad-hoc
rules directory or git URL, bypassing config entirely — see below), `--branch` (git branch for
`--rules-source`). `validate` additionally supports `--format console|json|sarif`, `--output
<file>`, `--rule <id>` (repeatable, restricts evaluation), `--solution <file>` (repeatable,
restricts analysis to specific `.sln` files), `--severity-threshold`, `--fail-on`.
`list-rules`/`list-standards` support `--format table|json`; `list-rules` also supports `--tag`,
`--standard`, and `--enabled-only`. `setup` supports `--source`, `--branch`, and `--type
directory|git` (see below).

Examples:

```bash
dotnet run --project src/RulesEngine.Cli -- list-rules
dotnet run --project src/RulesEngine.Cli -- explain-rule DDD-ENTITY-001
dotnet run --project src/RulesEngine.Cli -- validate --format json --output report.json
```

### Configuring where rules come from

By default, `validate`/`list-rules`/etc. look for rules via `.rulesengine/config.yml` in the
target repo. Two ways to point them somewhere else:

**One-time setup**, so every command works out of the box against any repo without per-repo
configuration — stored outside any repo, in the OS user/app-data directory:

```bash
# interactive - prompts for a directory path or git URL (and a branch, for git)
dotnet run --project src/RulesEngine.Cli -- setup

# non-interactive, a local directory of rules
dotnet run --project src/RulesEngine.Cli -- setup --source /home/jamie/rules-checkout

# non-interactive, a git repo - clones on first run, fetches/fast-forwards on later runs
dotnet run --project src/RulesEngine.Cli -- setup --source https://github.com/org/rules-repo.git --branch main
```

Re-running `setup` is the only thing that ever syncs a git rule source — `validate` and friends
never fetch on their own, so results stay reproducible between runs. Re-run `setup` whenever you
want the latest rules.

**Ad-hoc, one-off override** — skip `setup` entirely and point a single command straight at a
rules directory or git URL, without persisting anything:

```bash
dotnet run --project src/RulesEngine.Cli -- validate --path . --rules-source ../local-rules-checkout
dotnet run --project src/RulesEngine.Cli -- validate --path . --rules-source https://github.com/org/rules-repo.git --branch main
```

Resolution precedence (highest first): `--rules-source` > `--config` > the target repo's
`.rulesengine/config.yml` > a prior `setup` run's global settings > the built-in default. See
`docs/done/SETUP_COMMAND_PLAN.md` for the full design.

> **Known limitation:** running `validate` against this repo's own solution
> (`RuleEngine.sln`) currently crashes during self-analysis (a Buildalyzer build-output
> collision) — this doesn't affect validating other repos. See `CLAUDE.md` and
> `docs/IMPLEMENTATION_STATUS.md` for details.

## Configuration

`.rulesengine/config.yml` tells RuleEngine where to discover rules, skills, agents, source, and
tests in a given repository; missing paths are skipped silently. Rule files live under `rules/`
and are validated against `rules/schema/rule.schema.json`.

## Project layout

- `RulesEngine.Analysis` — pure model + analysis provider abstraction (no dependencies)
- `RulesEngine.RuleModel` — selector/assertion/condition interfaces
- `RulesEngine.Evaluation` / `RulesEngine.Core` — concrete selectors/assertions, and the rule evaluator
- `RulesEngine.Analyzers.Roslyn` / `.MSBuild` / `.Repository` — data providers (Roslyn, MSBuild via Buildalyzer, filesystem)
- `RulesEngine.Reporting` — Console/Json/Sarif violation reporters
- `RulesEngine.Configuration` — YAML rule parsing
- `RulesEngine.Cli` — the `rules-engine` command-line tool, depends on everything above

## Further reading

- `docs/PRIMITIVES.md` — original design rationale
- `docs/done/SETUP_COMMAND_PLAN.md` — design of the `setup` command and rule-source resolution
- `CLAUDE.md` — contributor/agent guidance, architecture detail, and known gotchas
