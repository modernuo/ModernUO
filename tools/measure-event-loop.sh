#!/usr/bin/env bash
# A/B measurement for the event loop scheduler, for macOS and Linux.
#
# Boots the shard twice against identical binaries -- once with idle sleeping disabled
# (server.eventLoopIdleWaitMs = 0) and once with it enabled (= 2) -- and samples process CPU time
# over a fixed window. Everything else is held constant, so the delta is the scheduler.
#
# The Windows equivalent is tools/Measure-EventLoop.ps1.
#
# Usage:  ./tools/measure-event-loop.sh [warmup_seconds] [sample_seconds]

set -euo pipefail

WARMUP="${1:-45}"
SAMPLE="${2:-60}"

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DIST="$ROOT/Distribution"
CONFIG="$DIST/Configuration/modernuo.json"

# The published binary has no extension on these platforms.
EXE="$DIST/ModernUO"

if [ ! -x "$EXE" ]; then
    echo "ModernUO not found at $EXE. Build with: dotnet build -c Release" >&2
    exit 1
fi

if [ ! -f "$CONFIG" ]; then
    echo "No configuration at $CONFIG. Start the shard once to generate it." >&2
    exit 1
fi

mkdir -p "$DIST/Logs"

# python3 rather than sed: the value must be replaced inside JSON, and the file is the live
# server configuration. macOS ships python3 with the developer tools.
set_idle_wait() {
    python3 - "$CONFIG" "$1" <<'PY'
import json, sys
path, value = sys.argv[1], sys.argv[2]
with open(path) as f:
    cfg = json.load(f)
cfg.setdefault("settings", {})["server.eventLoopIdleWaitMs"] = value
with open(path, "w") as f:
    json.dump(cfg, f, indent=2)
PY
}

# Process CPU time in seconds. ps reports [[dd-]hh:]mm:ss, which needs unpacking.
cpu_seconds() {
    local t
    t="$(ps -o time= -p "$1" | tr -d ' ')"
    python3 - "$t" <<'PY'
import sys
raw = sys.argv[1]
days, _, rest = raw.rpartition('-')
parts = [float(p) for p in rest.split(':')]
total = 0.0
for p in parts:
    total = total * 60 + p
if days:
    total += float(days) * 86400
print(total)
PY
}

measure() {
    local idle="$1" label="$2"

    set_idle_wait "$idle"

    echo
    echo "=== $label (server.eventLoopIdleWaitMs = $idle) ==="

    # Redirect stdin from /dev/null so the server runs headless and never blocks on console input.
    "$EXE" < /dev/null > "$DIST/Logs/measure-$idle.out" 2> "$DIST/Logs/measure-$idle.err" &
    local pid=$!

    # shellcheck disable=SC2064
    trap "kill $pid 2>/dev/null || true" EXIT

    echo "  pid $pid; warming up for ${WARMUP}s..."
    sleep "$WARMUP"

    if ! kill -0 "$pid" 2>/dev/null; then
        echo "  server exited during warmup. See Logs/measure-$idle.err" >&2
        tail -20 "$DIST/Logs/measure-$idle.err" >&2 || true
        exit 1
    fi

    local before after wall_before wall_after
    before="$(cpu_seconds "$pid")"
    wall_before="$(date +%s)"

    echo "  sampling for ${SAMPLE}s..."
    sleep "$SAMPLE"

    after="$(cpu_seconds "$pid")"
    wall_after="$(date +%s)"

    kill "$pid" 2>/dev/null || true
    wait "$pid" 2>/dev/null || true
    trap - EXIT

    python3 - "$before" "$after" "$wall_before" "$wall_after" <<'PY'
import sys
cpu = float(sys.argv[2]) - float(sys.argv[1])
wall = float(sys.argv[4]) - float(sys.argv[3])
pct = cpu / wall * 100 if wall > 0 else 0
print(f"  CPU: {pct:.2f}% of one core  ({cpu:.1f}s CPU over {wall:.0f}s wall)")
PY
}

measure 0 "Never sleep (max responsiveness)"
measure 2 "Idle sleeping"

echo
echo "=== Result ==="
echo "  Compare the two CPU figures above."
echo "  Then read the loop: lines for what it cost in timer accuracy:"
echo
grep -h "loop: " "$DIST/Logs/measure-0.out" | tail -3 || true
echo "  ---"
grep -h "loop: " "$DIST/Logs/measure-2.out" | tail -3 || true
echo
echo "  missed16ms and sched are the health numbers. cpu alone does not tell you"
echo "  whether the trade was worth it."

# Leave the configuration on the default.
set_idle_wait 2
