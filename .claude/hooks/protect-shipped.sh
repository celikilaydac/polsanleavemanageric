#!/bin/sh
# PreToolUse hook: files under specs/done/ are immutable (AGENTS.md invariant rule 7).
# Reads the tool-call JSON from stdin, blocks Edit/Write/NotebookEdit targeting specs/done/.
# Local divergence from adapters/claude-code (bootstrap review B-1/N-6, to be reported upstream):
# also reads notebook_path, normalizes Windows backslashes and case, and rejects '..' segments.
input=$(cat)
path=$(printf '%s' "$input" | grep -oE '"(file_path|notebook_path)"[[:space:]]*:[[:space:]]*"[^"]*"' | head -n 1 \
  | sed 's/^"[a-z_]*"[[:space:]]*:[[:space:]]*"//; s/"$//' | tr '\\' '/' | tr -s '/' | tr '[:upper:]' '[:lower:]' | sed 's#/\./#/#g; s#/\./#/#g; s#^\./##')
block() {
  echo "Blocked: $1" >&2
  exit 2
}
case "$path" in
  *specs/done/*)
    block "specs/done/ is immutable (shipped specs are the historical record — AGENTS.md rule 7). Behavior changing? Open a NEW spec in specs/active/ referencing the old one."
    ;;
  */../*|../*)
    block "paths with '..' segments are not allowed (specs/done/ protection, AGENTS.md rule 7). Use a direct path."
    ;;
esac
exit 0
