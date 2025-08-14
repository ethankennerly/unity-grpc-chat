#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

SERVER="Chat.Server/Chat.Server.csproj"

if [[ ! -f "$SERVER" ]]; then
  echo "Error: $SERVER not found."
  exit 1
fi

echo "Starting server: $SERVER"
dotnet run --project "$SERVER"
