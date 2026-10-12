#!/bin/bash
# The one watch. Everything worth knowing about the shard, once every half hour, in one batch.
LOGDIR="O:/ModernUOfork/ModernUO-fork/logs"
STAMP="$LOGDIR/.watch-mark"

line() { sed 's/.*INF\] //;s/ <s:.*//'; }

tick() {
  local log now total from win
  log=$(ls -t "$LOGDIR"/session-*.log 2>/dev/null | head -1)
  [ -z "$log" ] && { echo "WATCH: no session log"; return; }
  now=$(date +%H:%M)

  if tasklist //FI "IMAGENAME eq ModernUO.exe" 2>/dev/null | grep -q ModernUO; then
    echo "== $now == $(basename "$log") == up =="
  else
    echo "== $now == $(basename "$log") == !! ModernUO.exe IS NOT RUNNING !! =="
  fi

  from=1
  [ -f "$STAMP" ] && from=$(cat "$STAMP")
  total=$(wc -l < "$log")
  [ "$from" -gt "$total" ] && from=1
  echo "$total" > "$STAMP"
  win="/tmp/watch-win.$$"
  sed -n "${from},${total}p" "$log" > "$win"

  # 1. stuck — the standing goal
  grep 'BotBeat>' "$log" | grep 'Standing still' | tail -1 | line | cut -c1-215 | sed 's/^/ STUCK  /'
  grep 'BotStall>' "$win" | sed 's/.*ERR\] //;s/ <s:.*//' | cut -c1-165 | tail -3 | sed 's/^/   !    /'

  # 2. fighting — outcome first, then why a blow did not land
  echo "  FIGHT  $(grep -c 'is dealing with' "$log") engaged, $(grep -c 'it is down' "$log") killed, $(grep -c 'able to strike' "$log") given up | $(grep -c 'would not go down' "$log") lone fights with no scratch"
  grep 'BotSquads>' "$log" | grep 'no line to the thing' | tail -1 | line | cut -c1-215 | sed 's/^/ BLOWS  /'
  grep 'BotSquads>' "$log" | grep 'squads standing' | tail -1 | line | cut -c1-165 | sed 's/^/ SQUADS /'
  grep 'BotSquads>' "$log" | grep '^.*Arms:' | tail -1 | line | tr ';' '
' | grep -i 'looked after themselves'     | sed 's/^ *//' | cut -c1-230 | sed 's/^/  MEND   /'

  # 3. the economy: every craft chain, link by link, plus what the market moved
  grep 'Needs:' "$log" | tail -1 | line | tr ';' '
' | sed 's/^ *//' | cut -c1-230 | sed 's/^/  NEEDS  /'
  grep 'The ground:' "$log" | tail -1 | line | tr ';' '
' | grep -iE 'cut wood|seams|lode'     | sed 's/^ *//' | cut -c1-230 | sed 's/^/  GROUND /'
  echo "  MADE   $(grep -o 'finished [a-z-]*:' "$log" | sort | uniq -c | sort -rn | grep -iE 'smith|sew|fletch|inscribe|mine|chop|forge|tailor|craft|brew|herbs|forage|glean' | tr -d '
' | tr -s ' ')"
  echo "  MARKET $(grep -c 'filled .* want' "$log") wants filled, $(grep -c 'put .* up to' "$log") reprices, $(grep -c 'has put .*gp down' "$log") wants raised | $(grep -c 'BotOrder>' "$log") orders"

  # The one number that says whether this is an economy or a faucet and a drain: what the population
  # earns from shopkeepers, what it hands back to them, and how much of it went to another bot instead.
  awk '
    BEGIN { inn = 0; out = 0; own = 0 }
    /sold [0-9]+ [A-Za-z]+ to [A-Za-z]+ for [0-9]+gp/   { for (i=1;i<=NF;i++) if ($i ~ /^[0-9]+gp$/) { g=$i; sub("gp","",g); inn+=g } }
    /bought [0-9]+ [A-Za-z]+ from [A-Za-z]+ for [0-9]+gp/ { for (i=1;i<=NF;i++) if ($i ~ /^[0-9]+gp$/) { g=$i; sub("gp","",g); out+=g } }
    /was paid [0-9]+gp/                                { for (i=1;i<=NF;i++) if ($i ~ /^[0-9]+gp$/) { g=$i; sub("gp","",g); own+=g } }
    END { print "  FLOW   " inn " gp in from counters, " out " gp out to counters, " own " gp of it filling wants" }
  ' "$log"
  grep -oE '[0-9]+ lessons paid for at [0-9]+gp against [0-9]+gp paid back out in wages' "$log" | tail -1 | sed 's/^/  SCHOOL /'
  grep -oE 'fattest [0-9]+gp held by [A-Za-z]+ the [A-Za-z]+' "$log" | tail -1 | sed 's/^/  PURSE  /'
  grep -oE 'came back from the woods with [0-9]+ herbs worth about [0-9]+gp' "$log" | tail -1 | sed 's/^/  HERBS  /'
  # Does the market clear. Stalls and wants standing side by side with no fills is the disease.
  # The market'''s own turnover, which is the honest measure of trade between bots: it counts a stall bought
  # from as well as a want filled, and BotAuction.Buy writes no line at all, so the log cannot see one.
  grep -oE '[0-9]+ sales and [0-9]+ fills for [0-9]+gp' "$log" | tail -1 | sed 's/^/  TRADE  /'
  grep -oE '[0-9]+ of [0-9]+ stalls holding.*' "$log" | tail -1 | tr ';' '
' | head -2 | sed 's/^ *//' | cut -c1-230 | sed 's/^/  STALLS /'
  grep 'BotAuction>' "$win" | line | cut -c1-165 | tail -3 | sed 's/^/  SOLD   /'
  grep -iE 'first bot on this shard ever' "$log" | line | cut -c1-165 | sed 's/^/  FIRST  /'

  # 4. this window
  echo "  SINCE  $((total-from)) lines | stalls $(grep -c 'BotStall>' "$win") | deaths $(grep -c ' died doing ' "$win") | ERR $(grep -c '\[..:..:.. ERR\]' "$win") | works $(grep -c 'finished [a-z-]*:' "$win")"
  grep -o 'failed at [a-z-]*:' "$win" | sort | uniq -c | sort -rn | head -5 | tr '\n' ' ' | sed 's/^/  FAILS  /'; echo
  grep '\[..:..:.. ERR\]' "$win" | sed 's/.*ERR\] //;s/ <s:.*//' | grep -v 'has not moved' \
    | sed 's/[0-9]\+/N/g' | sort | uniq -c | sort -rn | head -3 | cut -c1-150 | sed 's/^/  ERR    /'
  grep 'gave up .* set aside' "$win" | line | cut -c1-150 | tail -2 | sed 's/^/ PARKED /'

  # 5. the watcher itself
  if [ -f "$LOGDIR/bot-debugger-commands.log" ]; then
    echo "  ARGUS  $(grep -c 'the model:' "$LOGDIR/bot-debugger-commands.log") commands, $(grep -c 'REFUSED\|THREW' "$LOGDIR/bot-debugger-commands.log") refused"
  fi
  rm -f "$win"
  echo "== end =="
}

tick
while true; do
  sleep 1800
  tick
done
