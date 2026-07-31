# Setup Command Plan — configuring where the CLI reads rules from

> Status: **done**. Implemented as designed below: `GlobalSettings`/`GlobalSettingsPaths`/
> `GlobalSettingsStore` under `src/CodeGuard.Configuration/GlobalConfig/` (namespace
> `CodeGuard.Configuration.GlobalConfig` — not `GlobalSettings`, to avoid a type/namespace name
> collision), `GitRuleSourceSync`/`RuleSourceResolver`/`SetupCommand` under `src/CodeGuard.Cli/`,
> the precedence change in `CliRepositoryContext.Resolve`, and `--rules-source`/`--branch` added to
> every command via `CommonOptions`. Covered by `tests/CodeGuard.Configuration.Tests/GlobalConfig/`
> and the new `tests/CodeGuard.Cli.Tests/` project (git-sync tests run against a throwaway local
> `git init` repo). Kept for design rationale.

## Context

Today, `codeguard validate` (and `list-rules`/`explain-rule`/`list-standards`) find rules purely
through `.codeguard/config.yml`'s `repository.rules` paths, resolved relative to the target repo
by `RepositoryDiscovery.ResolveExisting` (`src/CodeGuard.Configuration/Discovery/RepositoryDiscovery.cs`).
If no config file exists, `CodeGuardConfigLoader` falls back to a hardcoded
`Repository.Rules = ["rules"]` default (`src/CodeGuard.Configuration/Discovery/CodeGuardConfigLoader.cs:38`)
that only resolves to anything for CodeGuard's own dogfooding checkout (which happens to have a
`rules/` folder). Point this tool at any *other* repo with no `.codeguard/config.yml`, and
`ResolveExisting` silently filters out the non-existent `rules` path — you get **zero rules
evaluated, with no error**, which is a bad first-run experience.

The existing workaround is visible in this repo's own `.codeguard/config.external.yml`: hand-write
a config with an absolute path to a rules checkout. That works, but it means every repo you want to
validate needs its own hand-authored config file pointing at wherever you happen to keep the rules
repo checked out locally — not the "easy to use CLI" experience wanted here.

This doc proposes two complementary features to fix that:

1. **`codeguard setup`** — an interactive (or scriptable) command that lets you point the CLI at
   a rules source *once* — a local directory, or a git repo it clones and keeps up to date — stored
   outside any project repo, in the OS-appropriate user/app-data location. After running it,
   `validate` works against any target repo without per-repo configuration.
2. **`--rules-source` ad-hoc override** — for when you don't even want to run `setup` first: point
   `validate` (or any command) directly at a rules folder or repo URL for a one-off run, no
   persisted state required.

Both are additive — nothing about the existing `.codeguard/config.yml` schema or behavior for
this repo changes.

## Design decisions

### 1. Determinism over convenience — no silent auto-sync

CLAUDE.md's opening line describes this project as "a deterministic analysis/validation engine."
If `validate` silently re-fetched/pulled the configured rules repo on every run, identical
invocations could produce different violations over time as the upstream rules change underneath
you — worst of all inside CI, where reproducibility matters most.

**Decision**: only `codeguard setup`, run explicitly, checks for and pulls updates. Every other
command reads whatever is currently materialized on disk (in the local directory, or the git cache),
with no network calls and no implicit mutation. Refreshing the rule set is always a deliberate
action (`setup` again), never a side effect of validating.

### 2. Cross-platform app-data location

The global config needs to live outside any git repo, in the OS's conventional per-user config
location. A real gotcha worth calling out explicitly: **`Environment.GetFolderPath(SpecialFolder.ApplicationData)`
does not give macOS's native path.** .NET Core's Unix implementation follows the XDG Base Directory
spec uniformly for both Linux and macOS — it returns `~/.config` (or `$XDG_CONFIG_HOME`) on *both*,
never `~/Library/Application Support`. Relying on the built-in `SpecialFolder` enum would silently
give macOS users a non-native, XDG-flavored path.

**Decision**: resolve explicitly per OS via `RuntimeInformation.IsOSPlatform`, not
`Environment.SpecialFolder`:

| OS | Root directory |
|---|---|
| Windows | `%APPDATA%\CodeGuard` |
| macOS | `~/Library/Application Support/CodeGuard` |
| Linux | `$XDG_CONFIG_HOME/codeguard` (falls back to `~/.config/codeguard`) |

Since CI (`.github/workflows/ci.yml`) only runs on `ubuntu-latest`, the Windows/macOS branches can
never get real coverage by running the actual OS. **The resolver must be written as a pure function**
— taking the platform, an environment-lookup delegate, and the home directory as parameters, rather
than reading `RuntimeInformation.OSDescription`/`Environment.GetEnvironmentVariable` inline — so all
three branches are unit-testable from Linux CI by just passing in a different `OSPlatform` value.
A thin public wrapper supplies the real values at the call site.

### 3. New config layer — parallel to, not merged with, the existing one

A new module under `src/CodeGuard.Configuration/GlobalSettings/`, deliberately separate from
`CodeGuardConfig`/`CodeGuardConfigLoader` (which stay exactly as they are — per-repo, checked
into the target repo, describing *that repo's* rules/skills/agents/source/tests layout):

- **`GlobalSettingsPaths`** — the cross-platform root resolver above, plus a helper that derives the
  git clone cache directory for a given URL: `<root>/rules-cache/<sanitized-name-from-url>`. This is
  *derived at read time from the URL*, not persisted anywhere — nothing about it can go stale, and
  it naturally gives `setup` and the ad-hoc `--rules-source` flag (see §6) a shared cache keyed by
  URL, so cloning the same repo via either path reuses the same local copy.
- **`GlobalSettings`** — the persisted model:
  ```csharp
  public enum RuleSourceKind { Directory, Git }

  public sealed class GlobalSettings
  {
      public required RuleSourceKind Kind { get; init; }
      public required string Location { get; init; }   // absolute directory path, or a git URL
      public string? Branch { get; init; }              // git only; null = repo's default branch
  }
  ```
- **`GlobalSettingsStore`** — `Load`/`Save` against `<root>/settings.yml`, using the same
  `YamlDotNet` `DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance)`
  convention `CodeGuardConfigLoader` already uses, for consistency.

Sketch of `settings.yml`:

```yaml
kind: git
location: https://github.com/org/our-engineering-rules.git
branch: main          # omit to track the repo's default branch
```

or, for a local directory source:

```yaml
kind: directory
location: /home/james/rules-checkouts/our-engineering-rules
```

### 4. Resolution precedence

Implemented in `CliRepositoryContext.Resolve` (`src/CodeGuard.Cli/Support/CliRepositoryContext.cs`)
— already the shared composition point every command (`validate`, `list-rules`, `explain-rule`,
`list-standards`) calls through, so this is the one place that needs to change:

1. **`--rules-source` CLI flag**, if passed — highest precedence; the most explicit statement of
   intent ("use exactly this, right now"). See §6.
2. `--config` explicit path (unchanged).
3. `<repoRoot>/.codeguard/config.yml`, if present (unchanged).
4. **New**: if neither of the above exists or yields any rules, and `GlobalSettingsStore` has a
   configured source (i.e. `setup` has been run at some point), synthesize
   `Repository.Rules = [ resolvedSourcePath ]`. Only `Rules` comes from global settings —
   `Skills`/`Agents`/`Source`/`Tests` continue to resolve relative to the *target* repo exactly as
   they do today; `setup` only ever answers "where are the rules," never "how is this repo laid
   out."
5. The existing hardcoded `Rules = ["rules"]` fallback stays as the final safety net — it's already
   what this repo's own dogfooding relies on via step 3, so nothing regresses; it just stops being
   the only thing standing between a fresh checkout of some other repo and "0 rules evaluated."

### 5. Git sync algorithm (used by `setup`)

Non-destructive by construction — no force-push, no reset, no overwriting local state without
asking, consistent with this project's general caution around destructive git operations:

- **No local cache yet** → `git clone [--branch <branch>] <url> <cacheDir>`.
- **Cache already exists**:
  1. `git -C <cacheDir> fetch origin [<branch>]`
  2. Compare local `HEAD` against `origin/<branch>` via `git rev-parse`:
     - Equal → report "already up to date," no-op.
     - Local is strictly behind → `git -C <cacheDir> pull --ff-only` (fast-forward only; never a
       merge or force-anything).
     - Diverged, or the working tree has local modifications → **do not touch it.** Report a clear
       error explaining the cache has diverged/has local changes and needs manual resolution (delete
       and let `setup` re-clone, or fix it by hand). Silently discarding someone's local state in a
       cache directory they might not even remember exists is exactly the kind of surprise this
       project's conventions call out to avoid.
- **Directory-kind sources**: no sync step at all — always read live from the given path, since
  there's nothing to clone or fetch.

### 6. `codeguard setup` command shape

- **Interactive by default**: prompts for a rules source (local directory path or git URL), and —
  only if the source looks like a git URL — a branch (leave blank to track the repo's default
  branch, rather than hardcoding `main`).
- **Non-interactive flags**, for scripting/CI bootstrap: `--source <path-or-url>`,
  `--branch <name>`, `--type directory|git` (explicit override of auto-detection). Auto-detection:
  looks like a URL scheme (`http://`, `https://`, `git@`) or ends in `.git` → git; otherwise it must
  be an existing directory.
- **Re-running `setup`**:
  - Same source as already configured → just runs the sync algorithm above (this *is* the "refresh"
    action from §1).
  - Different source → overwrites `settings.yml` and clones fresh into the new source's cache
    directory. The previous cache directory is deliberately left on disk rather than auto-deleted —
    called out here as an accepted non-goal (no silent deletion of a directory the user didn't
    explicitly ask to remove), not an oversight.
- **On success**, prints a short summary including how many rule files were found at the resolved
  location — reusing `RuleFileLoader.CreateDefault().LoadFromDirectories(...)`, the same loader
  `CliRepositoryContext.LoadRules()` already uses — so a misconfigured source (wrong branch, empty
  repo, typo'd path) is caught immediately as "found 0 rule files," not silently discovered later as
  "validate reported 0 rules evaluated."

### 7. Ad-hoc override — skip `setup` entirely

For "I can't be bothered to configure anything, just validate this directory against that
folder/repo of rules, right now":

```
codeguard validate --path . --rules-source https://github.com/org/rules-repo.git
codeguard validate --path . --rules-source ../local-rules-checkout
```

- Add `--rules-source <path-or-url>` (and optional `--branch <name>`) to `CommonOptions`
  (`src/CodeGuard.Cli/Support/CommonOptions.cs`), alongside `--path`/`--config`, so it's available
  uniformly wherever `CliRepositoryContext` is used.
- Shares the same kind-detection and the same content-addressed cache directory derivation from
  `GlobalSettingsPaths` as `setup` (proposed shared helper: `RuleSourceResolver`, used by both
  `SetupCommand` and `CliRepositoryContext` — avoids duplicating the detection logic in two places).
- Directory sources: read live, no caching.
- Git sources: clone into the shared rules-cache **only if not already cached for that URL** —
  meaning this dedups automatically with anything already cloned via `setup`. Deliberately does
  **not** auto-fetch/pull on every invocation, for the same determinism reasoning as §1 — an ad-hoc
  `validate` run should be exactly as reproducible as a configured one. The first use of a brand-new
  URL pays a one-time clone cost; every subsequent use (via `setup` or another ad-hoc call) is a
  local read.
- Purely transient: never writes to `settings.yml`. `--rules-source` and `setup` are orthogonal —
  one is a persisted default, the other a one-off override. No `--save`-to-promote flag is proposed;
  that's already exactly what `setup` is for.
- Precedence: highest of all (§4) — an explicit command-line flag always wins over whatever's
  persisted.

### 8. Where the `git` shelling lives

`src/CodeGuard.Cli/Support/GitRuleSourceSync.cs` — process execution (`Process.Start("git", ...)`)
is a CLI-level concern. Keeping it out of `CodeGuard.Configuration` preserves that project's
current shape (pure YAML/file-system config resolution, no external process dependencies),
consistent with the dependency-direction discipline CLAUDE.md documents for the rest of the
solution.

Chose shelling out to the system `git` binary over a library like `LibGit2Sharp` deliberately: it
reuses the developer's already-configured SSH keys/credential helpers for free, and avoids adding
another per-platform native-dependency surface on top of the Buildalyzer/MSBuild native-dependency
pain already documented in `docs/IMPLEMENTATION_STATUS.md`. The tradeoff is a runtime dependency on
`git` being installed and on `PATH` — a safe assumption for engineers using a tool that validates
.NET repositories they keep in git.

### 9. Test coverage

Matches this repo's existing convention of not testing `System.CommandLine` wiring directly (there
are no tests today for `ValidateCommand`/`ListRulesCommand`/etc.'s command construction) — so none
are proposed for `SetupCommand`'s option wiring either. Coverage focuses on the testable logic
underneath:

- **`GlobalSettingsPathsTests`** — the pure per-OS resolver function, all three branches
  (Windows/macOS/Linux), runnable entirely on `ubuntu-latest`.
- **`GlobalSettingsStoreTests`** — round-trip save/load against a temp directory.
- **`CliRepositoryContextTests`** (new test class) — precedence ordering across all 5 tiers in §4.
- **Git sync tests** — against a throwaway local repo created with `git init` used as the "remote"
  (no network access needed in CI), covering: fresh clone, already-up-to-date no-op, fast-forward
  pull, and the diverged/local-changes-blocked case.

## Proposed file/module layout

```
src/CodeGuard.Configuration/GlobalSettings/
  GlobalSettings.cs            # RuleSourceKind enum + GlobalSettings model
  GlobalSettingsPaths.cs        # cross-platform root + cache-dir resolver (pure function)
  GlobalSettingsStore.cs        # YAML load/save

src/CodeGuard.Cli/
  Commands/SetupCommand.cs
  Support/GitRuleSourceSync.cs  # git clone/fetch/rev-parse/pull --ff-only wrapper
  Support/RuleSourceResolver.cs # shared kind-detection + cache-path logic (SetupCommand + CliRepositoryContext)
  Support/CommonOptions.cs      # + --rules-source / --branch options
  Support/CliRepositoryContext.cs  # + precedence tier 1 and tier 4 (§4)
  Program.cs                    # + rootCommand.Subcommands.Add(SetupCommand.Build())

tests/CodeGuard.Configuration.Tests/
  GlobalSettingsPathsTests.cs
  GlobalSettingsStoreTests.cs

tests/CodeGuard.Cli.Tests/            # new test project, or folded into an existing one
  CliRepositoryContextTests.cs
  GitRuleSourceSyncTests.cs
```

## Non-goals (explicitly out of scope for this feature)

- Auto-refreshing rules on every `validate` run (§1).
- Supporting more than one named rule source at a time (the ask is a single global default; nothing
  here prevents adding named/multiple sources later if it turns out to be needed).
- Cleaning up stale cache directories left behind when `setup` is re-pointed at a different source
  (§6).
- Promoting an ad-hoc `--rules-source` into the persisted global default via some `--save` flag
  (§7) — re-run `setup` for that.

## Verification (once this moves to implementation)

- `dotnet build` / `dotnet test` — the usual full-solution gate (per CLAUDE.md).
- Manual smoke test of the full loop: `codeguard setup --source <a small test git repo>`, confirm
  `settings.yml` and the cache directory appear under the right OS-specific root, confirm
  `codeguard validate --path <some other local repo>` picks up the rules with zero per-repo
  config; re-run `setup` and confirm it reports "already up to date"; make a commit on the source
  repo and re-run `setup` again to confirm it fast-forwards.
- Manual smoke test of the ad-hoc path: `codeguard validate --path . --rules-source
  <path-or-url>` against a repo with no `.codeguard/config.yml` at all and no prior `setup` run.
