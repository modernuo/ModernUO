#!/usr/bin/env python3
"""One row of the shard's key numbers, read out of the newest session log's own summary lines.

Written 12.10.2026 to replace the kpi.py that lived in a session scratchpad and was lost with it. Nothing here
is recomputed from events except the counting of deaths in a window and the division that makes a share; every
other number is the shard's own.

    python kpi.py                 the newest log, a row appended to O:/ModernUOfork/work/kpi.tsv and printed
    python kpi.py <log>           a particular log
    WINDOW=35 python kpi.py       deaths counted over the last N minutes of the log (default 60)
"""
import glob
import os
import re
import sys
from datetime import datetime, timedelta

LOGS = "O:/ModernUOfork/ModernUO-fork/logs"
OUT = os.environ.get("KPI_OUT", "O:/ModernUOfork/work/kpi.tsv")
WINDOW = int(os.environ.get("WINDOW", "60"))

log = sys.argv[1] if len(sys.argv) > 1 else max(glob.glob(f"{LOGS}/session-*.log"), key=os.path.getmtime)

last = {}
deaths = []
errors = 0
wanted = ("Resolve:", "Lines:", "Getting about:", "Money:", "The clock:", "Rest:", "The island:", "Packs:")
with open(log, "r", encoding="utf-8", errors="replace") as f:
    for line in f:
        if " ERR]" in line:
            errors += 1
        if "] Kept: " in line and " fell at " in line:
            deaths.append(line[1:9])
            continue
        for key in wanted:
            # "[17:44:15 INF] Resolve: ..." — the stamp and level are fourteen characters.
            if line.startswith("[") and line[14:].startswith(" " + key):
                last[key] = line[1:9], line.split("] ", 1)[1].split(" <s:")[0]
                break


def num(pattern, text, default=0):
    m = re.search(pattern, text)
    return int(m.group(1).replace(" ", "")) if m else default


at, resolve = last.get("Resolve:", ("--:--:--", ""))
endings = num(r" of (\d+) endings", resolve)
finished = num(r"Resolve: (\d+) of", resolve)
work = 100.0 * finished / endings if endings else 0
failed = num(r"(\d+) failed", resolve)
dropped = num(r"(\d+) dropped", resolve)
died = num(r"(\d+) died", resolve)
put_down = num(r"(\d+) put down \(", resolve)
not_resumed = num(r"taken up again, (\d+) not", resolve)

_, lines = last.get("Lines:", ("", ""))
first = lines.split("in the first 60 min:", 1)[1] if "in the first 60 min:" in lines else ""
met = num(r"(\d+) met", first)
late = num(r"(\d+) met after their hour", first)
nfail = num(r"\), (\d+) failed", first)
needs = 100.0 * met / (met + late + nfail) if met else 0

_, about = last.get("Getting about:", ("", ""))
searches = num(r"(\d+) searches", about)
reached = num(r"(\d+) reached", about)
partial = num(r"(\d+) partial", about)
paths = 100.0 * reached / searches if searches else 0

_, money = last.get("Money:", ("", ""))
median = num(r"middling (\d+)gp", money)
poorest = num(r"poorest (\d+)gp", money)
fattest = num(r"fattest (\d+)gp", money)
total_gp = num(r"(\d+)gp between them", money)

_, clock = last.get("The clock:", ("", ""))
ms = re.search(r"\(([\d.]+)ms a look, worst ([\d.]+)ms\)", clock)
loop, worst = (float(ms.group(1)), float(ms.group(2))) if ms else (0, 0)

_, rest = last.get("Rest:", ("", ""))
in_world = num(r"Rest: (\d+) in the world", rest)
resting = num(r"(\d+) resting", rest)

_, island = last.get("The island:", ("", ""))
poor = re.search(r"(\d+) of (\d+) hunts begun .* under 100gp", island)
poor_share = 100.0 * int(poor.group(1)) / int(poor.group(2)) if poor and int(poor.group(2)) else 0

_, packs = last.get("Packs:", ("", ""))
over = num(r"over the allowance in a capped kind: (\d+) bots", packs)

# Deaths in the last WINDOW minutes of the log, by the log's own clock (it is written in buffers).
if deaths:
    end = datetime.strptime(deaths[-1], "%H:%M:%S")
    if at != "--:--:--":
        end = max(end, datetime.strptime(at, "%H:%M:%S"))
    start = end - timedelta(minutes=WINDOW)
    recent = sum(1 for d in deaths if start <= datetime.strptime(d, "%H:%M:%S") <= end)
else:
    recent = 0
per_bot_h = recent / (in_world * WINDOW / 60.0) if in_world else 0

row = [
    datetime.now().strftime("%m-%d %H:%M"), at, os.path.basename(log)[8:24], str(in_world + resting), str(in_world),
    f"{work:.1f}", str(failed), str(dropped), str(died), f"{needs:.1f}", f"{paths:.1f}", str(recent),
    f"{per_bot_h:.2f}", str(median), str(poorest), str(fattest // 1000) + "k", str(total_gp // 1000) + "k",
    f"{poor_share:.1f}", f"{loop:.1f}", f"{worst:.0f}", str(errors), f"{not_resumed}/{put_down}", str(over),
]
head = [
    "now", "at", "log", "pop", "in_world", "work%", "failed", "dropped", "died", "needs%", "paths%",
    f"deaths{WINDOW}m", "d/bot-h", "median", "poorest", "fattest", "total", "poorhunt%", "loop_ms", "worst_ms",
    "err", "notresumed/putdown", "overcap",
]

os.makedirs(os.path.dirname(OUT), exist_ok=True)
new = not os.path.exists(OUT)
with open(OUT, "a", encoding="utf-8") as out:
    if new:
        out.write("\t".join(head) + "\n")
    out.write("\t".join(row) + "\n")
print("\t".join(head))
print("\t".join(row))
