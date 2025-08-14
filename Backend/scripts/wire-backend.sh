#!/usr/bin/env bash
# Ensures Chat.sln exists and includes the existing backend projects.
# Safe to run multiple times.
set -euo pipefail
cd "$(dirname "$0")/.."

echo "=== Wire Backend Solution ==="

# Expected projects (adjust names if you rename folders/files)
PROTO="Chat.Proto/Chat.Proto.csproj"
SERVER="Chat.Server/Chat.Server.csproj"
TESTS="Chat.Tests/Chat.Tests.csproj"

# Create solution if missing
if [[ ! -f "Chat.sln" ]]; then
  echo "[info] Creating Chat.sln"
  dotnet new sln -n Chat
else
  echo "[ok] Found Chat.sln"
fi

# Add projects that exist (skip if missing)
add_if_present() {
  local proj="$1"
  if [[ -f "$proj" ]]; then
    if dotnet sln Chat.sln list | grep -q "$(basename "$proj")"; then
      echo "[ok] Already in solution: $proj"
    else
      echo "[info] Adding to solution: $proj"
      dotnet sln Chat.sln add "$proj"
    fi
  else
    echo "[warn] Not found (skipped): $proj"
  fi
}

add_if_present "$PROTO"
add_if_present "$SERVER"
add_if_present "$TESTS"

echo "[done] Wire complete."
