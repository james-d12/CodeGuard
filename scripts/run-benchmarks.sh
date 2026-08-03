#!/usr/bin/env bash
# Runs the BenchmarkDotNet suite in benchmarks/CodeGuard.Benchmarks, always in Release
# configuration - benchmarks measure timing, and Debug JIT output makes the numbers meaningless.
#
# This is a separate, developer-run step, not part of `dotnet test`/CI (see
# docs/IMPLEMENTATION_STATUS.md gotcha #11): a shared/throttled CI runner produces noisy,
# unrepresentative timing numbers, and a multi-minute perf run shouldn't gate every push.
#
# Usage: scripts/run-benchmarks.sh [BenchmarkDotNet args...]
#   scripts/run-benchmarks.sh                                # interactive picker
#   scripts/run-benchmarks.sh --filter '*RuleEvaluation*'     # run one benchmark class
#   scripts/run-benchmarks.sh --filter '*' --job dry          # quick single-iteration smoke test
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
benchmarks_project="$repo_root/benchmarks/CodeGuard.Benchmarks/CodeGuard.Benchmarks.csproj"

dotnet run -c Release --project "$benchmarks_project" -- "$@"
