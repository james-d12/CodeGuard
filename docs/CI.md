# CI integration

CodeGuard runs anywhere that can run a shell command. There is no CI-specific plugin: each recipe
below installs the CLI, runs `codeguard validate`, keeps the report, and fails the build when the
exit code says so. For the full option list, see the [CLI reference](USAGE.md#cli-reference).

## Exit codes and failure threshold

`validate` exits with one of two codes:

| Exit code | Meaning |
|---|---|
| `0` | No violation at or above `--fail-on`, and no evaluation errors |
| `1` | A violation at or above `--fail-on`, an evaluation error, or a failure before evaluation (no solution found or ambiguous, rule files failing `rules validate`, no rules configured, or an unexpected error) |

Argument errors, such as an unknown `--fail-on` value, also exit with `1`.

The codes do not tell a violation apart from a broken run. Read the report to tell them apart.

`--fail-on` sets the minimum severity that fails the run: `info`, `warning`, `error` or `critical`.
The default is `info`, so any violation fails the build. Recipes below use `--fail-on error` as a
starting point. Two details matter:

- `--severity-threshold` removes violations below its level from the report **and** from the
  exit-code decision. Setting `--severity-threshold error` with `--fail-on warning` never fails on
  warnings.
- A failure before evaluation writes no report file. Upload steps must tolerate a missing file, as
  the recipes do.

## Output formats

| `--format` | Use |
|---|---|
| `console` (default) | Human-readable output on stdout, for build logs |
| `json` | Structured results for scripts and other tools (versioned contract, see [RESULT_CONTRACT.md](architecture/RESULT_CONTRACT.md)) |
| `sarif` | SARIF 2.1.0 for code-scanning platforms |
| `html` | A standalone report page for humans |

Use `--output <file>` to write the report to a file instead of stdout. Logs go to stderr, and
`--verbosity` controls them.

## Install

Every recipe uses the Linux install script, which resolves the latest release, verifies its SHA256
checksum when the release publishes one, extracts it to `$HOME/.codeguard` and makes `codeguard`
available for the current shell. The script also appends a `PATH` entry to the shell profile, which CI does not read. Each
recipe therefore sets `PATH` itself.

```bash
curl --proto '=https' -fsSL https://raw.githubusercontent.com/james-d12/CodeGuard/main/scripts/install.sh | bash
```

To pin a version, pass `--version X.Y.Z` to the script, or set `CODEGUARD_VERSION` before piping
it. Installing the NuGet package is an alternative when the runner already has the .NET SDK and
you prefer to manage tools through NuGet:

```bash
dotnet tool install -g CodeGuard
```

Global tools install to `~/.dotnet/tools`, which must be on `PATH`.

Runners need the .NET SDK for `validate`, because it locates MSBuild at runtime to analyse
`.sln` and `.csproj` files. The GitHub Actions and Azure recipes install the SDK explicitly. The
GitLab, Jenkins and Bitbucket recipes use a .NET SDK image. Pin the SDK your solution targets
rather than relying on the image default. If a repository
has more than one `.sln`, pass `--solution <file>` (repeatable) to choose which to analyse.

Every recipe passes `--rules-source` with a placeholder git URL. Replace it with your rule
source. The runner must be able to clone it, so a private rule repository needs credentials
configured for the CI system, as any private git clone does. Alternatively, commit a
`.codeguard/config.yml` to the repository and drop the `--rules-source` and `--branch` flags.

The recipes are minimal. Add caching, matrix builds or pull request annotations on top of them as
your platform requires.

## GitHub Actions

Uploading SARIF needs the `security-events: write` permission. The upload step runs `if: always()`
so that results are reported even when validation fails, and it is skipped when no report was
written.

```yaml
name: CodeGuard

on:
  pull_request:
  push:
    branches: [main]

permissions:
  contents: read

jobs:
  codeguard:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      security-events: write
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Install CodeGuard
        run: |
          curl --proto '=https' -fsSL https://raw.githubusercontent.com/james-d12/CodeGuard/main/scripts/install.sh | bash
          echo "$HOME/.codeguard" >> "$GITHUB_PATH"

      - name: Validate
        run: >-
          codeguard validate
          --rules-source https://github.com/org/rules-repo.git
          --branch main
          --format sarif
          --output codeguard.sarif
          --fail-on error

      - name: Upload SARIF
        if: always() && hashFiles('codeguard.sarif') != ''
        uses: github/codeql-action/upload-sarif@v4
        with:
          sarif_file: codeguard.sarif
```

## Azure Pipelines

The recipe publishes the SARIF file as a build artifact named `CodeAnalysisLogs`. That name is what
the SARIF SAST Scans Tab extension reads. The extension is a third-party Azure DevOps Marketplace
extension, and this recipe does not install it. Once it is installed, its tab shows the results on
the build.

```yaml
trigger:
  - main

pr:
  - main

pool:
  vmImage: ubuntu-latest

steps:
  - task: UseDotNet@2
    inputs:
      packageType: sdk
      version: '10.0.x'

  - bash: |
      curl --proto '=https' -fsSL https://raw.githubusercontent.com/james-d12/CodeGuard/main/scripts/install.sh | bash
      echo "##vso[task.prependpath]$HOME/.codeguard"
    displayName: Install CodeGuard

  - bash: |
      codeguard validate \
        --rules-source https://github.com/org/rules-repo.git \
        --branch main \
        --format sarif \
        --output "$(Build.ArtifactStagingDirectory)/codeguard.sarif" \
        --fail-on error
    displayName: Run CodeGuard

  - task: PublishBuildArtifacts@1
    condition: always()
    inputs:
      PathtoPublish: $(Build.ArtifactStagingDirectory)/codeguard.sarif
      ArtifactName: CodeAnalysisLogs
      publishLocation: Container
```

## GitLab CI

GitLab does not ingest SARIF natively. Its `artifacts:reports` keys expect GitLab's own report
formats, so a SARIF file uploaded this way is kept as a downloadable artifact only. It does not
appear in the merge request security widget. Use `--format json` if your tooling reads JSON
instead of SARIF.

```yaml
codeguard:
  image: mcr.microsoft.com/dotnet/sdk:10.0
  script:
    - curl --proto '=https' -fsSL https://raw.githubusercontent.com/james-d12/CodeGuard/main/scripts/install.sh | bash
    - export PATH="$HOME/.codeguard:$PATH"
    - codeguard validate --rules-source https://github.com/org/rules-repo.git --branch main --format sarif --output codeguard.sarif --fail-on error
  artifacts:
    when: always
    paths:
      - codeguard.sarif
    expire_in: 1 week
```

## Jenkins

This is a declarative pipeline that runs in a .NET SDK Docker agent, so the agent needs Docker. The
SARIF file is archived with `archiveArtifacts`. It is also parsed by the Warnings Next Generation
plugin's `recordIssues` step, which must be installed on the controller. The `recordIssues` step
runs in `post { always }` so that results are recorded even when validation fails.

```groovy
pipeline {
    agent {
        docker { image 'mcr.microsoft.com/dotnet/sdk:10.0' }
    }
    stages {
        stage('CodeGuard') {
            steps {
                sh '''
                    curl --proto '=https' -fsSL https://raw.githubusercontent.com/james-d12/CodeGuard/main/scripts/install.sh | bash
                    export PATH="$HOME/.codeguard:$PATH"
                    codeguard validate --rules-source https://github.com/org/rules-repo.git --branch main --format sarif --output codeguard.sarif --fail-on error
                '''
            }
        }
    }
    post {
        always {
            archiveArtifacts artifacts: 'codeguard.sarif', allowEmptyArchive: true
            recordIssues(tools: [sarif(pattern: 'codeguard.sarif')])
        }
    }
}
```

## Bitbucket Pipelines

This recipe runs on pull requests, using the .NET SDK image. The `artifacts` entry keeps the SARIF
file for download from the step.

```yaml
image: mcr.microsoft.com/dotnet/sdk:10.0

pipelines:
  pull-requests:
    '**':
      - step:
          name: CodeGuard
          script:
            - curl --proto '=https' -fsSL https://raw.githubusercontent.com/james-d12/CodeGuard/main/scripts/install.sh | bash
            - export PATH="$HOME/.codeguard:$PATH"
            - codeguard validate --rules-source https://github.com/org/rules-repo.git --branch main --format sarif --output codeguard.sarif --fail-on error
          artifacts:
            - codeguard.sarif
```

Bitbucket may not keep step artifacts from a failed step. Check this in your workspace if you need
the report when violations fail the build.

## Notes on these recipes

- The recipes were checked for valid YAML syntax. They were not run on the platforms themselves.
- Image tags and action versions (`actions/checkout@v4`, `actions/setup-dotnet@v4`,
  `github/codeql-action/upload-sarif@v4`, `UseDotNet@2`) should be checked against the platform's
  current releases before use.
