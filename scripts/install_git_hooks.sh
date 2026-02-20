#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

chmod +x .githooks/pre-commit
git config core.hooksPath .githooks

echo "Installed git hooks."
echo "hooksPath: $(git config --get core.hooksPath)"
echo ""
echo "pre-commit now enforces local release checks for Windows-related changes."
echo "To temporarily skip heavy preflight once:"
echo "  RUN_FULL_PRECOMMIT_PRECHECK=0 git commit -m \"...\""
