#!/usr/bin/env bash
# Installs the latest (or a pinned) CodeGuard release as a self-contained binary, as an
# alternative to `dotnet tool install -g CodeGuard`. No .NET SDK required to install or launch
# `codeguard` itself (though `validate` still needs one on PATH at runtime - see README.md).
#
# Usage:
#   curl --proto '=https' -fsSL https://raw.githubusercontent.com/james-d12/CodeGuard/main/scripts/install.sh | bash
#   curl --proto '=https' -fsSL .../install.sh | bash -s -- --version 1.2.3 --install-dir "$HOME/tools/codeguard"
#
# Env var overrides (equivalent to the flags above, useful when piping into `bash` with no args):
#   CODEGUARD_VERSION, CODEGUARD_INSTALL_DIR
set -euo pipefail

repo="james-d12/CodeGuard"
version="${CODEGUARD_VERSION:-}"
install_dir="${CODEGUARD_INSTALL_DIR:-$HOME/.codeguard}"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --version)
      version="$2"
      shift 2
      ;;
    --install-dir)
      install_dir="$2"
      shift 2
      ;;
    -h|--help)
      echo "Usage: install.sh [--version X.Y.Z] [--install-dir DIR]"
      echo "Env vars: CODEGUARD_VERSION, CODEGUARD_INSTALL_DIR"
      exit 0
      ;;
    *)
      echo "install.sh: unknown argument: $1" >&2
      exit 1
      ;;
  esac
done

if [[ -z "$version" ]]; then
  echo "Resolving latest CodeGuard release..."
  latest_json="$(curl --proto "=https" -fsSL "https://api.github.com/repos/${repo}/releases/latest")"
  version="$(printf '%s' "$latest_json" | grep -m1 '"tag_name"' | sed -E 's/.*"tag_name": *"v?([^"]+)".*/\1/')"
  if [[ -z "$version" ]]; then
    echo "install.sh: could not resolve the latest release version from the GitHub API" >&2
    exit 1
  fi
fi
echo "Installing CodeGuard $version"

os="$(uname -s)"
arch="$(uname -m)"
case "$os" in
  Linux) os_part="linux" ;;
  Darwin) os_part="osx" ;;
  *)
    echo "install.sh: unsupported OS '$os' - CodeGuard only publishes Linux and macOS binaries here" >&2
    echo "install.sh: on Windows, use scripts/install.ps1 instead" >&2
    exit 1
    ;;
esac
case "$arch" in
  x86_64|amd64) arch_part="x64" ;;
  arm64|aarch64) arch_part="arm64" ;;
  *)
    echo "install.sh: unsupported architecture '$arch'" >&2
    exit 1
    ;;
esac
rid="${os_part}-${arch_part}"

asset="codeguard-${version}-${rid}.tar.gz"
base_url="https://github.com/${repo}/releases/download/v${version}"

work_dir="$(mktemp -d)"
trap 'rm -rf "$work_dir"' EXIT

echo "Downloading ${asset}..."
curl --proto "=https" -fsSL "${base_url}/${asset}" -o "${work_dir}/${asset}"

echo "Fetching checksums.txt..."
if curl --proto "=https" -fsSL "${base_url}/checksums.txt" -o "${work_dir}/checksums.txt" 2>/dev/null; then
  expected_line="$(grep "  ${asset}\$" "${work_dir}/checksums.txt" || true)"
  if [[ -z "$expected_line" ]]; then
    echo "install.sh: warning: ${asset} not listed in checksums.txt, skipping verification" >&2
  else
    if command -v sha256sum >/dev/null 2>&1; then
      actual_hash="$(sha256sum "${work_dir}/${asset}" | awk '{print $1}')"
    else
      actual_hash="$(shasum -a 256 "${work_dir}/${asset}" | awk '{print $1}')"
    fi
    expected_hash="$(printf '%s' "$expected_line" | awk '{print $1}')"
    if [[ "$actual_hash" != "$expected_hash" ]]; then
      echo "install.sh: checksum mismatch for ${asset}" >&2
      echo "install.sh: expected ${expected_hash}, got ${actual_hash}" >&2
      exit 1
    fi
    echo "Checksum verified."
  fi
else
  echo "install.sh: warning: checksums.txt not found for this release (older release?), skipping verification" >&2
fi

echo "Installing to ${install_dir}..."
mkdir -p "$install_dir"
tar -xzf "${work_dir}/${asset}" -C "$install_dir"

if [[ "$os" = "Darwin" ]] && command -v xattr >/dev/null 2>&1; then
  xattr -dr com.apple.quarantine "$install_dir" 2>/dev/null || true
fi

chmod +x "${install_dir}/codeguard"

case ":${PATH}:" in
  *":${install_dir}:"*)
    ;;
  *)
    case "${SHELL:-}" in
      */zsh) profile_file="$HOME/.zshrc" ;;
      */bash) profile_file="$HOME/.bashrc" ;;
      *) profile_file="$HOME/.profile" ;;
    esac
    marker_begin="# >>> codeguard install >>>"
    marker_end="# <<< codeguard install <<<"
    if [[ -f "$profile_file" ]] && grep -qF "$marker_begin" "$profile_file"; then
      echo "PATH entry already present in ${profile_file}."
    else
      {
        echo ""
        echo "$marker_begin"
        echo "export PATH=\"${install_dir}:\$PATH\""
        echo "$marker_end"
      } >> "$profile_file"
      echo "Added ${install_dir} to PATH in ${profile_file}."
    fi
    echo "Restart your shell or run 'source ${profile_file}' to use 'codeguard' right away."
    ;;
esac

if "${install_dir}/codeguard" --help >/dev/null 2>&1; then
  echo ""
  echo "CodeGuard ${version} installed to ${install_dir}"
  echo "Note: 'codeguard validate' still needs a .NET SDK installed on this machine (it locates"
  echo "MSBuild at runtime via MSBuildLocator) - see README.md for details."
else
  echo "install.sh: installed binary at ${install_dir}/codeguard did not run successfully" >&2
  exit 1
fi
