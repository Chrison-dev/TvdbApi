#!/usr/bin/env bash
set -eo pipefail
dotnet run --project "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/build/_build.csproj" -- "$@"
