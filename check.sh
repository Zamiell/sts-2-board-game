#!/usr/bin/env bash
# Builds the mod and runs both checks. Pass the game's data directory to override the default.
set -euo pipefail
cd "$(dirname "$0")"
data="${1:-/mnt/c/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64}"
dotnet build -c Release -nologo -v q
DOTNET_ROLL_FORWARD=Major dotnet run --project tests/Behavior
dotnet build tests/Compatibility -c Release -nologo -v q
"${DOTNET9:-dotnet}" tests/Compatibility/bin/Release/net9.0/Compatibility.dll "$data" dist/BoardGame/BoardGame.dll
