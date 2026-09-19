<#
.SYNOPSIS
  Installs the latest (or a pinned) CodeGuard release as a self-contained binary, as an
  alternative to `dotnet tool install -g CodeGuard`. No .NET SDK required to install or launch
  `codeguard` itself (though `validate` still needs one on PATH at runtime - see README.md).

.EXAMPLE
  irm https://raw.githubusercontent.com/james-d12/CodeGuard/main/scripts/install.ps1 | iex

.EXAMPLE
  .\install.ps1 -Version 1.2.3 -InstallDir "$env:LOCALAPPDATA\CodeGuard-1.2.3"

.NOTES
  When piped into `iex` no arguments can be passed, so version/install-dir can also be set via
  the CODEGUARD_VERSION / CODEGUARD_INSTALL_DIR environment variables before running the command.
#>
param(
    [string]$Version,
    [string]$InstallDir
)

$ErrorActionPreference = 'Stop'

$repo = "james-d12/CodeGuard"

if (-not $Version) { $Version = $env:CODEGUARD_VERSION }
if (-not $InstallDir) { $InstallDir = $env:CODEGUARD_INSTALL_DIR }
if (-not $InstallDir) { $InstallDir = Join-Path $env:LOCALAPPDATA 'CodeGuard' }

$arch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture
if ($arch -ne [System.Runtime.InteropServices.Architecture]::X64) {
    Write-Error "install.ps1: unsupported architecture '$arch' - CodeGuard only publishes win-x64 binaries today."
    exit 1
}

if (-not $Version) {
    Write-Host "Resolving latest CodeGuard release..."
    $release = Invoke-RestMethod -Uri "https://api.github.com/repos/$repo/releases/latest"
    $Version = $release.tag_name.TrimStart('v')
}
Write-Host "Installing CodeGuard $Version"

$asset = "codeguard-$Version-win-x64.zip"
$baseUrl = "https://github.com/$repo/releases/download/v$Version"

$workDir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.IO.Path]::GetRandomFileName())
New-Item -ItemType Directory -Path $workDir | Out-Null
try {
    $assetPath = Join-Path $workDir $asset
    Write-Host "Downloading $asset..."
    Invoke-WebRequest -Uri "$baseUrl/$asset" -OutFile $assetPath

    $checksumsPath = Join-Path $workDir 'checksums.txt'
    $checksumsOk = $true
    try {
        Invoke-WebRequest -Uri "$baseUrl/checksums.txt" -OutFile $checksumsPath
    } catch {
        $checksumsOk = $false
        Write-Warning "checksums.txt not found for this release (older release?), skipping verification"
    }

    if ($checksumsOk) {
        $line = Select-String -Path $checksumsPath -Pattern ([Regex]::Escape($asset)) | Select-Object -First 1
        if (-not $line) {
            Write-Warning "$asset not listed in checksums.txt, skipping verification"
        } else {
            $expectedHash = ($line.Line -split '\s+')[0].ToLowerInvariant()
            $actualHash = (Get-FileHash -Path $assetPath -Algorithm SHA256).Hash.ToLowerInvariant()
            if ($actualHash -ne $expectedHash) {
                Write-Error "install.ps1: checksum mismatch for $asset (expected $expectedHash, got $actualHash)"
                exit 1
            }
            Write-Host "Checksum verified."
        }
    }

    Write-Host "Installing to $InstallDir..."
    New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
    Expand-Archive -Path $assetPath -DestinationPath $InstallDir -Force
} finally {
    Remove-Item -Recurse -Force $workDir -ErrorAction SilentlyContinue
}

$userPath = [Environment]::GetEnvironmentVariable('Path', 'User')
$pathEntries = $userPath -split ';' | Where-Object { $_ -ne '' }
if ($pathEntries -notcontains $InstallDir) {
    [Environment]::SetEnvironmentVariable('Path', "$userPath;$InstallDir", 'User')
    Write-Host "Added $InstallDir to your User PATH (new terminals will pick it up)."
}
if (($env:Path -split ';') -notcontains $InstallDir) {
    $env:Path = "$InstallDir;$env:Path"
}

$exePath = Join-Path $InstallDir 'codeguard.exe'
& $exePath --help | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Error "install.ps1: installed binary at $exePath did not run successfully"
    exit 1
}

Write-Host ""
Write-Host "CodeGuard $Version installed to $InstallDir"
Write-Host "Note: 'codeguard validate' still needs a .NET SDK installed on this machine (it locates"
Write-Host "MSBuild at runtime via MSBuildLocator) - see README.md for details."
