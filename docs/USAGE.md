# CodeGuard usage

Detailed reference for installing, running and configuring CodeGuard. For a short overview, see the
[README](../README.md).

## Contents

- [Requirements](#requirements)
- [Installation](#installation)
- [CLI reference](#cli-reference)
- [Configuring rule sources](#configuring-rule-sources)
- [Writing rules](#writing-rules)
- [Repository configuration](#repository-configuration)
- [Building and testing](#building-and-testing)
- [Project layout](#project-layout)
- [Further reading](#further-reading)

## Requirements

- .NET SDK `10.0.100` or later to build this repository. The version is pinned in `global.json`
  with `rollForward: latestFeature`.
- `validate` needs a .NET SDK installed at runtime. It locates MSBuild through `MSBuildLocator`
  to analyse `.sln` and `.csproj` files, even when using a standalone binary.

## Installation

### .NET tool

The CLI is published to nuget.org as a [.NET tool](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools):

```bash
dotnet tool install -g CodeGuard
codeguard --help
```

This installs the `codeguard` command globally. Every example in [CLI reference](#cli-reference)
works the same with `codeguard <command>` after installing, or with
`dotnet run --project src/CodeGuard.Cli -- <command>` from a checkout of this repository.

### Standalone binaries

Each [GitHub Release](https://github.com/james-d12/CodeGuard/releases) publishes self-contained
builds for Linux, macOS and Windows. These do not require a .NET runtime to launch `codeguard`.
Each carries the same version as that release's NuGet package.

Each archive extracts to a folder, not a single file. The folder contains `codeguard` (or
`codeguard.exe`) and its runtime and dependencies. `validate` uses Roslyn's `MSBuildWorkspace`,
which launches a second "BuildHost" process from a DLL that must sit next to the executable on
disk, so the binary cannot be merged into a single file.

| Platform | Archive |
|---|---|
| Linux x64 | `codeguard-<version>-linux-x64.tar.gz` |
| Linux arm64 | `codeguard-<version>-linux-arm64.tar.gz` |
| macOS x64 (Intel) | `codeguard-<version>-osx-x64.tar.gz` |
| macOS arm64 (Apple Silicon) | `codeguard-<version>-osx-arm64.tar.gz` |
| Windows x64 | `codeguard-<version>-win-x64.zip` |

Each release also publishes `checksums.txt` (SHA256) for the archives.

Two points apply to the standalone binaries:

- **macOS Gatekeeper:** the binary is not code-signed or notarised, so macOS refuses to run it on
  first launch. Clear the quarantine attribute once on the extracted folder:
  `xattr -dr com.apple.quarantine ./codeguard`.
- **SDK still required for `validate`:** the standalone binary removes the need for
  `dotnet tool install`, but `validate` still needs the .NET SDK (see [Requirements](#requirements)).

### Install scripts

The install scripts resolve the latest release, verify its checksum, extract it and add it to
`PATH`.

Linux and macOS:

```bash
curl --proto '=https' -fsSL https://raw.githubusercontent.com/james-d12/CodeGuard/main/scripts/install.sh | bash
```

Windows (PowerShell):

```powershell
irm https://raw.githubusercontent.com/james-d12/CodeGuard/main/scripts/install.ps1 | iex
```

Both scripts accept a pinned version and a custom install directory. Pass `--version` and
`--install-dir` to the bash script, or `-Version` and `-InstallDir` to the PowerShell script. When
piping, you can instead set the `CODEGUARD_VERSION` and `CODEGUARD_INSTALL_DIR` environment
variables, because piped input cannot receive arguments.

Review [`scripts/install.sh`](../scripts/install.sh) or [`scripts/install.ps1`](../scripts/install.ps1)
before running them if you prefer not to pipe a script into a shell.

For running these in a CI pipeline, see [CI integration](CI.md).

<details>
<summary>Manual install</summary>

Linux and macOS:

```bash
mkdir codeguard && curl -L https://github.com/james-d12/CodeGuard/releases/download/v<version>/codeguard-<version>-<rid>.tar.gz | tar xz -C codeguard
cd codeguard
./codeguard --help
```

Windows (PowerShell):

```powershell
Invoke-WebRequest -Uri https://github.com/james-d12/CodeGuard/releases/download/v<version>/codeguard-<version>-win-x64.zip -OutFile codeguard.zip
Expand-Archive codeguard.zip -DestinationPath codeguard
cd codeguard
.\codeguard.exe --help
```

</details>

## CLI reference

### Commands

| Command | Description |
|---|---|
| `validate` | Validate a repository against configured rules |
| `rules list` | List rules discovered from the configured rule directories |
| `rules explain <ruleId>` | Print full metadata and source YAML for a single rule |
| `rules validate` | Validate rule YAML files for structural correctness. Does not run against a repository |
| `rules test` | Run each rule's embedded `tests:` cases against a virtual analysis model (no repository, no disk I/O) |
| `rules analyze` | Check a rule set for problems beyond structural validity: missing or one-sided tests, unreachable assertions, exact-duplicate rules |
| `rules discover` | List every selector, assertion and analyzer kind the engine supports, with parameters. Reads no rule files |
| `setup` | Configure the rules source (a directory or git repository) used across all repositories |
| `info` | Show where rules are configured from and how many were discovered |

### Shared options

| Option | Meaning |
|---|---|
| `--path` | Repository root (default: current directory) |
| `--config` | An explicit `.codeguard/config.yml` |
| `--rules-source` | Point straight at a rules directory or git URL, bypassing config (see [Configuring rule sources](#configuring-rule-sources)) |
| `--branch` | Git branch to use with `--rules-source` |
| `--verbosity` | Minimum log level written to stderr: `debug`, `information`, `warning`, `error` or `critical` (case-insensitive). Default: `information` |

### Command-specific options

| Command | Options |
|---|---|
| `validate` | `--format console\|json\|sarif\|html`, `--output <file>`, `--rule <id>` (repeatable), `--solution <file>` (repeatable), `--severity-threshold`, `--fail-on`, `--color` / `--no-color`, `--max-parallelism` |
| `rules list` | `--format table\|json`, `--tag`, `--enabled-only` |
| `rules explain` | `--format console\|json` |
| `rules validate` | `--format console\|json`, `--update-fingerprints` |
| `rules test` | `--format console\|json`, `--rule <id>` (repeatable) |
| `rules analyze` | `--format console\|json` |
| `rules discover` | `--format console\|json\|markdown`, `--section selectors\|assertions\|analyzers` (with `markdown`) |
| `setup` | `--source`, `--branch`, `--type directory\|git` |

`--update-fingerprints` on `rules validate` is the only mode of that command that writes to rule
files. It recomputes `metadata.source.fingerprint` and `versionFingerprint` values in place, and
leaves other content untouched. Rules with a broken source link are not fixed automatically.

`--rule` on `validate` and `rules test` takes an ID. Repeat the flag to select several rules, for
example `--rule A --rule B`. Space-separated values are not supported.

### Examples

Installed tool:

```bash
codeguard rules list
codeguard rules explain DDD-ENTITY-001
codeguard validate --format json --output report.json
```

From a checkout of this repository:

```bash
dotnet run --project src/CodeGuard.Cli -- rules list
dotnet run --project src/CodeGuard.Cli -- rules explain DDD-ENTITY-001
dotnet run --project src/CodeGuard.Cli -- validate --format json --output report.json
```

## Configuring rule sources

By default, commands look for rules through `.codeguard/config.yml` in the target repository. You
can also point them elsewhere in two ways.

### One-time setup

`setup` makes every command work against any repository without per-repository configuration. The
setting is stored outside any repository, in the OS user or application-data directory.

```bash
# Interactive: prompts for a directory path or git URL, and a branch for git
dotnet run --project src/CodeGuard.Cli -- setup

# Non-interactive: a local directory of rules
dotnet run --project src/CodeGuard.Cli -- setup --source /home/jamie/rules-checkout

# Non-interactive: a git repository. Clones on first run, fetches and fast-forwards on later runs
dotnet run --project src/CodeGuard.Cli -- setup --source https://github.com/org/rules-repo.git --branch main
```

`setup` is the only command that syncs a git rule source. `validate` and the other commands never
fetch on their own, so results stay reproducible between runs. Re-run `setup` to pick up the latest
rules. Run `codeguard info` to check which source is active and how many rules it resolved.

### Ad-hoc override

To use a rules directory or git URL for a single command without persisting anything, pass
`--rules-source`. This skips `setup`.

```bash
dotnet run --project src/CodeGuard.Cli -- validate --path . --rules-source ../local-rules-checkout
dotnet run --project src/CodeGuard.Cli -- validate --path . --rules-source https://github.com/org/rules-repo.git --branch main
```

### Resolution precedence

Highest first:

1. `--rules-source`
2. `--config`
3. The target repository's `.codeguard/config.yml`
4. Global settings from a prior `setup` run
5. The built-in default

The full design is in [`docs/done/SETUP_COMMAND_PLAN.md`](done/SETUP_COMMAND_PLAN.md).

## Writing rules

### Anatomy of a rule

A rule is plain YAML. A `target` selector picks the code elements to check, and one or more
`assertions` must hold for every match. The sample in the [README](../README.md#example-rule) is
`examples/rules/ddd/ddd-entity-001.yml`. It carries `illustrative: true`, which only marks it as a
sample rule. That key is not part of the schema for your own rules.

`validate` loads every rule from the configured rules directory. It runs each rule's `target`
selector against the repository's analysis model (types, projects, namespaces and so on) to find
matching code. It then checks each assertion against every match. A failing assertion becomes a
violation at the rule's `severity`, with its `remediation` text attached, in the format selected by
`--format`.

Useful commands while authoring:

- `codeguard rules explain <ruleId>` prints a rule's full metadata and source YAML.
- `codeguard rules discover` lists every selector, assertion and analyzer kind available. Use it
  before hand-writing a rule.

### Schema

Every rule is validated against the JSON Schema at
`src/CodeGuard.Configuration/Validation/Schemas/rule.schema.json`. The schema is embedded as a
resource in the published tool.

### Sample rules

`examples/rules/` contains this repository's own rule set, organised by area (`ddd/`,
`architecture/`, `csharp/`, `persistence/`, `reporting/` and so on). Browse it to see what a
realistic rule set looks like before writing your own.

## Repository configuration

`.codeguard/config.yml` tells CodeGuard where to find rules, skills, agents, source and tests in a
repository. Paths that do not exist are skipped silently. Rule files are plain YAML validated
against the JSON Schema described above.

## Building and testing

```bash
dotnet build
dotnet test
```

Architecture notes, gotchas and the CI pipeline are in [`CLAUDE.md`](../CLAUDE.md).

## Project layout

- `CodeGuard.Analysis`: pure model and analysis provider abstraction (no dependencies)
- `CodeGuard.RuleModel`: selector, assertion and condition interfaces
- `CodeGuard.Evaluation` and `CodeGuard.Core`: concrete selectors and assertions, and the rule evaluator
- `CodeGuard.Analyzers.Roslyn`, `.MSBuild` and `.Repository`: data providers (Roslyn, MSBuild via `MSBuildWorkspace`, and the filesystem)
- `CodeGuard.Reporting`: Console, JSON, SARIF and HTML violation reporters
- `CodeGuard.Configuration`: YAML rule parsing
- `CodeGuard.Cli`: the `codeguard` command-line tool, which depends on all of the above

## Further reading

- [`docs/CI.md`](CI.md): CI integration recipes, exit codes and `--fail-on`
- [`docs/PRIMITIVES.md`](PRIMITIVES.md): original design rationale
- [`docs/roadmap/HIGH_LEVEL_AI_ASSISTING.md`](roadmap/HIGH_LEVEL_AI_ASSISTING.md): AI-assisted rule
  authoring, including `rules discover`, `rules analyze`, rule provenance (`metadata.source`) and
  the `codeguard-rule-generation` skill under `skills/`
- [`docs/done/SETUP_COMMAND_PLAN.md`](done/SETUP_COMMAND_PLAN.md): design of `setup` and rule-source resolution
- [`docs/README.md`](README.md): index of all documentation
- [`CLAUDE.md`](../CLAUDE.md): contributor and agent guidance, architecture detail and known gotchas
