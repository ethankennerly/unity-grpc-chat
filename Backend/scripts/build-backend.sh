#!/usr/bin/env bash
# Wires solution and builds (and tests, if present).
set -euo pipefail
cd "$(dirname "$0")/.."

echo "=== Backend Build ==="

if ! command -v dotnet >/dev/null 2>&1; then
  echo "Error: dotnet SDK not found. Install from https://dotnet.microsoft.com/download" >&2
  exit 1
fi

./scripts/wire-backend.sh

echo "[step] dotnet restore Chat.sln"
dotnet restore Chat.sln

echo "[step] dotnet build Chat.sln -c Debug"
dotnet build Chat.sln -c Debug

if dotnet sln Chat.sln list | grep -qi "Tests"; then
  echo "[step] dotnet test Chat.sln -c Debug"
  dotnet test Chat.sln -c Debug
else
  echo "[info] No test project in solution; skipping tests."
fi

echo "[done] Build complete."
