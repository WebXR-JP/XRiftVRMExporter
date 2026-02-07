#!/usr/bin/env bash
set -euo pipefail

# Check if already activated
ULF_DIR="/root/.local/share/unity3d/Unity"
if find "$ULF_DIR" -name '*.ulf' -print -quit 2>/dev/null | grep -q .; then
  echo "Unity license is already activated. Skipping."
  exit 0
fi

# Validate required environment variables
for var in UNITY_EMAIL UNITY_PASSWORD UNITY_SERIAL; do
  if [[ -z "${!var:-}" ]]; then
    echo "ERROR: $var is not set. Please configure .devcontainer/.env"
    exit 1
  fi
done

echo "Activating Unity license..."
unity-editor \
  -batchmode \
  -nographics \
  -quit \
  -username "$UNITY_EMAIL" \
  -password "$UNITY_PASSWORD" \
  -serial "$UNITY_SERIAL" \
  -logFile /dev/stdout

echo "Unity license activated successfully."
