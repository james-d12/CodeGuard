# One-liner install scripts for CodeGuard releases

## Context

CodeGuard is currently installed either via `dotnet tool install -g CodeGuard` or by manually
downloading and extracting a per-RID archive from GitHub Releases (documented in
`README.md` under "Standalone binaries"). The user wants a proper one-liner installer — one script
for macOS/Linux (`curl ... | bash`) and one for Windows (`irm ... | iex`) — as an alternative to
`dotnet tool install` that doesn't require the tool being pulled from NuGet.

`publish.yml` (triggered on `v*` tags) already builds and uploads self-contained per-RID archives
to the GitHub Release for `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64` (`.tar.gz`) and
`win-x64` (`.zip`), named `codeguard-<version>-<rid>.{tar.gz,zip}`. Each archive is a **folder**,
not a single file — `validate` launches Roslyn's out-of-process MSBuild "BuildHost" from a DLL that
must sit next to the executable, which is why publish.yml deliberately does not use
`PublishSingleFile`. The install scripts must preserve that folder layout (extract to a directory,
not try to lift out a single binary).

Confirmed with the user:
- Scripts live under `scripts/` (matches existing convention: `install-local.sh`,
  `verify-nupkg-contents.sh`, `sync-skill-references.sh`), not the repo root.
- Add SHA256 checksums to the release (new `checksums.txt` asset) and have both scripts verify the
  downloaded archive against it before extracting.
- Scripts should auto-update PATH (shell rc file on macOS/Linux, User PATH on Windows), not just
  print instructions — that's what makes them true one-liners.

## Changes

### 1. `.github/workflows/publish.yml` — add checksums

In the `build-binaries` job's existing "Package Archive" step, after `$asset` is produced, compute
its SHA256 next to it (runner is always `ubuntu-latest` regardless of target RID, so plain
`sha256sum` is available — no `shasum` fallback needed here):

```bash
(cd dist && sha256sum "$(basename "$asset")" > "$(basename "$asset").sha256")
```

Change the "Upload Binary Artifact" step's `path:` from `${{ env.ASSET_PATH }}` to `dist/*` so the
`.sha256` file travels alongside the archive under the same per-matrix artifact name.

In the `release` job, after the existing `actions/download-artifact` (merge-multiple) step and
before `gh release create`, add a step that aggregates the individual hash files into one
`checksums.txt` and removes the now-redundant per-file ones, so the release page shows one
checksums file rather than five extra small ones:

```yaml
- name: Build checksums.txt
  run: |
    cd release-assets
    cat *.sha256 > checksums.txt
    rm *.sha256
```

`gh release create ... release-assets/*` then picks up `checksums.txt` automatically. Don't add a
checksum for the `.nupkg` — NuGet.org already provides its own integrity guarantees for that
artifact, and it's out of scope of what was asked.

### 2. `scripts/install.sh` (new) — macOS/Linux installer

Bash script (`#!/usr/bin/env bash`, `set -euo pipefail`), designed to be run via
`curl -fsSL https://raw.githubusercontent.com/james-d12/CodeGuard/main/scripts/install.sh | bash`,
with `bash -s -- --version X.Y.Z` supported for pinning when invoked directly. Behavior:

- Resolve `version`: from `--version`/`CODEGUARD_VERSION` if set, else query
  `https://api.github.com/repos/james-d12/CodeGuard/releases/latest` and extract `tag_name` (strip
  leading `v`) — avoid a `jq` dependency, use `grep`/`sed` like most shell installers do.
- Resolve `install_dir`: `--install-dir`/`CODEGUARD_INSTALL_DIR`, default `$HOME/.codeguard`.
- Detect RID from `uname -s` (`Linux`→`linux`, `Darwin`→`osx`) and `uname -m`
  (`x86_64`/`amd64`→`x64`, `arm64`/`aarch64`→`arm64`); fail with a clear message for anything else
  (e.g. 32-bit, other OS) — only `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64` exist upstream.
- Download `codeguard-<version>-<rid>.tar.gz` and `checksums.txt` from
  `https://github.com/james-d12/CodeGuard/releases/download/v<version>/...` into a `mktemp -d`
  scratch dir (cleaned up via `trap ... EXIT`).
- Checksum verification: find the archive's line in `checksums.txt`, compute the local file's hash
  (`sha256sum` on Linux, `shasum -a 256` on macOS), compare, abort with a clear error on mismatch.
  If `checksums.txt` 404s (older releases predate this feature), print a warning and continue
  without verification rather than hard-failing — keeps `CODEGUARD_VERSION` pinning to older tags
  working.
- Extract: `mkdir -p "$install_dir"` then `tar -xzf <archive> -C "$install_dir"`.
- macOS only: `xattr -dr com.apple.quarantine "$install_dir" 2>/dev/null || true` (matches the
  Gatekeeper caveat already documented in `README.md`).
- PATH update: skip if `$install_dir` is already on `PATH`. Otherwise pick a profile file
  (`~/.zshrc` if `$SHELL` contains `zsh`, else `~/.bashrc`, else `~/.profile`) and idempotently
  append a marker-guarded block:
  ```
  # >>> codeguard install >>>
  export PATH="$install_dir:$PATH"
  # <<< codeguard install <<<
  ```
  (check for the marker first so re-running the installer doesn't duplicate it). Tell the user to
  restart their shell or `source` the file.
- Verify by running `"$install_dir/codeguard" --help` and report the installed version/location on
  success; non-zero exit with diagnostic output on failure.
- Print the same ".NET SDK still required for `validate`" caveat that's already in `README.md`, so
  the one-liner doesn't create a false impression of a fully standalone tool.

### 3. `scripts/install.ps1` (new) — Windows installer

PowerShell script for `irm https://raw.githubusercontent.com/james-d12/CodeGuard/main/scripts/install.ps1 | iex`,
with a `param(...)` block (`-Version`, `-InstallDir`) plus env var fallback
(`CODEGUARD_VERSION`/`CODEGUARD_INSTALL_DIR`) so it also works when piped into `iex` (no args
possible there). `$ErrorActionPreference = 'Stop'`. Behavior mirrors `install.sh`:

- Resolve version via `Invoke-RestMethod` against the same `releases/latest` API endpoint if not
  pinned.
- Only `win-x64` is published — check
  `[System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture`; fail with a clear message
  if it's not `X64` (no `win-arm64` asset exists upstream today).
- Default `InstallDir`: `Join-Path $env:LOCALAPPDATA 'CodeGuard'`.
- Download `codeguard-<version>-win-x64.zip` and `checksums.txt` to a temp dir; verify with
  `Get-FileHash -Algorithm SHA256` against the parsed `checksums.txt` line, same soft-fail-if-missing
  behavior as the bash script.
- `Expand-Archive -Force` into `InstallDir`.
- PATH update: read `[Environment]::GetEnvironmentVariable('Path','User')`, append `InstallDir` if
  not already present, `SetEnvironmentVariable(...,'User')`, and also prepend to `$env:Path` for the
  *current* session so the verification step below (and the user's current terminal) doesn't need a
  restart.
- Verify by invoking `codeguard.exe --help` from `InstallDir` and print success/failure.
- Same ".NET SDK still required for `validate`" caveat in the final message.

### 4. `README.md` — "Standalone binaries" section

Add the one-liner commands as the primary/recommended path, ahead of the existing manual
curl+tar / Invoke-WebRequest+Expand-Archive steps (keep those as a "manual install" fallback for
users who don't want to pipe a script into a shell — link to the script's raw GitHub path so they
can read it first). Mention that release archives are now checksummed and the scripts verify this
automatically. Keep the existing macOS Gatekeeper and ".NET SDK still required" callouts as-is —
both scripts are described above to already fold those in.

## Verification

No new tag will be pushed as part of this change (that's a separate, user-triggered release
action), so full end-to-end testing against a real `checksums.txt`-bearing release isn't possible
until the next `v*` tag ships with the updated `publish.yml`. Verify what's checkable now:

- `bash -n scripts/install.sh` (syntax check); run `shellcheck scripts/install.sh` if available.
- Exercise the OS/arch-detection and PATH-append logic against a throwaway fake `HOME` (e.g.
  `HOME=$(mktemp -d) CODEGUARD_INSTALL_DIR=$(mktemp -d) bash scripts/install.sh`) pointed at the
  **current, pre-checksum** latest release, confirming: correct RID chosen, archive downloads and
  extracts, checksum step warns-and-continues (since today's releases have no `checksums.txt`),
  `codeguard --help` runs from the install dir, and the rc-file block is appended exactly once even
  if run twice.
- If PowerShell (`pwsh`) is available locally, run `Set-StrictMode -Version Latest` +
  `Test-Path`/manual invocation against a scratch `-InstallDir`, and `Invoke-ScriptAnalyzer` if
  installed.
- Review the `publish.yml` diff by eye for YAML correctness (no CI run will exercise it until the
  next tag push); confirm `gh release create release-assets/*` behavior by reading `dist/*` upload
  changes carefully since this can't be dry-run locally.
