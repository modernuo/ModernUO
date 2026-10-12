"""One measured window of a running shard, for the night's 45-minute tick.

The summary lines are running totals since the shard started, so a window is the difference between the newest
summary and the one nearest to <minutes> before it. Nothing is recomputed from events except error counts.

    python night-window.py                 newest session log, 45 minutes
    python night-window.py 30              the same, 30 minutes
    python night-window.py 45 <log>        a given log
"""

import glob
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.abspath(__file__))
sys.stdout.reconfigure(encoding="utf-8")

minutes = int(sys.argv[1]) if len(sys.argv) > 1 else 45
log = sys.argv[2] if len(sys.argv) > 2 else max(glob.glob(os.path.join(ROOT, "logs", "session-*.log")), key=os.path.getmtime)

STAMP = re.compile(r"^\[(\d\d):(\d\d):(\d\d) ")
WORK = re.compile(r"(\d+) taken on, (\d+) finished, (\d+) failed, (\d+) dropped")
DIED = re.compile(r"(\d+) died doing it")
MARKET = re.compile(r"(\d+) sales and (\d+) fills for (\d+)gp")


def seconds(line):
    m = STAMP.match(line)
    return None if not m else int(m.group(1)) * 3600 + int(m.group(2)) * 60 + int(m.group(3))


work = []      # (t, taken, finished, failed, dropped, died)
market = []    # (t, sales, fills, gp)
rest = None
errors = []    # (t, text)
last_t = 0
day = 0
prev = None

with open(log, encoding="utf-8", errors="replace") as f:
    for line in f:
        t = seconds(line)
        if t is None:
            continue
        if prev is not None and t < prev - 3600:
            day += 86400
        prev = t
        t += day
        last_t = t
        if " ERR] " in line:
            errors.append((t, line))
        m = WORK.search(line)
        if m and ("Will" in line or "Resolve" in line or "taken on" in line):
            d = DIED.search(line)
            work.append((t, *map(int, m.groups()), int(d.group(1)) if d else 0))
        m = MARKET.search(line)
        if m and "The market:" in line:
            market.append((t, *map(int, m.groups())))
        if "INF] Rest: " in line:
            rest = line


def window(series):
    if not series:
        return None, None
    end = series[-1]
    start = None
    for row in series:
        if row[0] <= end[0] - minutes * 60:
            start = row
    return start, end


def hhmm(t):
    t %= 86400
    return f"{t // 3600:02d}:{t % 3600 // 60:02d}"


print(f"== {os.path.basename(log)}: window of {minutes} min ending {hhmm(last_t)} ==")

start, end = window(work)
if end:
    if start is None:
        start = (end[0] - end[0], 0, 0, 0, 0, 0)
        note = " (since the start of the session)"
    else:
        note = f" ({hhmm(start[0])}–{hhmm(end[0])})"
    taken, fin, fail, drop, died = (end[i] - start[i] for i in range(1, 6))
    ended = fin + fail + drop
    share = 100.0 * fin / ended if ended else 0.0
    print(f"work{note}: {taken} taken on, {fin} finished, {fail} failed, {drop} dropped -> {share:.1f}% finished; {died} died doing it")
    total = end[2] + end[3] + end[4]
    print(f"session so far: {end[1]} taken on, {end[2]} finished ({100.0 * end[2] / total if total else 0:.1f}%)")
else:
    print("work: no summary line yet")

start, end = window(market)
if end:
    base = start or (0, 0, 0, 0)
    print(f"market: {end[1] - base[1]} sales and {end[2] - base[2]} fills for {end[3] - base[3]}gp in the window")

if rest:
    print("rest:", rest.split("INF] Rest: ", 1)[1].split(" <s:")[0][:600])

cut = last_t - minutes * 60

# The window's failures and drops, by kind and by reason with the numbers taken out.
FAILED = re.compile(r"\] (.+?) failed at ([a-z-]+): (.*?) — (.*?)(?: <s:|$)")
DROPPED = re.compile(r"\] (.+?) dropped ([a-z-]+): (.*?) — (.*?)(?: <s:|$)")
fails, drops = {}, {}
by_kind_fail, by_kind_drop = {}, {}
who_fails = {}
with open(log, encoding="utf-8", errors="replace") as f:
    day = 0
    prev = None
    for line in f:
        t = seconds(line)
        if t is None:
            continue
        if prev is not None and t < prev - 3600:
            day += 86400
        prev = t
        if t + day < cut:
            continue
        for rx, bucket, kinds in ((FAILED, fails, by_kind_fail), (DROPPED, drops, by_kind_drop)):
            m = rx.search(line)
            if m:
                name, kind, _, why = m.groups()
                key = kind + ": " + re.sub(r"\([^)]*\)", "()", re.sub(r"-?\d+(\.\d+)?", "#", why))[:110]
                bucket[key] = bucket.get(key, 0) + 1
                kinds[kind] = kinds.get(kind, 0) + 1
                if rx is FAILED:
                    who_fails[name] = who_fails.get(name, 0) + 1
print("failed by kind:", ", ".join(f"{k} {n}" for k, n in sorted(by_kind_fail.items(), key=lambda kv: -kv[1])[:12]))
for key, n in sorted(fails.items(), key=lambda kv: -kv[1])[:12]:
    print(f"  {n:4d}  {key}")
print("dropped by kind:", ", ".join(f"{k} {n}" for k, n in sorted(by_kind_drop.items(), key=lambda kv: -kv[1])[:12]))
for key, n in sorted(drops.items(), key=lambda kv: -kv[1])[:8]:
    print(f"  {n:4d}  {key}")
print("most failures by bot:", ", ".join(f"{k} {n}" for k, n in sorted(who_fails.items(), key=lambda kv: -kv[1])[:8]))

recent = [text for t, text in errors if t >= cut]
print(f"errors in the window: {len(recent)}")
kinds = {}
for text in recent:
    body = text.split("ERR] ", 1)[1]
    key = re.sub(r"\d+", "#", body)[:90]
    kinds[key] = kinds.get(key, 0) + 1
for key, n in sorted(kinds.items(), key=lambda kv: -kv[1])[:6]:
    print(f"  {n:4d}  {key}")

alerts = os.path.join(ROOT, "logs", "alerts.ndjson")
if os.path.exists(alerts):
    import datetime

    since = datetime.datetime.now() - datetime.timedelta(minutes=minutes)
    raised = {}
    with open(alerts, encoding="utf-8", errors="replace") as f:
        for line in f.readlines()[-2000:]:
            try:
                a = json.loads(line)
                at = datetime.datetime.fromisoformat(a.get("at", ""))
            except Exception:
                continue
            if at >= since and a.get("state") in ("raised", "clear"):
                raised.setdefault((a.get("kind"), a.get("state")), []).append(a)
    print(f"alarms in the window: {sum(len(v) for v in raised.values())}")
    for (kind, state), rows in sorted(raised.items(), key=lambda kv: str(kv[0])):
        print(f"   {state:6s} {kind:14s} x{len(rows)}  {str(rows[-1].get('say', ''))[:150]}")
