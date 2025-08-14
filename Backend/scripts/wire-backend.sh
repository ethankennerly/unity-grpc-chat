#!/usr/bin/env bash
# Ensures Chat.sln exists and includes backend projects + required references.
# Safe to run multiple times.
set -euo pipefail
cd "$(dirname "$0")/.."

echo "=== Wire Backend Solution ==="

# Project paths (adjust if you rename)
PROTO="Chat.Proto/Chat.Proto.csproj"
SERVER="Chat.Server/Chat.Server.csproj"
TESTS="Chat.Tests/Chat.Tests.csproj"
CLIENTSDK="Chat.Client/Chat.Client.csproj"
CLIENTSDKTESTS="Chat.Client.Tests/Chat.Client.Tests.csproj"

# Create solution if missing
if [[ ! -f "Chat.sln" ]]; then
  echo "[info] Creating Chat.sln"
  dotnet new sln -n Chat
else
  echo "[ok] Found Chat.sln"
fi

# Add a project to the solution if present and not already added
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

# Add a project reference if both exist and the reference is not already present
add_reference_if_missing() {
  local from_proj="$1"   # e.g., Chat.Tests/Chat.Tests.csproj
  local to_proj="$2"     # e.g., Chat.Client/Chat.Client.csproj
  if [[ -f "$from_proj" && -f "$to_proj" ]]; then
    local to_name
    to_name="$(basename "$to_proj")"
    if dotnet list "$from_proj" reference | grep -q "$to_name"; then
      echo "[ok] $(basename "$from_proj") already references $to_name"
    else
      echo "[info] Adding project reference $to_name -> $(basename "$from_proj")"
      dotnet add "$from_proj" reference "$to_proj"
    fi
  else
    echo "[warn] Skipping reference: missing '$from_proj' or '$to_proj'"
  fi
}

# Wire projects
add_if_present "$PROTO"
add_if_present "$SERVER"
add_if_present "$TESTS"
add_if_present "$CLIENTSDK"
add_if_present "$CLIENTSDKTESTS"

# Wire references (idempotent)
# Ensure tests can use the shared client SDK
add_reference_if_missing "$TESTS" "$CLIENTSDK"

echo "[done] Wire complete."
