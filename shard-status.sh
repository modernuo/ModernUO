#!/bin/bash
# One screen about the shard, for whoever is watching it while somebody else works on it.
#
# Written 13.09.2026. Everything here is read out of lines the shard wrote itself — the five-minute
# summary, the war ledger's own log lines, the alarm channel — and the only re-derivation is counting.
# Nothing is recomputed that the shard already publishes.
#
#   bash shard-status.sh            the newest session log
#   bash shard-status.sh <log>      a particular one
#   WINDOW=30 bash shard-status.sh  count deaths and kills over the last N minutes (default 60)

ROOT="${SHARD_ROOT:-$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)}"
LOGS="$ROOT/logs"
LOG="${1:-$(ls -t "$LOGS"/session-*.log 2>/dev/null | head -1)}"
WINDOW="${WINDOW:-60}"

line() { sed 's/.*\] //;s/ <s:.*//'; }

if tasklist //FI "IMAGENAME eq ModernUO.exe" 2>/dev/null | grep -q ModernUO; then
  up="up"
else
  up="!! NOT RUNNING !!"
fi

first=$(grep -ao "^\[[0-9:]*" "$LOG" | head -1 | tr -d '[')
last=$(grep -ao "^\[[0-9:]*" "$LOG" | tail -1 | tr -d '[')
echo "== $(date +%H:%M) == $(basename "$LOG") == $up == log covers $first .. $last, $(grep -ac ' ERR]' "$LOG") errors =="

# The last hour, by the clock in the log lines rather than by the wall: the log is written in buffers.
since=$(date -d "-$WINDOW min" +%H:%M:%S 2>/dev/null || date +%H:%M:%S)
recent="/tmp/shard-status.$$"
awk -v s="$since" 'substr($0,2,8) >= s' "$LOG" > "$recent"

echo
echo "-- wars --"
grep -a "declared war\|won its war\|ended by\|would declare war\|lost its war\|now stands at" "$LOG" | line | tail -6 | cut -c1-200 | sed 's/^/  /'
est=$(grep -a "Estate:" "$LOG" | tail -1)
wars=$(grep -a "^\[[0-9:]* INF\] Wars:" "$LOG" | tail -1)
[ -z "$wars" ] && wars="$est"
echo "$wars" | grep -ao "[0-9]* wars standing ([^;]*" | cut -c1-300 | sed 's/^/  now: /'
echo "$wars" | grep -ao "[0-9]* declared, [0-9]* refused for a truce[^;]*; [0-9]* won by blood[^;]*" | sed 's/^/  ledger: /'
echo "$est" | grep -ao "[0-9]* opinions between guilds, worst [^;]*" | cut -c1-120 | sed 's/^/  regard: /'

echo
echo "-- blood, last $WINDOW min --"
echo "  $(grep -ac 'was killed at' "$recent") bots killed, $(grep -ac 'of the enemy is down' "$recent") of them by another guild; $(grep -ac 'took on quarrel' "$recent") quarrels taken, $(grep -ac 'defending our ground' "$recent") in defence of home"
echo "$est" | grep -ao "[0-9]* times an enemy was seen on a guild's own ground[^;]*" | sed 's/^/  /'

echo
echo "-- halls and seats --"
echo "$est" | grep -ao "^.*halls stand: [^;]*" | line | cut -c1-200 | sed 's/^/  /'
echo "$wars" | grep -ao "seats: [^;]*" | cut -c1-300 | sed 's/^/  /'
echo "$est" | grep -ao "[0-9]* halls moved[^;]*" | sed 's/^/  /'
echo "$wars" | grep -ao "[0-9]* bots put down at their guild[^;]*" | sed 's/^/  /'
echo "$est" | grep -ao "[0-9]* squares are spoken for[^;]*" | cut -c1-160 | sed 's/^/  /'
grep -a "Ground a hall could stand on\|is back on its feet" "$recent" | line | awk '/Ground a hall/{print "  " substr($0,1,120)} /back on its feet/{n++; split($0,a,"at "); gsub(/[()]/,"",a[2]); split(a[2],b,","); k=int(b[1]/100)","int(b[2]/100); c[k]++} END{if(n){printf "  %d revivals, by hundred-tile square:", n; for(k in c) printf " %s=%d", k, c[k]; print ""}}'

echo
echo "-- work --"
grep -a "^\[[0-9:]* INF\] The clock:" "$LOG" | tail -1 | grep -ao "[0-9]* looks, [0-9]* turns handed out[^<]*" | cut -c1-230 | sed 's/^/  /'
grep -a "Will:" "$LOG" | tail -1 | grep -ao "[0-9]* taken on, [0-9]* finished, [0-9]* failed, [0-9]* dropped, [0-9]* died doing it" | sed 's/^/  /'
grep -a "Will:" "$LOG" | tail -1 | grep -ao "holding now: [^;]*" | cut -c1-200 | sed 's/^/  /'
# Commitment (14.09.2026): what share of endings were finishes, what took bots off the rest, and what holds did.
grep -a "INF\] Resolve:" "$LOG" | tail -1 | sed -E 's/.*Resolve: //; s/ <s:.*//' | cut -d';' -f1-5 | cut -c1-330 | sed 's/^/  /'
grep -a "Getting about:" "$LOG" | tail -1 | grep -ao "[0-9]* searches, [^;]*\|the whole ceiling was burned by: [^;]*\|[0-9]* searches at the stranded ceiling[^;]*" | cut -c1-200 | sed 's/^/  /'

echo
echo "-- alarms, newest --"
tail -4 "$LOGS/alerts.ndjson" 2>/dev/null | sed -E 's/.*"at":"[0-9-]+T([0-9:]+)".*"state":"([a-z]+)","kind":"([^"]+)".*"say":"(.*)"\}/  \1 [\2] \3 — \4/' | cut -c1-200

rm -f "$recent"
