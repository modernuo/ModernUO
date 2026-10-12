#!/bin/bash
# The lines a restart has to print before anything measured after it means anything.
#
# Written on 08.09.2026, after a batch of eleven changes went in at once. Every one of them either files
# something, refuses something, or moves a place, and each has a boot line or a first-sweep line that says
# which. Reading them by eye four times in a morning is how a change gets believed without being checked.

LOG="${1:-$(ls -t O:/ModernUOfork/ModernUO-fork/logs/session-*.log | head -1)}"

echo "== $LOG =="
echo

say() {
  local what="$1" pat="$2"
  local hit
  hit=$(grep -ao "$pat" "$LOG" | tail -1)
  if [ -n "$hit" ]; then
    echo "  OK   $what"
    echo "       $hit" | cut -c1-200
  else
    echo "  MISS $what  (pattern: $pat)"
  fi
}

say "shard listening"          "Listening: 127.0.0.1:2593"
say "refused-ground record"    "Refused ground: one entry per[^<]*"
say "training field"           "training field at ([0-9, ]*)[^<]*"
say "population raised"        "Population raised: [^<]*"
say "first sweep"              "Swept [0-9]* tiles around[^<]*"
say "fires by address"         "The fires now known, by address[^<]*"
say "smith's metal"            "the board's metal reads[^<]*"
echo
echo "-- counts that decide whether the footing gate was too tight --"
grep -ao "[0-9]* sweeps: [0-9]* seams, [0-9]* fires, [0-9]* hearths, [0-9]* counters" "$LOG" | tail -1
grep -ao "[0-9]* workshops never filed for having no floor at them[^,]*, [0-9]* passed over for standing on another floor[^,]*" "$LOG" | tail -1
echo
echo "-- errors in the first minutes --"
grep -ac ' ERR]' "$LOG"
