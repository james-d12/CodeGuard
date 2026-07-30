#!/usr/bin/env bash
# Fails the build if a packed .nupkg contains any rule-authoring content.
#
# rules/ (ddd, architecture, csharp, generated) is this repo's own example/company-derived
# rule set - it must never ship inside the public RulesEngine tool package. Only the JSON
# schema is meant to travel with the tool, and that's already embedded as a resource inside
# RulesEngine.Configuration.dll, so it never appears as a loose file in the package listing.
#
# Usage: scripts/verify-nupkg-contents.sh <path-to-nupkg-or-glob>
set -euo pipefail

if [ "$#" -lt 1 ]; then
    echo "Usage: $0 <path-to-nupkg>" >&2
    exit 2
fi

status=0

for nupkg in "$@"; do
    if [ ! -f "$nupkg" ]; then
        echo "error: nupkg not found: $nupkg" >&2
        exit 2
    fi

    echo "Inspecting $nupkg"
    listing=$(unzip -l "$nupkg")

    # Any loose rule-authoring content (a "rules/" path component, or a .yaml/.yml file)
    # would mean company-derived rule content leaked into the public package.
    if echo "$listing" | grep -Eiq '(^|/)rules/'; then
        echo "FAIL: $nupkg contains a rules/ path - company rule content must not be packaged." >&2
        echo "$listing" | grep -Ei '(^|/)rules/' >&2
        status=1
    fi

    if echo "$listing" | grep -Eiq '\.ya?ml$'; then
        echo "FAIL: $nupkg contains YAML file(s) - rule definitions must not be packaged." >&2
        echo "$listing" | grep -Ei '\.ya?ml$' >&2
        status=1
    fi
done

if [ "$status" -eq 0 ]; then
    echo "OK: no rule-authoring content found in package(s)."
fi

exit $status
