:; set -eo pipefail
:; SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && pwd)
:; exec "$SCRIPT_DIR/build.sh" "$@"

@echo off
powershell -ExecutionPolicy ByPass -NoProfile -File "%~dp0build.ps1" %*
