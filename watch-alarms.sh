#!/bin/bash
# The half-hour watch, built on the alarm channel rather than on a re-derivation of the log.
#
# One batch every WATCH_EVERY seconds, printed at once so it arrives as a single notification. Everything in
# it is either an event the shard raised itself or a line the shard wrote itself — nothing here recomputes an
# aggregate the shard already publishes, because a re-computation is one more instrument to be wrong. The one
# exception is counting, which is the smallest possible re-derivation.
#
# Marks are kept in logs/.alarm-mark (line number in alerts.ndjson) and logs/.session-mark (line number in the
# newest session log), so each batch covers only what is new since the last one. A restarted shard writes a
# new session log; the mark resets when the file gets shorter than the mark.

ROOT="O:/ModernUOfork/ModernUO-fork"
LOGS="$ROOT/logs"
ALERTS="$LOGS/alerts.ndjson"
# Marks are named after the watch, so a test run does not eat the live one's events. Found the hard way:
# every `WATCH_EVERY=1 bash watch-alarms.sh` used for checking a change moved the same two files the real
# watch reads from, and the 23:04 error alarm was consumed by a three-second test rather than reported.
MARK="${WATCH_MARK:-alarm}"
AMARK="$LOGS/.$MARK-mark"
SMARK="$LOGS/.$MARK-session-mark"
EVERY="${WATCH_EVERY:-1800}"

line() { sed 's/.*INF\] //;s/ <s:.*//'; }

# One JSON event to one readable line. No jq on this box, and a python start-up per event is too much, so
# this reads the four fields that matter with sed. Anything unparseable is printed raw rather than dropped.
readable() {
  sed -E 's/.*"at":"[0-9-]+T([0-9:]+)".*"state":"([a-z]+)","kind":"([^"]+)","n":([-0-9]+),"of":([-0-9]+),"window":"([^"]*)".*"say":"(.*)"\}/  \1 [\2] \3 \4\/\5 over \6 — \7/'
}

tick() {
  local now log total from
  now=$(date +%H:%M)

  if tasklist //FI "IMAGENAME eq ModernUO.exe" 2>/dev/null | grep -q ModernUO; then
    echo "== $now == shard up =="
  else
    echo "== $now == !! ModernUO.exe IS NOT RUNNING !! =="
  fi

  # 1. Everything the shard raised since the last batch.
  if [ -f "$ALERTS" ]; then
    total=$(wc -l < "$ALERTS")
    from=1
    [ -f "$AMARK" ] && from=$(($(cat "$AMARK") + 1))
    [ "$from" -gt "$((total + 1))" ] && from=1
    echo "$total" > "$AMARK"

    if [ "$from" -le "$total" ]; then
      # Events from before the last restart are marked as such. The channel is appended to across restarts,
      # so a window that contains one holds two different worlds: on 07.09.2026 a finding about a bot stuck
      # at 256 of 243 stones was read as current when the bot in question had been deleted with the previous
      # population and the one wearing that name was walking about with a third of a load. Anything above the
      # boundary describes a shard that no longer exists.
      local win boundary
      win="/tmp/alarm-win.$$"
      sed -n "${from},${total}p" "$ALERTS" > "$win"
      boundary=$(grep -n '"kind":"shard"' "$win" | tail -1 | cut -d: -f1)

      if [ -n "$boundary" ] && [ "$boundary" -gt 1 ]; then
        echo "  -- before the last restart, about a shard that is gone --"
        sed -n "1,$((boundary - 1))p" "$win" | grep -v '"state":"alive"' | readable
        echo "  -- since the restart --"
        sed -n "${boundary},\$p" "$win" | grep -v '"state":"alive"' | readable
      else
        grep -v '"state":"alive"' "$win" | readable
      fi

      grep '"state":"alive"' "$win" | tail -1 | readable | sed 's/^  /  last heartbeat: /'
      rm -f "$win"
      echo "  ($((total - from + 1)) events on the channel this window)"
    else
      # Silence needs its own context or it reads as an alarm. The age of the newest line in the whole file
      # answers the only question that matters here: nothing new because nothing happened, or nothing new
      # because nothing is being written. The heartbeat is hourly, so under an hour of quiet is ordinary.
      local last age
      # Age from the file's own timestamp rather than from parsing its last line: one less thing to get
      # wrong, and the file is written the moment an event is.
      last=$(date -r "$ALERTS" +%H:%M:%S 2>/dev/null)
      age=$(( ( $(date +%s) - $(stat -c %Y "$ALERTS") ) / 60 ))
      echo "  Nothing new on the channel this window. Newest line in the file is $last, about ${age}m ago."
      echo "  The heartbeat is hourly, so quiet under an hour is ordinary; quiet past that means the channel itself has stopped."
    fi
  else
    echo "  !! no alerts.ndjson — the alarm channel is not being written !!"
  fi

  # 2. Errors, counted from the log the shard writes. The channel carries a specimen; this is the count.
  log=$(ls -t "$LOGS"/session-*.log 2>/dev/null | head -1)

  if [ -n "$log" ]; then
    total=$(wc -l < "$log")
    from=1
    [ -f "$SMARK" ] && from=$(($(cat "$SMARK") + 1))
    [ "$from" -gt "$((total + 1))" ] && from=1
    echo "$total" > "$SMARK"

    echo "  log: $(basename "$log"), $((total - from + 1)) new lines, $(sed -n "${from},${total}p" "$log" | grep -c ' ERR]') errors"

    # 3. The shard's own last word on each thing it summarises. Its sentences, not mine.
    for what in 'Will:' 'The market:' 'Standing still:' 'Money:'; do
      grep "$what" "$log" | tail -1 | line | cut -c1-200 | sed 's/^/  /'
    done
  fi

  echo
}

# WATCH_ONCE=1 prints one batch and leaves. A watch that has to outlive half an hour cannot be a stream any
# more (the harness ends one at thirty minutes), so the long watch is an alarm clock that calls this.
if [ -n "$WATCH_ONCE" ]; then
  tick
  exit 0
fi

while true; do
  tick
  sleep "$EVERY"
done
