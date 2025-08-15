#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
BACKEND="$ROOT/Backend"
PROTO_PROJ="$BACKEND/Chat.Proto/Chat.Proto.csproj"
OUT_DIR="$ROOT/Assets/Generated/ChatProto"

echo "=== Generate Unity proto C# ==="

if [[ ! -f "$PROTO_PROJ" ]]; then
  echo "Error: $PROTO_PROJ not found." >&2
  exit 1
fi

dotnet build "$PROTO_PROJ" -c Debug >/dev/null

GEN_ROOT="$(dirname "$PROTO_PROJ")/obj"

rm -rf "$OUT_DIR"
mkdir -p "$OUT_DIR"

count=0
while IFS= read -r -d '' f; do
  echo "[copy] $f"
  cp "$f" "$OUT_DIR/"
  count=$((count + 1))
done < <(
  find "$GEN_ROOT" -type f -name '*.cs' \
    \( -name '*Grpc*.cs' -o -name '*pb.cs' -o -name '*Reflection*.cs' -o -name 'Chat*.cs' \) \
    ! -name '*GlobalUsings*.cs' \
    ! -name '*AssemblyInfo*.cs' \
    -print0 | sort -z
)

if [[ $count -eq 0 ]]; then
  echo "Error: No generated proto C# files found under $GEN_ROOT" >&2
  echo "Hint: Ensure Chat.Proto.csproj has <Protobuf Include=\"chat.proto\" CompileOutputs=\"true\" />" >&2
  exit 1
fi

ASMDEF="$OUT_DIR/Chat.Proto.Runtime.asmdef"
if [[ ! -f "$ASMDEF" ]]; then
  cat > "$ASMDEF" <<'JSON'
{
  "name": "Chat.Proto.Runtime",
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
JSON
fi

echo "[ok] Generated $count files into $OUT_DIR"