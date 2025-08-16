#!/usr/bin/env bash
set -euo pipefail

# Enable shell trace when DEBUG=1
[[ "${DEBUG:-0}" == "1" ]] && set -x

echo "=== Backend Install + Build ==="

# Check Homebrew
if ! command -v brew >/dev/null 2>&1; then
  echo "[info] Installing Homebrew..."
  /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
else
  echo "[ok] Homebrew found."
fi

# Check .NET 8 runtime
if ! dotnet --list-runtimes 2>/dev/null | grep -q 'Microsoft.NETCore.App 8\.'; then
  echo "[warn] .NET 8 runtime not found. Installing..."
  brew tap isen-ng/dotnet-sdk-versions || true
  brew install --cask dotnet-sdk8
else
  echo "[ok] .NET 8 runtime found."
fi

# Check .NET SDK (any version)
if ! command -v dotnet >/dev/null 2>&1; then
  echo "[error] .NET SDK not found after install. Please check your PATH."
  exit 1
else
  echo "[ok] .NET SDK: $(dotnet --version)"
fi

echo "=== Wire Backend Solution ==="
if [ ! -f Chat.sln ]; then
  echo "[info] Creating Chat.sln"
  dotnet new sln -n Chat
  dotnet sln Chat.sln add Chat.Proto/Chat.Proto.csproj
  dotnet sln Chat.sln add Chat.Server/Chat.Server.csproj
  dotnet sln Chat.sln add Chat.Tests/Chat.Tests.csproj
else
  echo "[ok] Found Chat.sln"
fi

echo "[step] dotnet restore Chat.sln"
dotnet restore Chat.sln

echo "[step] dotnet build Chat.sln -c Debug"
dotnet build Chat.sln -c Debug

echo "=== Test (Debug, verbose) ==="
# Make test logs clearer:
export DOTNET_CLI_UI_LANGUAGE=en
export NUGET_SHOW_STACK=true
# Show server stdout/stderr in test logs:
dotnet test Chat.sln -c Debug -v n --logger "console;verbosity=detailed"

scripts/sync-all.sh
