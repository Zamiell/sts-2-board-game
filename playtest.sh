#!/usr/bin/env bash
# Plays one board game run automatically in the real game (WSL only), using STS2's AutoSlay.
#
#   ./playtest.sh [CHARACTER] [SEED]     e.g. ./playtest.sh BG_IRONCLAD TEST1
#
# The game must be closed and Steam running. Your saves are backed up first and restored afterwards, because AutoSlay
# abandons any run in progress and records its own run. Output goes to playtest-output/: autoslay.log, the game's
# godot.log, and a screenshot every few seconds.
set -euo pipefail
cd "$(dirname "$0")"

character="${1:-}"
seed="${2:-PLAYTEST$RANDOM}"
game_dir="/mnt/c/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2"
data_dir="$(wslpath "$(cmd.exe /c 'echo %APPDATA%' 2>/dev/null | tr -d '\r')")/SlayTheSpire2"
timeout_seconds="${PLAYTEST_TIMEOUT:-1800}"

if tasklist.exe 2>/dev/null | grep -qi SlayTheSpire2.exe; then
    echo "Close Slay the Spire 2 first." >&2
    exit 1
fi

./install.sh > /dev/null

# Windows paths for the game's output, in a Windows temp folder so the game can write there.
win_temp="$(cmd.exe /c 'echo %TEMP%' 2>/dev/null | tr -d '\r')"
out_win="$win_temp\\bg-playtest"
out="$(wslpath "$out_win")"
rm -rf "$out"
mkdir -p "$out/screenshots"

backup="$(mktemp -d)"
cp -a "$data_dir/steam" "$backup/steam"
restore() {
    rm -rf "$data_dir/steam"
    cp -a "$backup/steam" "$data_dir/steam"
    rm -rf "$backup"
}
trap restore EXIT

args=(--bg-playtest --seed "$seed" --log-file "$out_win\\autoslay.log" --bg-screenshots "$out_win\\screenshots")
if [[ -n "$character" ]]; then
    args+=(--bg-character "$character")
fi

echo "Playing seed $seed${character:+ as $character} (timeout ${timeout_seconds}s)..."
status=0
# Launched outside Steam, the game needs its app ID passed in (WSLENV passes the variable to Windows).
export SteamAppId=2868840 SteamGameId=2868840
export WSLENV="${WSLENV:+$WSLENV:}SteamAppId:SteamGameId"
(cd "$game_dir" && timeout "$timeout_seconds" ./SlayTheSpire2.exe "${args[@]}" > "$out/stdout.log" 2>&1) || status=$?
if [[ $status -eq 124 ]]; then
    taskkill.exe /IM SlayTheSpire2.exe /F > /dev/null 2>&1 || true
    sleep 2
fi

rm -rf playtest-output
mkdir -p playtest-output
cp -a "$out/." playtest-output/
cp "$data_dir/logs/godot.log" playtest-output/godot.log 2>/dev/null || true

echo "Exit status $status. Output in playtest-output/."
grep -iE "Run (completed|failed)|\[Error\]|Exception" playtest-output/autoslay.log 2>/dev/null | head -20 || true
exit "$status"
