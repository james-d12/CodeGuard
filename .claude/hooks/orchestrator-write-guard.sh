#!/usr/bin/env bash
# PreToolUse hook for the orchestrator agent: it may only Write/Edit markdown files
# directly under docs/orchestration/. Everything else must go through a task-worker.
# Exit 2 blocks the tool call and feeds stderr back to the agent.
set -euo pipefail

input="$(cat)"
path="$(printf '%s' "$input" | jq -r '.tool_input.file_path // empty')"

if [[ -z "$path" ]]; then
  exit 0
fi

if [[ "$path" =~ (^|/)docs/orchestration/[^/]+\.md$ ]]; then
  exit 0
fi

echo "orchestrator may only write run logs under docs/orchestration/ (attempted: $path). Delegate this change to a task-worker." >&2
exit 2
