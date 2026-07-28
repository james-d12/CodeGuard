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

Common options for `validate`: `--path` (repo root, default cwd), `--config` (explicit
`.rulesengine/config.yml`), `--format console|json|sarif`, `--output <file>`, `--rule <id>`
(repeatable, restricts evaluation), `--severity-threshold`, `--fail-on`.
`list-rules`/`list-standards` support `--format table|json`; `list-rules` also supports `--tag`,
`--standard`, and `--enabled-only`.

Examples:

```bash
dotnet run --project src/RulesEngine.Cli -- list-rules
dotnet run --project src/RulesEngine.Cli -- explain-rule DDD-ENTITY-001
dotnet run --project src/RulesEngine.Cli -- validate --format json --output report.json
```

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
- `CLAUDE.md` — contributor/agent guidance, architecture detail, and known gotchas
