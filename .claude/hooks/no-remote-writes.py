#!/usr/bin/env python3
"""PreToolUse hook for Bash: block commands that write to a git remote.

Used by task-worker and merge-conflict-resolver. They may commit on their current
branch, check out, inspect and stage, but must never push, open PRs or otherwise
touch the remote. The orchestrator (or the user) does that.

Reads the hook JSON on stdin, exits 2 with a reason on stderr to block, 0 to allow.
Run with --self-test to exercise the matcher.
"""
import json
import re
import shlex
import sys

# git subcommands that write to a remote, or make a push possible under another name
BLOCKED_GIT = {
    "push": "pushes to the remote",
    "send-email": "sends patches to a remote",
    "svn": "git-svn can commit to a remote",
    "request-pull": "remote interaction",
}
# git subcommands where only some forms are a problem
BLOCKED_GIT_FORMS = {
    "remote": (re.compile(r"^(add|set-url|rename|remove|rm|prune|update|set-head|set-branches)$"),
               "changes remotes; the worker may only read them"),
    "config": (re.compile(r"alias\.|url\.|remote\.|push\.|credential\."),
               "could redefine git commands or remotes"),
}
# gh subcommands that are read-only; everything else under gh is blocked
GH_READ_ONLY = {
    "pr": {"view", "list", "diff", "checks", "status"},
    "issue": {"view", "list", "status"},
    "run": {"view", "list", "watch"},
    "release": {"view", "list"},
    "repo": {"view"},
    "search": None,  # whole subcommand is read-only
    "auth": {"status"},
}
GIT_GLOBAL_OPTS_WITH_ARG = {"-C", "-c", "--git-dir", "--work-tree", "--namespace", "--exec-path", "--config-env"}
SPLIT_RE = re.compile(r"\|\||&&|[;|\n]|\$\(|`|\(")


def is_git(tok):
    return tok == "git" or tok.endswith("/git")


def is_gh(tok):
    return tok == "gh" or tok.endswith("/gh")


def check_git(args):
    """args = tokens after 'git'. Return a reason to block, or None."""
    i = 0
    while i < len(args) and args[i].startswith("-"):
        if args[i] in GIT_GLOBAL_OPTS_WITH_ARG:
            i += 1
        i += 1
    if i >= len(args):
        return None
    sub = args[i]
    rest = args[i + 1:]
    if sub in BLOCKED_GIT:
        return f"git {sub}: {BLOCKED_GIT[sub]}"
    if sub in BLOCKED_GIT_FORMS:
        pat, why = BLOCKED_GIT_FORMS[sub]
        if any(pat.search(a) for a in rest):
            return f"git {sub} {' '.join(rest)}: {why}"
    return None


WRAPPERS = {"sh", "bash", "zsh", "dash", "ksh", "eval", "exec", "xargs", "env", "timeout", "nohup",
            "nice", "sudo", "command", "su", "time", "watch"}
GH_API_WRITE_FLAGS = {"-X", "--method", "-f", "-F", "--field", "--raw-field", "--input"}


def check_gh(args):
    positional = [a for a in args if not a.startswith("-")]
    if not positional:
        return None
    sub = positional[0]
    verb = positional[1] if len(positional) > 1 else ""
    if sub == "api":
        # GET is fine; any method/body flag makes it a write
        if any(a in GH_API_WRITE_FLAGS or a.split("=", 1)[0] in GH_API_WRITE_FLAGS for a in args):
            return "gh api with a write method/body"
        return None
    allowed = GH_READ_ONLY.get(sub)
    if allowed is None and sub in GH_READ_ONLY:
        return None
    if allowed is not None and verb in allowed:
        return None
    return f"gh {sub} {verb}: only read-only gh commands are allowed".rstrip()


def scan_tokens(tokens):
    for idx, tok in enumerate(tokens):
        if is_git(tok):
            why = check_git(tokens[idx + 1:])
            if why:
                return why
        elif is_gh(tok):
            why = check_gh(tokens[idx + 1:])
            if why:
                return why
        elif any(c.isspace() for c in tok) and tokens and tokens[0].rsplit("/", 1)[-1] in WRAPPERS:
            # a quoted string handed to an interpreter/wrapper (sh -c "...", eval "...", xargs ...)
            why = check_command(tok)
            if why:
                return why
    return None


def check_command(cmd):
    for segment in SPLIT_RE.split(cmd):
        segment = segment.strip()
        if not segment:
            continue
        try:
            tokens = shlex.split(segment)
        except ValueError:
            tokens = segment.split()
        # drop leading VAR=value assignments
        while tokens and re.match(r"^[A-Za-z_][A-Za-z0-9_]*=", tokens[0]):
            tokens.pop(0)
        why = scan_tokens(tokens)
        if why:
            return why
    # last-resort textual check for anything the tokenizer could not see through
    if re.search(r"\bgit\b.*\bpush\b", cmd, re.S) and re.search(r"\b(eval|xargs|sh|bash|zsh|exec|python3?|perl)\b", cmd):
        return "indirect git push"
    return None


def self_test():
    allow = [
        "git status", "git add -A && git commit -m 'x'", "git checkout -b feature/x", "git switch main",
        "git log --oneline -5", "git diff --stat", "git fetch origin", "git pull --ff-only",
        "git remote -v", "git remote get-url origin", "git config user.name", "git log --grep push",
        "dotnet build && dotnet test", "gh pr view 100", "gh pr list", "gh api repos/o/r/pulls/1",
        "git -C /tmp/x commit -am msg", "echo 'git push' > notes.txt",
    ]
    block = [
        "git push", "git push -u origin HEAD", "git -C /tmp/x push", "git --no-pager push origin main",
        "cd repo && git push", "dotnet test; git push --force", "timeout 60 git push",
        "sh -c 'git push origin main'", "bash -c \"cd x && git push\"", "echo origin | xargs git push",
        "GIT_SSH_COMMAND=ssh git push", "/usr/bin/git push", "git remote set-url origin evil",
        "git remote add other https://x", "git config alias.p push", "git config remote.origin.url x",
        "gh pr create --title x", "gh pr merge 100", "gh api -X POST repos/o/r/pulls", "gh release create v1",
        "gh pr comment 100 --body hi", "git send-email HEAD~1", "eval \"git push\"",
    ]
    bad = [(c, "allowed") for c in block if check_command(c) is None] + \
          [(c, "blocked") for c in allow if check_command(c) is not None]
    for c, what in bad:
        print(f"FAIL: unexpectedly {what}: {c}")
    print(f"{len(allow) + len(block) - len(bad)}/{len(allow) + len(block)} cases pass")
    return 1 if bad else 0


def main():
    if len(sys.argv) > 1 and sys.argv[1] == "--self-test":
        sys.exit(self_test())
    try:
        payload = json.load(sys.stdin)
    except json.JSONDecodeError:
        sys.exit(0)
    cmd = (payload.get("tool_input") or {}).get("command") or ""
    why = check_command(cmd)
    if why:
        print(f"blocked: {why}. Workers commit on their own branch only; the orchestrator (or the user) pushes and opens PRs.", file=sys.stderr)
        sys.exit(2)
    sys.exit(0)


if __name__ == "__main__":
    main()
