#!/usr/bin/env bash
set -euo pipefail

echo "=== Post-create setup ==="

# 1. Initialize and update git submodules
echo "Initializing git submodules..."
cd /workspace
git submodule update --init --recursive

# 2. Resolve VPM packages
echo "Resolving VPM packages..."
if command -v vrc-get &>/dev/null; then
  vrc-get resolve --project /workspace
else
  echo "WARNING: vrc-get not found. Skipping VPM package resolution."
fi

# 3. Check Unity license status
ULF_DIR="/root/.local/share/unity3d/Unity"
if find "$ULF_DIR" -name '*.ulf' -print -quit 2>/dev/null | grep -q .; then
  echo "Unity license is already activated."
else
  echo ""
  echo "============================================"
  echo " Unity license is NOT activated yet."
  echo " Run the following command to activate:"
  echo ""
  echo "   bash .devcontainer/scripts/activate-unity.sh"
  echo "============================================"
  echo ""
fi

echo "=== Post-create setup complete ==="
