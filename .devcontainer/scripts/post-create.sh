#!/usr/bin/env bash
set -euo pipefail

echo "=== Post-create setup ==="

# Allow workspace directory owned by different user (bind mount)
git config --global --add safe.directory /workspace

# 1. Initialize and update git submodules
echo "Initializing git submodules..."
cd /workspace
if ! git submodule update --init --recursive; then
  echo "WARNING: git submodule update failed (credentials may not be available in container)."
  echo "You can retry manually after setting up git credentials."
fi

# 2. Resolve VPM packages
echo "Resolving VPM packages..."
if command -v vrc-get &>/dev/null; then
  if ! vrc-get resolve --project /workspace; then
    echo "WARNING: vrc-get resolve failed. VPM package repos may need to be added first."
    echo "You can retry manually: vrc-get resolve --project /workspace"
  fi
else
  echo "WARNING: vrc-get not found. Skipping VPM package resolution."
fi

# 3. Check Unity license status
ULF_DIR="$HOME/.local/share/unity3d/Unity"
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
