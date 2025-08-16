#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

PORT="${1:-5000}"
DATA_DIR="data"
DB_FILE="${2:-chat-dev.db}"

SERVER="Chat.Server/Chat.Server.csproj"

mkdir -p "$DATA_DIR"
URL="http://127.0.0.1:${PORT}"
export ASPNETCORE_URLS="$URL"
export CHAT_CONNECTION="Data Source=./${DATA_DIR}/${DB_FILE}"

if [[ ! -f "$SERVER" ]]; then
  echo "Error: $SERVER not found."
  exit 1
fi

echo "Starting server on ${URL}"
echo "DB: ${CHAT_CONNECTION}"
dotnet run --project "$SERVER" --configuration Debug