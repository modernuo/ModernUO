#!/bin/bash
# The watcher's tick: one KPI row appended, the last few rows shown, and the shard's own alarms of the window.
#
#   bash tick.sh            a row over the last 60 minutes of the log
#   bash tick.sh 35         a row over the last 35 minutes
#
# Written 12.10.2026 next to kpi.py. If the shard is not running the first line says so, loudly.
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
W="${1:-60}"

if tasklist //FI "IMAGENAME eq ModernUO.exe" 2>/dev/null | grep -q ModernUO; then
  echo "== $(date +%H:%M) shard up =="
else
  echo "== $(date +%H:%M) !! SHARD NOT RUNNING !! =="
fi

WINDOW=$W python "$ROOT/kpi.py" > /dev/null
echo "last rows of O:/ModernUOfork/work/kpi.tsv (deaths over ${W}m):"
head -1 O:/ModernUOfork/work/kpi.tsv | cut -f1,2,4,5,6,10,11,13,14,18,19,21,22,23
tail -n +2 O:/ModernUOfork/work/kpi.tsv | tail -4 | cut -f1,2,4,5,6,10,11,13,14,18,19,21,22,23

echo "alarms raised in the last ${W} minutes:"
since=$(date -d "-$W min" +%Y-%m-%dT%H:%M 2>/dev/null)
grep -a '"raised"\|"kind": *"dying"\|"kind": *"errors"' "$ROOT/logs/alerts.ndjson" 2>/dev/null | awk -v s="$since" '$0 >= s' | tail -5 | cut -c1-220
