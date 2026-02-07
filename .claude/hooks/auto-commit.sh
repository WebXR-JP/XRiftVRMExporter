#!/bin/bash
# Claude Code の応答完了時に自動コミットするフック

cd "$CLAUDE_PROJECT_DIR" || exit 0

# gitリポジトリでなければ何もしない
git rev-parse --is-inside-work-tree >/dev/null 2>&1 || exit 0

# 変更がなければ何もしない
if git diff --quiet && git diff --cached --quiet && [ -z "$(git ls-files --others --exclude-standard)" ]; then
  exit 0
fi

# 変更をステージしてコミット
git add -A
TIMESTAMP=$(date '+%Y-%m-%d %H:%M:%S')
git commit -m "auto-commit by Claude Code [$TIMESTAMP]" >/dev/null 2>&1

exit 0
