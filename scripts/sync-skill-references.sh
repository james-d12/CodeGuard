#!/usr/bin/env bash
# Regenerates the rule-authoring skill's reference tables from the engine's parser registries.
#
# These tables used to be hand-maintained and drifted 18 primitives behind the engine, which made the
# skill steer rule generation away from working functionality. Generating them means a primitive
# cannot ship without appearing here.
#
# Run after adding or changing any selector, assertion or analyzer. CI runs this and fails if the
# result differs from what is committed.
set -euo pipefail

cd "$(dirname "$0")/.."

REFS="skills/codeguard-rule-generation/references"
SCHEMA="src/CodeGuard.Configuration/Validation/Schemas/rule.schema.json"

run_codeguard() {
  dotnet run --project src/CodeGuard.Cli --no-build --verbosity quiet -- "$@" 2>/dev/null
}

if [[ "${1:-}" != "--no-build" ]]; then
  dotnet build --nologo --verbosity quiet
fi

# Replaces the region between the generated markers, leaving the surrounding prose alone.
# The generated table goes via a temp file rather than stdin: the python script is itself a heredoc,
# so stdin is already spoken for.
splice() {
  local file="$1" section="$2" table
  table="$(mktemp)"
  trap 'rm -f "$table"' RETURN
  run_codeguard rules discover --format markdown --section "$section" >"$table"

  python3 - "$file" "$section" "$table" <<'PY'
import sys
path, section, table = sys.argv[1], sys.argv[2], sys.argv[3]
generated = open(table, encoding="utf-8").read().rstrip("\n")
begin = f"<!-- BEGIN GENERATED {section} - do not edit by hand; run scripts/sync-skill-references.sh -->"
end = "<!-- END GENERATED -->"

lines = open(path, encoding="utf-8").read().split("\n")
try:
    i, j = lines.index(begin), lines.index(end)
except ValueError:
    sys.exit(f"{path}: missing generated markers for '{section}'")

open(path, "w", encoding="utf-8").write("\n".join(lines[: i + 1] + generated.split("\n") + lines[j:]))
print(f"  {path}")
PY
}

echo "Regenerating skill reference tables:"
splice "$REFS/selectors.md" selectors
splice "$REFS/assertions.md" assertions
splice "$REFS/analyzers.md" analyzers

cp "$SCHEMA" "$REFS/rule-schema.json"
echo "  $REFS/rule-schema.json"
