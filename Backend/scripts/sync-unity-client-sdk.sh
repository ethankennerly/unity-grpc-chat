#!/usr/bin/env bash
set -euo pipefail

# Sync a minimal, Unity-safe client SDK into Assets.
# Copies only .cs runtime sources (no bin/obj/DLLs/CSProj/SLN/Roslyn g.cs).
# Idempotent: cleans destination (unless --no-clean). Non-strict by default.
# Usage:
#   scripts/sync-unity-client-sdk.sh [--src PATH] [--dst PATH] [--dry-run] [--no-clean] [--verbose] [--strict]
#
# Defaults:
#   --src Backend/Chat.Client
#   --dst Assets/Generated/ChatClientSDK

ROOT="$(cd "$(dirname "$0")"/../.. && pwd)"
SRC_DEFAULT="$ROOT/Backend/Chat.Client"
DST_DEFAULT="$ROOT/Assets/Generated/ChatClientSDK"

SRC="$SRC_DEFAULT"
DST="$DST_DEFAULT"
DRY_RUN=false
NO_CLEAN=false
VERBOSE=false
STRICT=false

warn() { echo "[warn] $*" >&2; }
info() { echo "[info] $*"; }
ok()   { echo "[ok] $*"; }
die()  { echo "[err] $*" >&2; exit 1; }

usage() {
  cat <<EOF
Sync Unity Client SDK (idempotent & safe)

Options:
  --src PATH      Source project folder (default: $SRC_DEFAULT)
  --dst PATH      Destination folder in Unity (default: $DST_DEFAULT)
  --dry-run       Show what would change (rsync --dry-run)
  --no-clean      Do not delete destination before sync
  --verbose       Extra logging
  --strict        Fail if forbidden artifacts are detected after sync
  -h, --help      Show this help
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --src)     SRC="$2"; shift 2 ;;
    --dst)     DST="$2"; shift 2 ;;
    --dry-run) DRY_RUN=true; shift ;;
    --no-clean) NO_CLEAN=true; shift ;;
    --verbose) VERBOSE=true; shift ;;
    --strict)  STRICT=true; shift ;;
    -h|--help) usage; exit 0 ;;
    *) die "Unknown arg: $1" ;;
  esac
done

[[ -d "$SRC" ]] || die "Source not found: $SRC"

rsync_flags=(-a --delete -m)   # -m = prune empty dirs
$DRY_RUN && rsync_flags+=(--dry-run)
$VERBOSE && rsync_flags+=(-v)

# Allowlist: only .cs (runtime), plus README.md. Everything else excluded.
include_exclude=(
  --include='*/'
  --include='README.md'
  --include='*.cs'
  --exclude='*.g.cs'
  --exclude='*.Designer.cs'
  --exclude='bin/'
  --exclude='obj/'
  --exclude='.vs/'
  --exclude='.idea/'
  --exclude='.vscode/'
  --exclude='*.csproj'
  --exclude='*.sln'
  --exclude='*.user'
  --exclude='*.suo'
  --exclude='*.dll'
  --exclude='*.pdb'
  --exclude='*.nupkg'
  --exclude='*.props'
  --exclude='*.targets'
  --exclude='*.json'
  --exclude='*.editorconfig'
  --exclude='**/*.meta'
)

echo "=== Sync Unity Client SDK ==="
echo "[cfg] SRC: $SRC"
echo "[cfg] DST: $DST"
$DRY_RUN && echo "[cfg] DRY-RUN: true"
$NO_CLEAN && echo "[cfg] NO-CLEAN: true"
$STRICT && echo "[cfg] STRICT: true"

if ! $NO_CLEAN; then
  rm -rf "$DST"
fi
mkdir -p "$DST"

# Perform sync
rsync "${rsync_flags[@]}" "${include_exclude[@]}" "$SRC/" "$DST/"

# Post-sync validation
forbidden=()
while IFS= read -r -d '' f; do forbidden+=("$f"); done < <(find "$DST" -type d \( -name bin -o -name obj \) -print0)
while IFS= read -r -d '' f; do forbidden+=("$f"); done < <(find "$DST" -type f \( -name '*.dll' -o -name '*.pdb' -o -name '*.csproj' -o -name '*.sln' -o -name '*.g.cs' \) -print0)

if [[ ${#forbidden[@]} -gt 0 ]]; then
  echo "[warn] Forbidden artifacts detected in $DST (will remove):"
  for f in "${forbidden[@]}"; do echo "  - $f"; done
  # Clean proactively to keep Unity healthy
  for f in "${forbidden[@]}"; do
    [[ -d "$f" ]] && rm -rf "$f" || rm -f "$f"
  done

  if $STRICT; then
    die "Strict mode: aborted after cleaning forbidden artifacts."
  else
    warn "Cleaned forbidden artifacts; continuing (non-strict)."
  fi
fi

# Stamp/readme to make intent clear in the repo
README="$DST/README.md"
if [[ ! -f "$README" ]]; then
  cat > "$README" <<'MD'
# ChatClientSDK (Generated)

This folder is **generated** by `Backend/scripts/sync-unity-client-sdk.sh`.

- Do **not** edit files here by hand; changes will be overwritten.
- Only plain C# runtime sources are copied (no bin/obj/DLLs/CSProj/SLN/Roslyn .g.cs).
- If you need to regenerate, run the sync script again.

MD
fi

ok "Synced source files to $DST"