#!/bin/bash
# One session log in, one block of comparable numbers out.
#
# Written on 08.09.2026 because two sessions were being compared by eye, with a different grep each time, and
# a comparison whose two halves were measured differently is not a comparison. Everything here is read out of
# the shard's own lines - nothing is recomputed from events - except the counting, which is the smallest
# possible re-derivation.
#
#   bash measure-session.sh logs/session-2026-09-08_08-23.log
#
# With no argument it takes the newest session log.

LOG="${1:-$(ls -t O:/ModernUOfork/ModernUO-fork/logs/session-*.log | head -1)}"

echo "== $LOG =="
first=$(grep -ao "^\[[0-9:]*" "$LOG" | head -1 | tr -d '[')
last=$(grep -ao "^\[[0-9:]*" "$LOG" | tail -1 | tr -d '[')
echo "covers $first .. $last"
echo

echo "-- work --"
grep -ao "[0-9]* taken on, [0-9]* finished, [0-9]* failed, [0-9]* dropped[^;]*" "$LOG" | tail -1
grep -ao "[0-9]* endings were not allowed to price the ground[^<]*" "$LOG" | tail -1
echo

echo "-- roads --"
echo "no way through, all told: $(grep -ac 'no way through to' "$LOG")"
echo "by trade:"
grep -a "no way through to" "$LOG" | sed -E 's/.*failed at ([a-z-]+):.*/  \1/' | sort | uniq -c | sort -rn | head -8
echo "hottest destinations:"
grep -ao "no way through to ([0-9]*, [0-9]*, [-0-9]*)" "$LOG" | sort | uniq -c | sort -rn | head -6 | sed 's/^/  /'
echo

echo "-- searching --"
grep -ao "Getting about: [^<]*" "$LOG" | tail -1 | cut -c1-260
echo

echo "-- ground --"
grep -ao "[0-9]* sweeps: [^<]*" "$LOG" | tail -1 | cut -c1-320
grep -ao "The fires now known, by address[^<]*" "$LOG" | tail -1
echo

echo "-- market --"
grep -ao "The market: [^<]*" "$LOG" | tail -1 | cut -c1-260
echo

echo "-- money --"
grep -ao "Money: [^<]*" "$LOG" | tail -1 | cut -c1-260
echo

echo "-- standing still --"
grep -ao "Standing still: [^<]*" "$LOG" | tail -1 | cut -c1-320
echo

echo "-- errors --"
echo "errors, all told: $(grep -ac ' ERR]' "$LOG")"
grep -a ' ERR]' "$LOG" | sed -E 's/.*ERR\] [A-Za-z]+ (the [A-Za-z]+ )?//; s/\(.*//; s/[0-9]+//g' | sort | uniq -c | sort -rn | head -6 | cut -c1-150
