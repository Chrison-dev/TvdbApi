#!/usr/bin/env bash
# Reproducible codegen for the TheTVDB v4 client.
#
#   1. download the live TheTVDB v4 OpenAPI spec   -> openapi/tvdb-v4.raw.yml
#   2. apply the checked-in overlay (patch_spec.py) -> openapi/tvdb-v4.patched.yml
#   3. run NSwag (pinned local tool) against the patched spec
#
# The raw + patched specs are committed so a regen diff is reviewable.
# Run from anywhere:  tools/openapi/generate.sh
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO="$(cd "$HERE/../.." && pwd)"
SPEC_URL="${TVDB_SPEC_URL:-https://thetvdb.github.io/v4-api/swagger.yml}"
OPENAPI_DIR="$REPO/src/TvdbClient/openapi"
RAW="$OPENAPI_DIR/tvdb-v4.raw.yml"
PATCHED="$OPENAPI_DIR/tvdb-v4.patched.yml"

mkdir -p "$OPENAPI_DIR"
echo "→ downloading live spec: $SPEC_URL"
curl -fsSL "$SPEC_URL" -o "$RAW"
echo "→ applying overlay"
python3 "$HERE/patch_spec.py" "$RAW" "$PATCHED"
echo "→ restoring pinned tools"
( cd "$REPO" && dotnet tool restore >/dev/null )
echo "→ running NSwag"
# NSwag.ConsoleCore targets net9.0; roll forward onto a newer runtime (e.g. net10)
# when the 9.0 runtime isn't installed. Also pinned via .config/dotnet-tools.json.
( cd "$REPO/src/TvdbClient" && DOTNET_ROLL_FORWARD="${DOTNET_ROLL_FORWARD:-Major}" dotnet nswag run TvdbClient.nswag )
echo "✓ codegen complete"
