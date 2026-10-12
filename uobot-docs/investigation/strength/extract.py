#!/usr/bin/env python3
"""Extract the proving-ground measurements from ModernUO shard logs.

Usage:
    python extract.py <logs-folder> [--until "YYYY-MM-DD HH:MM:SS"] [--out <data-folder>] [--no-html]

The write-up in README.md was made with:
    python extract.py path/to/logs --until "2026-09-26 15:30:00"

<logs-folder> holds the shard's session logs (session-YYYY-MM-DD_HH-MM.log) and,
optionally, the door's command log (bot-debugger-commands.log). Everything in
data/ is regenerated from them. Unless --no-html is given, the JSON block inside
index.html (next to this script) is refreshed too, so the page shows the same data.

Standard library only (Python 3.8+).

What is read (all lines are "[HH:MM:SS LVL] message"; the date comes from the
file name, with a day added when the clock goes backwards inside one file):

  Proving: <bot> the <class> (<build>...) against the <creature> (...): ...   one single fight
  Proving: <guild>'s company (...) against the worst room of <dungeon> ...   one company fight
  Proving trace: <bot> +<n>s holding ...                                    one second of a traced bow fight
  Proving: held, N fights (...) ... by the formula xC ...                   the five-minute summary
  Proving: the ladder is read off the dungeons, ...                         the rungs read at boot
  Proving moved a delve offer: ...                                          a delve offer the readings changed
  Delving: ... judged by the proving ground: ...                            the delve's counters
  <bot> the <class> was killed at (x, y, z)                                 a bot's death
  A dial was moved by hand: BotWake.Running a -> b                          the wake switched by hand
  The wake is on|off ...                                                    the wake's state at boot
and, from bot-debugger-commands.log, the answers to the door verb "awake".
"""

import argparse
import csv
import glob
import json
import math
import os
import re
import statistics
import sys
from datetime import datetime, timedelta

# --------------------------------------------------------------------------------------------
# Which build each session ran. The logs do not print a build number; this table comes from
# the decision log (Projects/BotAIv2/DECISIONS.md, section 4.1, deploy times) checked against
# markers in the logs themselves. "basis" says how firm each row is.
# --------------------------------------------------------------------------------------------
SESSION_BUILDS = {
    "2026-09-26_01-08": ("233", "before the proving ground; no proving lines"),
    "2026-09-26_01-35": ("234", "only session with R 20.00 readings (R capped at 20)"),
    "2026-09-26_01-42": ("235", "DECISIONS 236: 'build 235's first second (01:42:39)'"),
    "2026-09-26_01-45": ("236", "DECISIONS 237: 'build 236's first second' offers 81-82k"),
    "2026-09-26_01-48": ("237", "deploy order between 236 and 238"),
    "2026-09-26_01-55": ("238", "first session whose fight lines say 'the double held'"),
    "2026-09-26_02-05": ("239", "first session with toll lines (build 239)"),
    "2026-09-26_02-10": ("240", "deploy order (inferred)"),
    "2026-09-26_02-17": ("241", "first hall-size and Shadow-dissolve lines (build 241)"),
    "2026-09-26_02-20": ("242", "deploy order (inferred)"),
    "2026-09-26_02-25": ("243", "deploy order (inferred)"),
    "2026-09-26_02-29": ("244", "deploy order (inferred)"),
    "2026-09-26_02-33": ("245", "first outpost lines (build 245)"),
    "2026-09-26_02-42": ("246", "first company (room) fights (build 246)"),
    "2026-09-26_03-26": ("247", "DECISIONS 247: deployed ~03:27"),
    "2026-09-26_03-41": ("248+249", "boot stopped at the store's y/n prompt; no fights"),
    "2026-09-26_03-43": ("248+249", "boot warns 'measured before build 249 against creatures asleep'"),
    "2026-09-26_03-45": ("250", "DECISIONS 250: deployed 03:45"),
    "2026-09-26_04-07": ("251", "first boot line 'The wake is ...' (BotWake, build 251)"),
    "2026-09-26_04-15": ("252", "first fight lines with 'the double took N steps'"),
    "2026-09-26_04-26": ("253", "deploy order; DECISIONS 253: ~04:27"),
    "2026-09-26_04-31": ("254", "first 'Proving trace:' lines (build 254)"),
    "2026-09-26_04-57": ("255", "DECISIONS 255: deployed ~04:58"),
    "2026-09-26_10-41": ("256", "DECISIONS 256: deployed ~10:42"),
    "2026-09-26_11-45": ("257", "DECISIONS 257: deployed ~11:50"),
    "2026-09-26_13-10": ("258", "deploy order between 257 and 259"),
    "2026-09-26_13-16": ("259", "first boot with 'The wake is on everywhere a bot goes'"),
    "2026-09-26_14-05": ("260", "first 'hostile strength' refusals (build 260)"),
    "2026-09-26_14-11": ("261", "deploy order between 260 and 262"),
    "2026-09-26_14-25": ("262", "first 'Chart:' line (build 262)"),
    "2026-09-26_14-32": ("263", "deploy order between 262 and 264"),
    "2026-09-26_15-18": ("264", "first 'Navigation:' line (build 264)"),
    "2026-09-26_15-30": ("265", "deploy order (inferred); after the write-up's cut-off"),
    "2026-09-26_15-37": ("266", "deploy order (inferred); after the write-up's cut-off"),
}

# The proving ground's creatures were asleep until build 249 woke its rings (session 03:43).
RINGS_AWAKE_FROM = "2026-09-26_03-43"
# Build 259 switched BotWake on for the whole world (session 13:16).
WORLD_AWAKE_FROM = "2026-09-26_13-16"
# Felucca's dungeons all lie east of x = 5120 (BotDungeon boxes: 5125..6135).
DUNGEON_SIDE_X = 5120
# The window the decision log compares the wake experiment with (asleep, same night).
BASELINE_FROM = ("2026-09-26", "01:08:00")
BASELINE_TO = ("2026-09-26", "04:00:00")

# The dungeons' boxes, copied from Projects/BotAIv2/BotDelve/BotDungeon.cs (x1, y1, x2, y2).
DUNGEON_BOXES = [
    ("the Orc Caves", 5280, 1295, 5365, 1385),
    ("Shame", 5380, 5, 5630, 240),
    ("Deceit", 5130, 520, 5350, 760),
    ("Despise", 5380, 515, 5615, 1005),
    ("Wrong", 5650, 520, 5865, 600),
    ("Covetous", 5380, 1790, 5625, 2045),
    ("Ice", 5660, 130, 5875, 370),
    ("Khaldun", 5385, 1285, 5615, 1495),
    ("Terathan Keep", 5125, 1545, 5370, 1765),
    ("Fire", 5630, 1285, 5875, 1480),
    ("Destard", 5130, 770, 5360, 1015),
    ("Hythloth", 5900, 15, 6135, 245),
]

RANGED_WEAPONS = ("Bow", "Crossbow", "HeavyCrossbow", "CompositeBow", "RepeatingCrossbow")

# --------------------------------------------------------------------------------------------
# Line shapes. Each mirrors the C# that writes it (see README, "From logs to data"): BotTrial.Say,
# BotRoomTrial.Say, BotTrial.Trace, BotProving.Describe, BotProving.Ladder, BotDelver, BotMobile, BotKept.
# --------------------------------------------------------------------------------------------
LINE = re.compile(r"^\[(\d\d):(\d\d):(\d\d) [A-Z]{3}\] (.*)$")
SESSION_NAME = re.compile(r"session-(\d{4}-\d\d-\d\d)_(\d\d)-(\d\d)\.log$")

FIGHT = re.compile(
    r"^Proving: (?P<bot>.+?) the (?P<cls>[A-Za-z]+) \("
    r"(?P<skills>\d+) in the fighting skills, (?P<stats>\d+) of stats, armour (?P<armour>-?\d+), "
    r"(?P<weapon>\S+) \((?P<damage>-?\d+)\)"
    r"(?:; the double held (?P<held>.+?), (?P<draws>\d+) weapon changes)?\) "
    r"against the (?P<kind>\S+) \((?P<might>\d+) of might\): "
    r"(?:won in (?P<won_s>\d+)s"
    r"|lost in (?P<lost_s>\d+)s with the \S+ at (?P<lost_left>\d+)\s?%"
    r"|(?P<called>called[^,]*?) after (?P<called_s>\d+)s with the \S+ at (?P<called_left>\d+)\s?%), "
    r"taking (?P<taken>\d+) and healing (?P<healed>\d+) of (?P<hits>\d+), (?P<dealt>\d+) dealt; "
    r"(?P<spells>\d+) spells, (?P<heals>\d+) healing spells, (?P<bandages>\d+) bandages, "
    r"(?P<bottles>\d+) bottles, (?P<mana>\d+) mana; "
    r"R (?P<r>\d+(?:\.\d+)?), which is (?P<strength>\d+) of strength against the (?P<power>\d+) "
    r"the old formula gives it"
    r"(?:; the double took (?P<steps>\d+) steps(?:, fired (?P<shots>\d+))?"
    r"(?: and stood paralysed (?P<para>\d+)s)?)?"
    r"(?: <s:.*)?$"
)
HELD = re.compile(r"^(?P<w>\S+)(?: with (?P<ammo>\d+) to fire)?$")

ROOM = re.compile(
    r"^Proving: (?P<guild>.+?)'s company \((?P<members>[^)]*)\) against the worst room of "
    r"(?P<deep>.+?) \((?P<room>[^)]*)\): (?P<verdict>cleared|wiped out|called at the cap) in (?P<s>\d+)s, "
    r"(?P<down>\d+) of (?P<foes>\d+) down, (?P<taken>\d+)\s?% of the room's health taken, "
    r"(?P<fell>\d+) of (?P<size>\d+) fell, (?P<spent>\d+)\s?% of the company's health spent"
)

TRACE = re.compile(
    r"^Proving trace: (?P<bot>.+?) \+(?P<t>\d+)s holding (?P<weapon>\S+) \((?P<ammo>-?\d+) left\), "
    r"swing in (?P<swing>-?\d+)ms, still (?P<still>-?\d+)ms, (?P<tiles>\d+) tiles, sees (?P<sees>\w+), "
    r"in sight (?P<los>\w+), paralysed (?P<para>\w+), frozen (?P<frozen>\w+), combatant (?P<comb>.+?), "
    r"warmode (?P<war>\w+), hits (?P<hits>-?\d+); the (?P<kind>\S+) (?P<fhits>-?\d+)/(?P<fmax>\d+), "
    r"its combatant (?P<fcomb>.+?), casting (?P<cast>\S+)"
)

SUMMARY = re.compile(r"^Proving: (?P<state>held|stopped), (?P<held>\d+) fights \((?P<won>\d+) won, "
                     r"(?P<lost>\d+) lost, (?P<called>\d+) called\), (?P<field>\d+) on the field now")
SUMMARY_ROOMS = re.compile(r"(\d+) companies against a dungeon's worst room \((\d+) cleared it, (\d+) did not\), "
                           r"cleared lately: (.*?), (\d+) could not be set up")
SUMMARY_TAIL = re.compile(r"(\d+) could not be set up, (\d+) asked for by hand, (\d+) faults, (\d+) things swept; "
                          r"(\d+) bots measured, (\d+) readings; the delve reads (proven strength|the old formula): "
                          r"(\d+) proven, (\d+) from a neighbouring rung, (\d+) by the formula \u00d7(\d+(?:\.\d+)?)")

LADDER = re.compile(r"^Proving: the ladder is read off the dungeons, (\d+) rungs: (.*?)(?: <s:.*)?$")
RUNG = re.compile(r"(\d+)\. (\S+) (\d+) \(([^)]*)\)")

MOVED = re.compile(
    r"^Proving moved a delve(?: offer)?: (?P<leader>.+?)'s band of (?P<band>\d+) is (?P<proven>\d+) of strength against "
    r"(?P<deep>.+?)'s worst by the proving ground and (?P<formula>\d+) by the old formula, so it can reach "
    r"(?P<reach>.+?) where the formula said (?P<said>.+?)(?: <s:.*)?$"
)

JUDGED = re.compile(r"judged by the proving ground: (\d+) offers went deeper than the old formula would have sent them, "
                    r"(\d+) less deep, (\d+) bands kept on the island that the formula would have sent down"
                    r"(?:, (\d+) parties sent elsewhere once raised)?"
                    r"(?:, (\d+) dungeons closed to a band by its weakest member \([^)]*\))?"
                    r"(?:, (\d+) by its guild's company not having cleared the worst room lately)?")

DEATH = re.compile(r"^(?P<bot>[A-Z][A-Za-z' ]*?) the (?P<cls>[A-Za-z]+) was killed at \((?P<x>-?\d+), (?P<y>-?\d+), (?P<z>-?\d+)\)")
KEPT = re.compile(r"^Kept: (?P<bot>.+?) fell at \(-?\d+, -?\d+\) with (?P<power>\d+) of strength in a company of "
                  r"(?P<company>\d+), killed by (?P<killer>.+?)(?: \((?P<kmight>\d+)\))?; ")
DIAL = re.compile(r"^A dial was moved by hand: BotWake\.Running (\w+) -> (\w+)")
WAKE_BOOT = re.compile(r"^The wake is (on|off)")

CENSUS = re.compile(
    r"^\[(\d\d:\d\d:\d\d)\]\s+-> of the (\d+) creatures within (\d+) tiles of a bot, (\d+) have their minds on; "
    r"of the (\d+) that would fight, (\d+); inside dungeons (\d+) of (\d+); (\d+) clients on the shard\. "
    r"The wake (is off|is on inside dungeons|is on everywhere a bot goes)"
)


# --------------------------------------------------------------------------------------------
# Small numeric helpers (no numpy).
# --------------------------------------------------------------------------------------------
def quantile(values, q):
    """Linear-interpolated quantile (numpy's default), or None for an empty list."""
    xs = sorted(v for v in values if v is not None)
    if not xs:
        return None
    pos = (len(xs) - 1) * q
    lo = math.floor(pos)
    hi = math.ceil(pos)
    return xs[lo] + (xs[hi] - xs[lo]) * (pos - lo)


def ranks(values):
    order = sorted(range(len(values)), key=lambda i: values[i])
    out = [0.0] * len(values)
    i = 0
    while i < len(order):
        j = i
        while j + 1 < len(order) and values[order[j + 1]] == values[order[i]]:
            j += 1
        mean_rank = (i + j) / 2.0 + 1.0
        for k in range(i, j + 1):
            out[order[k]] = mean_rank
        i = j + 1
    return out


def pearson(a, b):
    n = len(a)
    if n < 3:
        return None
    ma = sum(a) / n
    mb = sum(b) / n
    sa = math.sqrt(sum((x - ma) ** 2 for x in a))
    sb = math.sqrt(sum((y - mb) ** 2 for y in b))
    if sa == 0 or sb == 0:
        return None
    return sum((x - ma) * (y - mb) for x, y in zip(a, b)) / (sa * sb)


def spearman(a, b):
    """Spearman's rank correlation, ties averaged; None when it cannot be computed."""
    if len(a) != len(b) or len(a) < 3:
        return None
    return pearson(ranks(a), ranks(b))


def rnd(x, digits=3):
    return None if x is None else round(x, digits)


def stats_of(values, digits=3):
    xs = [v for v in values if v is not None]
    if not xs:
        return {"n": 0}
    return {
        "n": len(xs),
        "min": rnd(min(xs), digits),
        "p10": rnd(quantile(xs, 0.10), digits),
        "p25": rnd(quantile(xs, 0.25), digits),
        "median": rnd(statistics.median(xs), digits),
        "p75": rnd(quantile(xs, 0.75), digits),
        "p90": rnd(quantile(xs, 0.90), digits),
        "max": rnd(max(xs), digits),
    }


# --------------------------------------------------------------------------------------------
# Reading the logs.
# --------------------------------------------------------------------------------------------
class Clock:
    """Turns a file's HH:MM:SS stamps into full timestamps, adding a day when the clock goes back."""

    def __init__(self, date_str):
        self.day = datetime.strptime(date_str, "%Y-%m-%d")
        self.last = None

    def stamp(self, hh, mm, ss):
        t = timedelta(hours=int(hh), minutes=int(mm), seconds=int(ss))
        if self.last is not None and t < self.last - timedelta(hours=1):
            self.day += timedelta(days=1)
        self.last = t
        return self.day + t


def session_key(path):
    m = SESSION_NAME.search(os.path.basename(path))
    if not m:
        return None, None
    return f"{m.group(1)}_{m.group(2)}-{m.group(3)}", m.group(1)


def build_of(key):
    row = SESSION_BUILDS.get(key)
    return row[0] if row else ""


def build_number(build):
    """The build as a number for comparisons; '248+249' reads as 249; unknown as None."""
    if not build:
        return None
    return int(build.split("+")[-1])


def dungeon_at(x, y):
    for name, x1, y1, x2, y2 in DUNGEON_BOXES:
        if x1 <= x <= x2 and y1 <= y <= y2:
            return name
    return ""


def is_ranged(name):
    return name in RANGED_WEAPONS


def read_logs(logs, until=None):
    paths = [p for p in glob.glob(os.path.join(logs, "session-*.log")) if session_key(p)[0]]
    paths.sort(key=lambda p: session_key(p)[0])
    if until is not None:
        paths = [p for p in paths
                 if datetime.strptime(session_key(p)[0], "%Y-%m-%d_%H-%M") < until]

    fights, rooms, traces, summaries, ladders, moved, judged, deaths, dials, sessions = ([] for _ in range(10))
    unparsed = []

    for path in paths:
        key, date_str = session_key(path)
        build = build_of(key)
        clock = Clock(date_str)
        pending_traces = {}
        last_judged = None
        world_wake = ""
        info = {"session": key, "date": date_str, "build": build,
                "basis": SESSION_BUILDS.get(key, ("", "not in the table"))[1],
                "first_line": "", "last_line": "", "fights": 0, "companies": 0, "not_held": 0, "traces": 0,
                "world_wake_at_boot": ""}

        with open(path, encoding="utf-8", errors="replace") as f:
            for raw in f:
                m = LINE.match(raw.rstrip("\r\n"))
                if not m:
                    continue
                when = clock.stamp(m.group(1), m.group(2), m.group(3))
                if until is not None and when > until:
                    break
                body = m.group(4)
                if not info["first_line"]:
                    info["first_line"] = when.strftime("%H:%M:%S")
                info["last_line"] = when.strftime("%H:%M:%S")
                date = when.strftime("%Y-%m-%d")
                time = when.strftime("%H:%M:%S")

                if body.startswith("Proving trace: "):
                    t = TRACE.match(body)
                    if t:
                        row = {"_when": when, "date": date, "time": time, "session": key, "bot": t.group("bot"),
                               "t_s": int(t.group("t")), "weapon": t.group("weapon"), "ammo_left": int(t.group("ammo")),
                               "swing_in_ms": int(t.group("swing")), "still_ms": int(t.group("still")),
                               "tiles": int(t.group("tiles")), "sees": t.group("sees"), "in_sight": t.group("los"),
                               "paralysed": t.group("para"), "frozen": t.group("frozen"), "combatant": t.group("comb"),
                               "warmode": t.group("war"), "hits": int(t.group("hits")), "creature": t.group("kind"),
                               "creature_hits": int(t.group("fhits")), "creature_hits_max": int(t.group("fmax")),
                               "creature_combatant": t.group("fcomb"), "creature_casting": t.group("cast")}
                        pending_traces.setdefault(row["bot"], []).append(row)
                        traces.append(row)
                        info["traces"] += 1
                    else:
                        unparsed.append((key, time, body[:200]))
                    continue

                if body.startswith("Proving: "):
                    fm = FIGHT.match(body)
                    if fm:
                        fights.append(make_fight(fm, when, key, build, pending_traces))
                        info["fights"] += 1
                        continue
                    rm = ROOM.match(body)
                    if rm:
                        rooms.append(make_room(rm, date, time, key, build))
                        info["companies"] += 1
                        continue
                    sm = SUMMARY.match(body)
                    if sm:
                        row = make_summary(sm, body, date, time, key, build)
                        # The shard's own counters (since this boot) against the lines parsed so far in this session.
                        mine = [f for f in fights if f["session"] == key]
                        rms = [r for r in rooms if r["session"] == key]
                        parsed = (len(mine), sum(1 for f in mine if f["result"] == "won"),
                                  sum(1 for f in mine if f["result"] == "lost"),
                                  sum(1 for f in mine if f["result"] == "called"))
                        shard = (row["fights"], row["won"], row["lost"], row["called"])
                        agree = parsed == shard
                        if row["companies"] != "":
                            agree = agree and (len(rms), sum(1 for r in rms if r["result"] == "cleared")) == \
                                (row["companies"], row["companies_cleared"])
                        row["parsed_lines_agree"] = 1 if agree else 0
                        summaries.append(row)
                        continue
                    lm = LADDER.match(body)
                    if lm:
                        for r in RUNG.finditer(lm.group(2)):
                            ladders.append({"date": date, "time": time, "session": key, "build": build,
                                            "rung": int(r.group(1)), "creature": r.group(2), "might": int(r.group(3)),
                                            "dungeons": "; ".join(s.strip() for s in r.group(4).split(","))})
                        continue
                    if re.match(r"^Proving: \d+ things a stop had left", body):
                        continue
                    # A fight that could not be set up, or was taken off the field before it was decided: no reading.
                    if re.search(r" could not be (?:tried|set) against | was taken off the field undecided", body):
                        info["not_held"] += 1
                        continue
                    unparsed.append((key, time, body[:200]))
                    continue

                # Build 234 wrote "Proving moved a delve:", later builds "Proving moved a delve offer:".
                if body.startswith("Proving moved a delve"):
                    mm = MOVED.match(body)
                    if mm:
                        moved.append({"date": date, "time": time, "session": key, "build": build,
                                      "leader": mm.group("leader"), "band": int(mm.group("band")),
                                      "proven_strength": int(mm.group("proven")), "formula_strength": int(mm.group("formula")),
                                      "judged_against": mm.group("deep"), "can_reach": mm.group("reach"),
                                      "formula_said": mm.group("said")})
                    else:
                        unparsed.append((key, time, body[:200]))
                    continue

                if body.startswith("Delving: "):
                    jm = JUDGED.search(body)
                    if jm:
                        last_judged = (date, time, jm)
                    continue

                if body.startswith("Kept: "):
                    # Written in the same second as the death line it explains.
                    km = KEPT.match(body)
                    if km:
                        for d in reversed(deaths[-20:]):
                            if d["session"] == key and d["bot"] == km.group("bot") and d["time"] == time and not d["killer"]:
                                d["killer"] = km.group("killer")
                                d["killer_might"] = km.group("kmight") or ""
                                d["formula_power_at_death"] = km.group("power")
                                break
                    continue

                dm = DEATH.match(body)
                if dm:
                    x, y = int(dm.group("x")), int(dm.group("y"))
                    deaths.append({"date": date, "time": time, "session": key, "build": build,
                                   "bot": dm.group("bot"), "class": dm.group("cls"),
                                   "x": x, "y": y, "z": int(dm.group("z")),
                                   "dungeon_side": 1 if x >= DUNGEON_SIDE_X else 0,
                                   "dungeon": dungeon_at(x, y), "killer": "", "killer_might": "",
                                   "formula_power_at_death": ""})
                    continue

                wm = WAKE_BOOT.match(body)
                if wm and not world_wake:
                    world_wake = wm.group(1)
                    info["world_wake_at_boot"] = world_wake
                    continue

                dl = DIAL.match(body)
                if dl:
                    dials.append({"date": date, "time": time, "session": key, "from": dl.group(1), "to": dl.group(2)})
                    continue

        if last_judged:
            date, time, jm = last_judged
            g = [int(x) if x is not None else "" for x in jm.groups()]
            judged.append({"session": key, "build": build, "date": date, "time": time,
                           "offers_deeper": g[0], "offers_less_deep": g[1], "bands_kept_on_island": g[2],
                           "parties_sent_elsewhere": g[3], "closed_by_weakest_member": g[4],
                           "closed_by_room_gate": g[5]})
        sessions.append(info)

    # Number the fights and link the traces.
    for i, f in enumerate(fights, 1):
        f["fight_id"] = i
        for t in f.pop("_traces"):
            t["fight_id"] = i
    for t in traces:
        t.setdefault("fight_id", "")
    for i, r in enumerate(rooms, 1):
        r["company_id"] = i
    return {"fights": fights, "rooms": rooms, "traces": traces, "summaries": summaries, "ladders": ladders,
            "moved": moved, "judged": judged, "deaths": deaths, "dials": dials, "sessions": sessions,
            "unparsed": unparsed}


def make_fight(fm, when, key, build, pending_traces):
    g = fm.groupdict()
    date = when.strftime("%Y-%m-%d")
    time = when.strftime("%H:%M:%S")
    if g["won_s"] is not None:
        result, reason, seconds, left = "won", "", int(g["won_s"]), 0
    elif g["lost_s"] is not None:
        result, reason, seconds, left = "lost", "", int(g["lost_s"]), int(g["lost_left"])
    else:
        result = "called"
        reason = "cap" if "cap" in g["called"] else "still"
        seconds, left = int(g["called_s"]), int(g["called_left"])

    held_w, held_ammo = "", ""
    if g["held"] is not None:
        hm = HELD.match(g["held"])
        if hm:
            held_w = hm.group("w")
            held_ammo = int(hm.group("ammo")) if hm.group("ammo") else ""
        else:
            held_w = g["held"]

    hits = int(g["hits"])
    taken = int(g["taken"])
    healed = int(g["healed"])
    might = int(g["might"])
    power = int(g["power"])
    strength = int(g["strength"])
    r = float(g["r"])
    new_line = g["steps"] is not None
    bn = build_number(build)

    weapons = g["weapon"].replace("-dry", "").split("+")
    # The traces of this fight are the bot's pending ones that fall inside it (a trace of a fight that was never
    # settled would otherwise be pinned on the bot's next one).
    began = when - timedelta(seconds=seconds + 2)
    traces = [t for t in pending_traces.pop(g["bot"], []) if t["_when"] >= began]

    return {
        "fight_id": 0,
        "date": date,
        "time": time,
        "session": key,
        "build": build,
        "rings_awake": 1 if key >= RINGS_AWAKE_FROM else 0,
        "world_awake": 1 if key >= WORLD_AWAKE_FROM else 0,
        "r_cap": 20 if bn is not None and bn < 235 else 5,
        "bot": g["bot"],
        "class": g["cls"],
        "fighting_skills": int(g["skills"]),
        "stats": int(g["stats"]),
        "armour": int(g["armour"]),
        "weapon": g["weapon"],
        "weapon_damage": int(g["damage"]),
        "carries_bow": 1 if any(is_ranged(w) for w in weapons) else 0,
        "held": held_w,
        "held_ammo": held_ammo,
        "weapon_changes": int(g["draws"]) if g["draws"] is not None else "",
        "creature": g["kind"],
        "creature_might": might,
        "result": result,
        "called_reason": reason,
        "seconds": seconds,
        "creature_left_pct": left,
        "share_killed": round(1.0 - left / 100.0, 2),
        "taken": taken,
        "healed": healed,
        "hits_max": hits,
        "net_damage": round(max(taken - healed, 0.02 * max(1, hits)), 2),
        "dealt": int(g["dealt"]),
        "spells": int(g["spells"]),
        "healing_spells": int(g["heals"]),
        "bandages": int(g["bandages"]),
        "bottles": int(g["bottles"]),
        "mana_spent": int(g["mana"]),
        "r": r,
        "proven_strength": strength,
        "formula_power": power,
        "proven_over_formula": round(strength / power, 4) if power > 0 else "",
        "formula_implied_r": round(power / might, 4) if might > 0 else "",
        "steps": int(g["steps"]) if new_line else "",
        "shots": int(g["shots"]) if g["shots"] is not None else "",
        "paralysed_s": (int(g["para"]) if g["para"] is not None else 0) if new_line else "",
        "traced_seconds": len(traces),
        "_traces": traces,
    }


def make_room(rm, date, time, key, build):
    g = rm.groupdict()
    members = [s.strip() for s in g["members"].split(",") if s.strip()]
    return {
        "company_id": 0,
        "date": date,
        "time": time,
        "session": key,
        "build": build,
        "rings_awake": 1 if key >= RINGS_AWAKE_FROM else 0,
        "guild": g["guild"],
        "members": "; ".join(members),
        "company_size": int(g["size"]),
        "dungeon": g["deep"],
        "room": g["room"],
        "room_creatures": int(g["foes"]),
        "result": g["verdict"],
        "seconds": int(g["s"]),
        "creatures_down": int(g["down"]),
        "room_health_taken_pct": int(g["taken"]),
        "fell": int(g["fell"]),
        "company_health_spent_pct": int(g["spent"]),
    }


def make_summary(sm, body, date, time, key, build):
    row = {"date": date, "time": time, "session": key, "build": build, "state": sm.group("state"),
           "fights": int(sm.group("held")), "won": int(sm.group("won")), "lost": int(sm.group("lost")),
           "called": int(sm.group("called")), "on_field": int(sm.group("field")),
           "companies": "", "companies_cleared": "", "companies_failed": "", "cleared_lately": "",
           "unset": "", "by_hand": "", "faults": "", "swept": "", "bots_measured": "", "readings": "",
           "delve_reads": "", "asked_proven": "", "asked_neighbour": "", "asked_formula": "", "calibration": ""}
    rm = SUMMARY_ROOMS.search(body)
    if rm:
        row.update(companies=int(rm.group(1)), companies_cleared=int(rm.group(2)), companies_failed=int(rm.group(3)),
                   cleared_lately=rm.group(4))
    tm = SUMMARY_TAIL.search(body)
    if tm:
        row.update(unset=int(tm.group(1)), by_hand=int(tm.group(2)), faults=int(tm.group(3)), swept=int(tm.group(4)),
                   bots_measured=int(tm.group(5)), readings=int(tm.group(6)),
                   delve_reads="proven" if tm.group(7) == "proven strength" else "formula",
                   asked_proven=int(tm.group(8)), asked_neighbour=int(tm.group(9)), asked_formula=int(tm.group(10)),
                   calibration=float(tm.group(11)))
    return row


def read_census(logs, sessions, until=None):
    """Answers to the door verb 'awake' (build 251). The door log has no dates: the first answer is dated by the
    first session whose boot line says 'The wake is ...' (the verb and the wake came in the same build, 251), and a
    day is added whenever the clock goes backwards after that."""
    path = os.path.join(logs, "bot-debugger-commands.log")
    anchor = next((s["date"] for s in sessions if s["world_wake_at_boot"]), None)
    if anchor is None or not os.path.exists(path):
        return []
    rows = []
    day = datetime.strptime(anchor, "%Y-%m-%d")
    last = None
    with open(path, encoding="utf-8", errors="replace") as f:
        for line in f:
            line = line.rstrip("\r\n")
            cm = CENSUS.match(line)
            tm = re.match(r"^\[(\d\d):(\d\d):(\d\d)\]", line)
            if tm and (rows or cm):
                t = timedelta(hours=int(tm.group(1)), minutes=int(tm.group(2)), seconds=int(tm.group(3)))
                if last is not None and t < last - timedelta(hours=1):
                    day += timedelta(days=1)
                last = t
            if not cm:
                continue
            if until is not None and day + last > until:
                break
            g = cm.groups()
            rows.append({"date": day.strftime("%Y-%m-%d"), "time": g[0], "creatures_near_bots": int(g[1]),
                         "reach_tiles": int(g[2]), "ai_on": int(g[3]), "would_fight": int(g[4]),
                         "would_fight_ai_on": int(g[5]), "in_dungeons_ai_on": int(g[6]),
                         "in_dungeons": int(g[7]), "clients": int(g[8]), "wake": g[9].replace("is ", "")})
    return rows


# --------------------------------------------------------------------------------------------
# Derived tables.
# --------------------------------------------------------------------------------------------
def period_of(f):
    if not f["rings_awake"]:
        return "asleep"
    return "awake_world_awake" if f["world_awake"] else "awake"


PERIOD_LABELS = {
    "asleep": "builds 234-247: the rings' creatures asleep",
    "awake": "builds 249-258: the rings woken, the rest of the world asleep",
    "awake_world_awake": "build 259 on: the rings woken and the world woken round the bots",
}


def outcome_counts(rows):
    won = sum(1 for f in rows if f["result"] == "won")
    lost = sum(1 for f in rows if f["result"] == "lost")
    called = sum(1 for f in rows if f["result"] == "called")
    return {"fights": len(rows), "won": won, "lost": lost, "called": called,
            "win_rate": rnd(won / len(rows)) if rows else None}


def group_table(rows, key):
    groups = {}
    for f in rows:
        groups.setdefault(f[key], []).append(f)
    out = []
    for k, fs in groups.items():
        c = outcome_counts(fs)
        out.append({key: k, **c,
                    "bots": len({f["bot"] for f in fs}),
                    "median_r": rnd(statistics.median(f["r"] for f in fs)),
                    "median_seconds": rnd(statistics.median(f["seconds"] for f in fs), 1),
                    "median_proven_over_formula": rnd(statistics.median(
                        f["proven_over_formula"] for f in fs if f["proven_over_formula"] != "")),
                    "median_formula_power": rnd(statistics.median(f["formula_power"] for f in fs), 1),
                    "median_hits_max": rnd(statistics.median(f["hits_max"] for f in fs), 1),
                    "median_fighting_skills": rnd(statistics.median(f["fighting_skills"] for f in fs), 1),
                    "median_armour": rnd(statistics.median(f["armour"] for f in fs), 1),
                    "mean_spells": rnd(sum(f["spells"] for f in fs) / len(fs), 2),
                    "mean_bandages": rnd(sum(f["bandages"] for f in fs) / len(fs), 2),
                    "mean_bottles": rnd(sum(f["bottles"] for f in fs) / len(fs), 2)})
    out.sort(key=lambda r: (-r["fights"], str(r[key])))
    return out


def trace_fights(traces, fights_by_id):
    """Per traced fight: shots (drops in the ammunition count), the seconds between them, when the creature came
    within one tile, and what it cast."""
    by_fight = {}
    for t in traces:
        if t["fight_id"] != "":
            by_fight.setdefault(t["fight_id"], []).append(t)
    out = []
    for fid, ts in sorted(by_fight.items()):
        ts.sort(key=lambda t: t["t_s"])
        f = fights_by_id[fid]
        shots_at = []
        timer_after_shot = []
        first = ts[0]
        # A shot fired between the start of the fight and the first trace (the double's timer set to "now", build 255):
        # the fight line's "with N to fire" is then one more than the first trace's count.
        if f["held_ammo"] != "" and first["ammo_left"] >= 0 and f["held_ammo"] > first["ammo_left"] and first["hits"] > 0:
            shots_at.extend([0] * (f["held_ammo"] - first["ammo_left"]))
            timer_after_shot.append(first["swing_in_ms"])
        prev = None
        for t in ts:
            # Only while the double stands: at death its arrows go to the corpse and the count drops (build 254).
            if (prev is not None and t["hits"] > 0 and t["ammo_left"] >= 0 and prev["ammo_left"] >= 0
                    and t["ammo_left"] < prev["ammo_left"]):
                shots_at.extend([t["t_s"]] * (prev["ammo_left"] - t["ammo_left"]))
                # The next swing as the trace saw it at most a second after this shot: a lower bound on the delay.
                timer_after_shot.append(t["swing_in_ms"])
            prev = t
        gaps = [b - a for a, b in zip(shots_at, shots_at[1:])]
        closed = next((t["t_s"] for t in ts if t["tiles"] <= 1), "")
        spells = []
        for t in ts:
            if t["creature_casting"] != "no" and (not spells or spells[-1] != t["creature_casting"]):
                spells.append(t["creature_casting"])
        para = sum(1 for t in ts if t["paralysed"] == "True")
        out.append({"fight_id": fid, "date": f["date"], "time": f["time"], "build": f["build"], "bot": f["bot"],
                    "class": f["class"],
                    "creature": f["creature"], "result": f["result"], "seconds": f["seconds"],
                    "traced_seconds": len(ts), "first_trace_weapon": first["weapon"],
                    "first_swing_in_ms": first["swing_in_ms"], "start_tiles": first["tiles"],
                    "ammo_at_start": f["held_ammo"], "ammo_first_trace": first["ammo_left"],
                    "first_shot_at_s": shots_at[0] if shots_at else "",
                    "shots_seen": len(shots_at), "shot_times_s": " ".join(str(s) for s in shots_at),
                    "shot_gaps_s": " ".join(str(g) for g in gaps),
                    "median_shot_gap_s": rnd(statistics.median(gaps), 2) if gaps else "",
                    "swing_in_ms_after_shots": " ".join(str(x) for x in timer_after_shot),
                    "creature_within_1_tile_at_s": closed,
                    "creature_spells_in_order": " ".join(s.replace("Spell", "") for s in spells),
                    "seconds_seen_paralysed": para,
                    "creature_hits_start": first["creature_hits"], "creature_hits_max": first["creature_hits_max"],
                    "creature_hits_end": ts[-1]["creature_hits"], "double_hits_end": ts[-1]["hits"]})
    return out


def per_bot(fights):
    out = []
    bots = {}
    for f in fights:
        bots.setdefault(f["bot"], []).append(f)
    for bot, fs in sorted(bots.items()):
        awake = [f for f in fs if f["rings_awake"]]
        last = awake[-1] if awake else None
        om = [f for f in awake if f["creature"] == "OrcishMage"]
        out.append({
            "bot": bot,
            "class": fs[-1]["class"],
            "fights": len(fs),
            "fights_asleep": len(fs) - len(awake),
            "fights_awake": len(awake),
            "won_awake": sum(1 for f in awake if f["result"] == "won"),
            "creatures_beaten_awake": "; ".join(sorted({f["creature"] for f in awake if f["result"] == "won"})),
            "orcish_mage_fights_awake": len(om),
            "orcish_mage_won_awake": sum(1 for f in om if f["result"] == "won"),
            "orcish_mage_median_r_awake": rnd(statistics.median(f["r"] for f in om)) if om else "",
            "first_fight": f"{fs[0]['date']} {fs[0]['time']}",
            "last_fight": f"{fs[-1]['date']} {fs[-1]['time']}",
            "fighting_skills_first": fs[0]["fighting_skills"],
            "fighting_skills_last": fs[-1]["fighting_skills"],
            "armour_first": fs[0]["armour"],
            "armour_last": fs[-1]["armour"],
            "last_awake_time": f"{last['date']} {last['time']}" if last else "",
            "last_awake_creature": last["creature"] if last else "",
            "last_awake_result": last["result"] if last else "",
            "last_awake_r": last["r"] if last else "",
            "last_awake_proven_strength": last["proven_strength"] if last else "",
            "last_awake_formula_power": last["formula_power"] if last else "",
            "last_awake_proven_over_formula": last["proven_over_formula"] if last else "",
        })
    return out


def window_deaths(deaths, start, end, dungeon_side_only=True):
    rows = [d for d in deaths if (d["date"], d["time"]) >= start and (d["date"], d["time"]) < end
            and (d["dungeon_side"] or not dungeon_side_only)]
    return rows


def minutes_between(a, b):
    ta = datetime.strptime(f"{a[0]} {a[1]}", "%Y-%m-%d %H:%M:%S")
    tb = datetime.strptime(f"{b[0]} {b[1]}", "%Y-%m-%d %H:%M:%S")
    return (tb - ta).total_seconds() / 60.0


def summarise(data, census, trace_rows):
    fights = data["fights"]
    rooms = data["rooms"]
    s = {}
    if not fights:
        return {"note": "no proving fights found"}

    s["cut_off"] = {"first_fight": f"{fights[0]['date']} {fights[0]['time']}",
                    "last_fight": f"{fights[-1]['date']} {fights[-1]['time']}",
                    "last_session": data["sessions"][-1]["session"] if data["sessions"] else "",
                    "last_line_of_last_session": data["sessions"][-1]["last_line"] if data["sessions"] else ""}
    s["counts"] = {"fights": len(fights), "companies": len(rooms), "trace_lines": len(data["traces"]),
                   "bots": len({f["bot"] for f in fights}),
                   "fight_lines_shape": {
                       "no 'double held' part (builds 234-237)": sum(1 for f in fights if f["weapon_changes"] == ""),
                       "'double held', no steps (238-251)": sum(1 for f in fights if f["weapon_changes"] != "" and f["steps"] == ""),
                       "steps, shots and paralysis (252 on)": sum(1 for f in fights if f["steps"] != "")},
                   "unparsed_proving_lines": len(data["unparsed"]),
                   "summary_lines": len(data["summaries"]),
                   "summary_lines_agreeing_with_parsed_lines": sum(x["parsed_lines_agree"] for x in data["summaries"])}

    # Periods.
    periods = {}
    for f in fights:
        periods.setdefault(period_of(f), []).append(f)
    s["periods"] = {}
    for p in ("asleep", "awake", "awake_world_awake"):
        fs = periods.get(p, [])
        if not fs:
            continue
        ratios = [f["proven_over_formula"] for f in fs if f["proven_over_formula"] != ""]
        s["periods"][p] = {
            "label": PERIOD_LABELS[p],
            "first_fight": f"{fs[0]['date']} {fs[0]['time']}", "last_fight": f"{fs[-1]['date']} {fs[-1]['time']}",
            **outcome_counts(fs),
            "called_at_cap": sum(1 for f in fs if f["called_reason"] == "cap"),
            "called_nobody_hurt": sum(1 for f in fs if f["called_reason"] == "still"),
            "bots": len({f["bot"] for f in fs}),
            "fights_with_no_damage_taken": sum(1 for f in fs if f["taken"] == 0),
            "fights_at_r_cap": sum(1 for f in fs if f["r"] >= f["r_cap"]),
            "creatures": sorted({f["creature"] for f in fs}),
            "orcish_mage": outcome_counts([f for f in fs if f["creature"] == "OrcishMage"]),
            "r": stats_of([f["r"] for f in fs]),
            "proven_over_formula": stats_of(ratios),
            "seconds_won": stats_of([f["seconds"] for f in fs if f["result"] == "won"], 1),
            "seconds_lost": stats_of([f["seconds"] for f in fs if f["result"] == "lost"], 1),
        }

    # Asleep: what the double held when the fight began (known from build 238 on).
    asleep = periods.get("asleep", [])
    if asleep:
        def held_kind(f):
            if f["held"] == "":
                return "unknown (builds 234-237)"
            return "bow" if is_ranged(f["held"]) else "blade, staff or fists"
        by_held = {}
        for f in asleep:
            by_held.setdefault(held_kind(f), []).append(f)
        s["asleep_by_what_the_double_held"] = {
            k: {**outcome_counts(v), "no_damage_taken": sum(1 for f in v if f["taken"] == 0)}
            for k, v in sorted(by_held.items())}
        s["asleep_walkovers_at_r_20"] = [
            f"{f['time']} {f['bot']} vs {f['creature']}: {f['result']} in {f['seconds']}s, taking {f['taken']}, "
            f"R {f['r']}, {f['proven_strength']} of strength (formula {f['formula_power']})"
            for f in asleep if f["r"] >= 20]

    # The first hour of fights, and the first fights after the rings were woken.
    first = fights[0]
    t0 = datetime.strptime(f"{first['date']} {first['time']}", "%Y-%m-%d %H:%M:%S")
    hour = [f for f in fights
            if datetime.strptime(f"{f['date']} {f['time']}", "%Y-%m-%d %H:%M:%S") < t0 + timedelta(hours=1)]
    hr = [f["proven_over_formula"] for f in hour if f["proven_over_formula"] != ""]
    s["first_hour"] = {"from": f"{first['date']} {first['time']}", "fights": len(hour),
                       **{k: v for k, v in outcome_counts(hour).items() if k != "fights"},
                       "proven_over_formula_min": rnd(min(hr), 3) if hr else None,
                       "proven_over_formula_max": rnd(max(hr), 3) if hr else None,
                       "note": "strength / formula per fight line, rings asleep, R capped at 20 then 5"}
    awake = [f for f in fights if f["rings_awake"]]
    if awake:
        first35 = awake[:35]
        s["first_35_awake"] = {"from": f"{first35[0]['date']} {first35[0]['time']}",
                               "to": f"{first35[-1]['date']} {first35[-1]['time']}", **outcome_counts(first35),
                               "winners": sorted({f"{f['bot']} vs {f['creature']}" for f in first35 if f["result"] == "won"})}
        # The shard's own counter is per boot: "35 fights (2 won, 33 lost)" at 03:53 counted build 250's session only.
        b250 = [f for f in awake if f["build"] == "250"][:35]
        if b250:
            s["first_35_of_build_250"] = {"to": f"{b250[-1]['date']} {b250[-1]['time']}", **outcome_counts(b250),
                                          "winners": [f"{f['bot']} vs {f['creature']}" for f in b250 if f["result"] == "won"]}
        # The night report's snapshot ended at 09:26:07 ("320 fights with awake creatures, 35 won, 285 lost").
        night = [f for f in awake if (f["date"], f["time"]) <= ("2026-09-26", "09:26:07")]
        s["awake_to_report_snapshot"] = {"to": "2026-09-26 09:26:07", **outcome_counts(night),
                                         "bots": len({f["bot"] for f in night}),
                                         "note": "cross-check with the night report's data snapshot"}

        # By class and creature, rings awake.
        s["awake_by_class"] = group_table(awake, "class")
        s["awake_by_creature"] = group_table(awake, "creature")
        above = [f for f in awake if f["creature"] != "OrcishMage"]
        s["awake_above_rung_1"] = {**outcome_counts(above),
                                   "wins": [f"{f['time']} {f['bot']} the {f['class']} ({f['fighting_skills']} skills, "
                                            f"armour {f['armour']}) beat the {f['creature']} in {f['seconds']}s, R {f['r']}"
                                            for f in above if f["result"] == "won"]}

        # What goes with R against the orcish mage, rings awake: rank correlations.
        om = [f for f in awake if f["creature"] == "OrcishMage"]
        cols = {"formula_power": "formula_power", "fighting_skills": "fighting_skills", "armour": "armour",
                "stats": "stats", "weapon_damage": "weapon_damage", "hits_max": "hits_max",
                "bandages": "bandages", "bottles": "bottles", "paralysed_s": "paralysed_s"}
        corr = {}
        for name, col in cols.items():
            pairs = [(f[col], f["r"]) for f in om if f[col] != ""]
            corr[name] = {"n": len(pairs), "spearman_with_r": rnd(spearman([a for a, _ in pairs], [b for _, b in pairs]))}
        s["awake_orcish_mage_rank_correlations"] = corr

        # The same bots fought many times, so the fights are not independent: the same question per bot, on each
        # bot's medians (bots with at least three such fights).
        per = {}
        for f in om:
            per.setdefault(f["bot"], []).append(f)
        per = {b: fs for b, fs in per.items() if len(fs) >= 3}
        med_r = [statistics.median(f["r"] for f in fs) for fs in per.values()]
        bcorr = {"bots": len(per)}
        for name in ("formula_power", "fighting_skills", "hits_max", "armour", "stats", "weapon_damage"):
            xs = [statistics.median(f[name] for f in fs) for fs in per.values()]
            bcorr[name] = rnd(spearman(xs, med_r))
        s["awake_orcish_mage_rank_correlations_per_bot"] = bcorr

        # Wins against the orcish mage by the formula's own prediction.
        fr = [f["formula_implied_r"] for f in om]
        s["awake_orcish_mage_formula_implied_r"] = stats_of(fr)
        s["awake_orcish_mage_formula_implied_r_at_least_1"] = sum(1 for x in fr if x >= 1)
        s["awake_orcish_mage_won_where_formula_implied_r_under_1"] = sum(
            1 for f in om if f["result"] == "won" and f["formula_implied_r"] < 1)

        # The orcish mage, period against period: who was fighting it and with what.
        s["awake_orcish_mage_by_period"] = {}
        for p in ("awake", "awake_world_awake"):
            fs = [f for f in om if period_of(f) == p]
            if fs:
                s["awake_orcish_mage_by_period"][p] = {
                    **outcome_counts(fs), "bots": len({f["bot"] for f in fs}),
                    "median_fighting_skills": rnd(statistics.median(f["fighting_skills"] for f in fs), 1),
                    "median_hits_max": rnd(statistics.median(f["hits_max"] for f in fs), 1),
                    "median_armour": rnd(statistics.median(f["armour"] for f in fs), 1),
                    "mean_bandages": rnd(sum(f["bandages"] for f in fs) / len(fs), 2),
                    "mean_bottles": rnd(sum(f["bottles"] for f in fs) / len(fs), 2),
                    "mean_spells": rnd(sum(f["spells"] for f in fs) / len(fs), 2),
                    "median_r": rnd(statistics.median(f["r"] for f in fs))}
        casters = {}
        for f in awake:
            if f["class"] in ("Mage", "Sage", "Healer", "WarriorMage"):
                casters.setdefault(f"{f['class']} / {period_of(f)}", []).append(f)
        s["awake_casters_spells"] = {k: {"fights": len(v), "fights_with_a_spell": sum(1 for f in v if f["spells"] > 0),
                                         "won": sum(1 for f in v if f["result"] == "won")}
                                     for k, v in sorted(casters.items())}

        # Paralysis and shots, from the line (build 252 on).
        newer = [f for f in fights if f["steps"] != ""]
        para = [f["paralysed_s"] for f in newer]
        s["paralysis"] = {"fights": len(newer), "with_any": sum(1 for p in para if p > 0),
                          "seconds_when_any": stats_of([p for p in para if p > 0], 1),
                          "window_0420_0428": stats_of([f["paralysed_s"] for f in newer
                                                        if ("2026-09-26", "04:20:00") <= (f["date"], f["time"]) <= ("2026-09-26", "04:28:59")], 1)}
        bow = [f for f in newer if f["shots"] != ""]
        # Build 252-253 counted the arrows a dead double's corpse took as shots; build 254 fixed it.
        counted = [f for f in bow if (build_number(f["build"]) or 999) >= 254]
        s["bow_fights"] = {"fights_starting_with_a_bow": len(bow), **outcome_counts(bow),
                           "counted_from_build_254": len(counted),
                           "shots": stats_of([f["shots"] for f in counted], 1),
                           "fights_with_no_shot": sum(1 for f in counted if f["shots"] == 0),
                           "shots_per_minute": stats_of([f["shots"] * 60.0 / f["seconds"] for f in counted if f["seconds"] > 0], 2),
                           "dealt": stats_of([f["dealt"] for f in bow], 1),
                           "held_ammo_at_start": stats_of([f["held_ammo"] for f in bow if f["held_ammo"] != ""], 1)}
        s["traced_fights"] = {"fights": len(trace_rows),
                              "with_a_shot": sum(1 for t in trace_rows if t["shots_seen"] > 0),
                              "shot_gaps_s": stats_of([float(g) for t in trace_rows for g in str(t["shot_gaps_s"]).split() if g], 2),
                              "swing_in_ms_after_shots": stats_of([float(g) for t in trace_rows
                                                                   for g in str(t["swing_in_ms_after_shots"]).split() if g], 0),
                              "creature_within_1_tile_at_s": stats_of([t["creature_within_1_tile_at_s"] for t in trace_rows
                                                                       if t["creature_within_1_tile_at_s"] != ""], 1),
                              "first_swing_in_ms_build_254": [t["first_swing_in_ms"] for t in trace_rows
                                                              if t["build"] == "254"],
                              "first_shot_at_s_build_254": [t["first_shot_at_s"] for t in trace_rows
                                                            if t["build"] == "254"],
                              "later_with_arrows": sum(1 for t in trace_rows if t["build"] != "254"
                                                       and t["ammo_at_start"] not in ("", 0)),
                              "later_first_shot_at_0s": sum(1 for t in trace_rows if t["build"] != "254"
                                                            and t["first_shot_at_s"] == 0),
                              "later_first_swing_in_ms": stats_of([t["first_swing_in_ms"] for t in trace_rows
                                                                   if t["build"] != "254"
                                                                   and t["ammo_at_start"] not in ("", 0)], 0)}

    # Companies.
    s["companies"] = {}
    for label, rs in (("asleep", [r for r in rooms if not r["rings_awake"]]), ("awake", [r for r in rooms if r["rings_awake"]])):
        if not rs:
            continue
        s["companies"][label] = {
            "fights": len(rs), "cleared": sum(1 for r in rs if r["result"] == "cleared"),
            "wiped_out": sum(1 for r in rs if r["result"] == "wiped out"),
            "called": sum(1 for r in rs if r["result"] == "called at the cap"),
            "rooms": sorted({f"{r['dungeon']}: {r['room']}" for r in rs}),
            "seconds_wiped_out": stats_of([r["seconds"] for r in rs if r["result"] == "wiped out"], 1),
            "room_health_taken_pct_wiped_out": stats_of([r["room_health_taken_pct"] for r in rs if r["result"] == "wiped out"], 1),
            "clears": [f"{r['date']} {r['time']} {r['guild']} ({r['members']}) {r['dungeon']} in {r['seconds']}s, "
                       f"{r['fell']} of {r['company_size']} fell" for r in rs if r["result"] == "cleared"],
        }

    # The formula's worth as the shard itself reckoned it (five-minute summaries).
    cal = [x for x in data["summaries"] if x["calibration"] != ""]
    if cal:
        by_build = {}
        for x in cal:
            by_build.setdefault(x["build"], []).append(x["calibration"])
        s["calibration"] = {"readings": len(cal), "first": f"{cal[0]['date']} {cal[0]['time']} x{cal[0]['calibration']}",
                            "last": f"{cal[-1]['date']} {cal[-1]['time']} x{cal[-1]['calibration']}",
                            "by_build": {b: {"n": len(v), "min": min(v), "max": max(v)} for b, v in by_build.items()},
                            "awake_rings": stats_of([x["calibration"] for x in cal if x["session"] >= RINGS_AWAKE_FROM], 2)}

    # Creature might across boots.
    might = {}
    for l in data["ladders"]:
        might.setdefault(l["creature"], []).append(l["might"])
    s["creature_might_across_boots"] = {k: {"boots": len(v), "min": min(v), "max": max(v)} for k, v in might.items()}

    # The wake experiment and its baseline.
    dials = data["dials"]
    exp = None
    for i, d in enumerate(dials):
        if d["to"] == "true":
            off = next((e for e in dials[i + 1:] if e["to"] == "false"), None)
            if off:
                exp = ((d["date"], d["time"]), (off["date"], off["time"]))
                break
    if exp:
        ds = window_deaths(data["deaths"], exp[0], exp[1])
        mins = minutes_between(exp[0], exp[1])
        s["wake_experiment"] = {"on": f"{exp[0][0]} {exp[0][1]}", "off": f"{exp[1][0]} {exp[1][1]}",
                                "minutes": rnd(mins, 2), "dungeon_side_deaths": len(ds),
                                "bots": sorted({d["bot"] for d in ds}),
                                "per_hour": rnd(len(ds) * 60.0 / mins, 1) if mins else None,
                                "dungeons": sorted({d["dungeon"] for d in ds}),
                                "killers": sorted(d["killer"] for d in ds)}
    base = window_deaths(data["deaths"], BASELINE_FROM, BASELINE_TO)
    bm = minutes_between(BASELINE_FROM, BASELINE_TO)
    s["asleep_baseline"] = {"from": " ".join(BASELINE_FROM), "to": " ".join(BASELINE_TO),
                            "dungeon_side_deaths": len(base), "per_hour": rnd(len(base) * 60.0 / bm, 1)}

    # Delve offers the readings moved, and the delve's own counters.
    moved = data["moved"]
    s["delve_offers_moved"] = {"lines": len(moved),
                               "note": "the delve logs at most the first 8 differences per boot",
                               "by_target": {}}
    for m in moved:
        k = f"{m['can_reach']} (formula said {m['formula_said']})"
        s["delve_offers_moved"]["by_target"][k] = s["delve_offers_moved"]["by_target"].get(k, 0) + 1
    s["delve_offers_moved"]["build_234"] = [f"{m['time']} {m['leader']}'s band of {m['band']}: {m['proven_strength']} "
                                            f"proven vs {m['formula_strength']} formula -> {m['can_reach']}"
                                            for m in moved if m["build"] == "234"]
    s["census"] = census
    return s


def page_payload(data, summary, trace_rows, bots):
    """A compact copy for index.html: columns for the charts, not every field."""
    fcols = ["fight_id", "date", "time", "build", "rings_awake", "world_awake", "bot", "class", "fighting_skills",
             "armour", "creature", "creature_might", "result", "seconds", "creature_left_pct", "taken", "healed",
             "hits_max", "dealt", "r", "proven_strength", "formula_power", "proven_over_formula", "formula_implied_r",
             "paralysed_s", "shots", "r_cap"]
    ccols = ["company_id", "date", "time", "build", "rings_awake", "guild", "company_size", "dungeon", "room",
             "result", "seconds", "creatures_down", "room_creatures", "room_health_taken_pct", "fell"]
    scols = ["date", "time", "build", "fights", "won", "lost", "bots_measured", "readings", "calibration"]
    return {
        "generated_from": summary.get("cut_off", {}),
        "fights": {"columns": fcols, "rows": [[f[c] for c in fcols] for f in data["fights"]]},
        "companies": {"columns": ccols, "rows": [[r[c] for c in ccols] for r in data["rooms"]]},
        "summaries": {"columns": scols, "rows": [[x[c] for c in scols] for x in data["summaries"] if x["calibration"] != ""]},
        "bots": [{k: b[k] for k in ("bot", "class", "fights_awake", "won_awake", "last_awake_creature",
                                    "last_awake_result", "last_awake_r", "last_awake_proven_strength",
                                    "last_awake_formula_power")} for b in bots if b["fights_awake"]],
        "ladder_first_boot_awake": [l for l in data["ladders"] if l["session"] == RINGS_AWAKE_FROM],
        "traced": trace_rows,
        "census": summary.get("census", []),
        "summary": {k: summary[k] for k in ("cut_off", "counts", "periods", "first_hour", "first_35_awake",
                                            "first_35_of_build_250", "companies", "calibration", "wake_experiment",
                                            "asleep_baseline", "paralysis", "bow_fights", "traced_fights",
                                            "awake_orcish_mage_rank_correlations_per_bot",
                                            "awake_orcish_mage_rank_correlations", "awake_above_rung_1",
                                            "asleep_by_what_the_double_held", "awake_orcish_mage_by_period")
                    if k in summary},
    }


# --------------------------------------------------------------------------------------------
# Output.
# --------------------------------------------------------------------------------------------
def write_csv(path, rows, columns=None):
    if columns is None:
        columns = list(rows[0].keys()) if rows else []
    with open(path, "w", encoding="utf-8", newline="") as f:
        w = csv.DictWriter(f, fieldnames=columns, extrasaction="ignore", lineterminator="\n")
        w.writeheader()
        for r in rows:
            w.writerow(r)


def refresh_html(html_path, payload):
    if not os.path.exists(html_path):
        return False
    with open(html_path, encoding="utf-8") as f:
        html = f.read()
    start_tag = '<script id="proving-data" type="application/json">'
    i = html.find(start_tag)
    j = html.find("</script>", i + len(start_tag)) if i >= 0 else -1
    if i < 0 or j < 0:
        return False
    text = json.dumps(payload, ensure_ascii=True, separators=(",", ":")).replace("</", "<\\/")
    html = html[: i + len(start_tag)] + text + html[j:]
    with open(html_path, "w", encoding="utf-8", newline="\n") as f:
        f.write(html)
    return True


def main():
    ap = argparse.ArgumentParser(description="Extract proving-ground measurements from ModernUO shard logs.")
    ap.add_argument("logs", help="folder holding session-YYYY-MM-DD_HH-MM.log files")
    ap.add_argument("--out", default=None, help="output folder (default: data/ next to this script)")
    ap.add_argument("--no-html", action="store_true", help="do not refresh the data block in index.html")
    ap.add_argument("--until", default=None,
                    help="ignore everything logged after this moment, 'YYYY-MM-DD HH:MM:SS' "
                         "(the write-up used '2026-09-26 15:30:00')")
    args = ap.parse_args()
    until = datetime.strptime(args.until, "%Y-%m-%d %H:%M:%S") if args.until else None

    here = os.path.dirname(os.path.abspath(__file__))
    out = args.out or os.path.join(here, "data")
    os.makedirs(out, exist_ok=True)

    if not os.path.isdir(args.logs):
        sys.exit(f"not a folder: {args.logs}")

    data = read_logs(args.logs, until)
    # Deaths only on the days the proving ground held fights: the rest of the month is another story.
    days = {f["date"] for f in data["fights"]}
    data["deaths"] = [d for d in data["deaths"] if d["date"] in days]
    data["sessions"] = [s for s in data["sessions"] if s["date"] in days]
    census = read_census(args.logs, data["sessions"], until)
    fights_by_id = {f["fight_id"]: f for f in data["fights"]}
    trace_rows = trace_fights(data["traces"], fights_by_id)
    bots = per_bot(data["fights"])
    summary = summarise(data, census, trace_rows)

    fight_cols = ["fight_id", "date", "time", "session", "build", "rings_awake", "world_awake", "r_cap", "bot", "class",
                  "fighting_skills", "stats", "armour", "weapon", "weapon_damage", "carries_bow", "held", "held_ammo",
                  "weapon_changes", "creature", "creature_might", "result", "called_reason", "seconds",
                  "creature_left_pct", "share_killed", "taken", "healed", "hits_max", "net_damage", "dealt", "spells",
                  "healing_spells", "bandages", "bottles", "mana_spent", "r", "proven_strength", "formula_power",
                  "proven_over_formula", "formula_implied_r", "steps", "shots", "paralysed_s", "traced_seconds"]
    write_csv(os.path.join(out, "fights.csv"), data["fights"], fight_cols)
    write_csv(os.path.join(out, "companies.csv"), data["rooms"],
              ["company_id", "date", "time", "session", "build", "rings_awake", "guild", "members", "company_size",
               "dungeon", "room", "room_creatures", "result", "seconds", "creatures_down", "room_health_taken_pct",
               "fell", "company_health_spent_pct"])
    write_csv(os.path.join(out, "traces.csv"), data["traces"],
              ["fight_id", "date", "time", "session", "bot", "t_s", "weapon", "ammo_left", "swing_in_ms", "still_ms",
               "tiles", "sees", "in_sight", "paralysed", "frozen", "combatant", "warmode", "hits", "creature",
               "creature_hits", "creature_hits_max", "creature_combatant", "creature_casting"])
    write_csv(os.path.join(out, "traced_fights.csv"), trace_rows,
              list(trace_rows[0].keys()) if trace_rows else ["fight_id"])
    write_csv(os.path.join(out, "ladder.csv"), data["ladders"],
              ["date", "time", "session", "build", "rung", "creature", "might", "dungeons"])
    write_csv(os.path.join(out, "summaries.csv"), data["summaries"],
              ["date", "time", "session", "build", "state", "fights", "won", "lost", "called", "on_field", "companies",
               "companies_cleared", "companies_failed", "cleared_lately", "unset", "by_hand", "faults", "swept",
               "bots_measured", "readings", "delve_reads", "asked_proven", "asked_neighbour", "asked_formula",
               "calibration", "parsed_lines_agree"])
    write_csv(os.path.join(out, "delve_offers_moved.csv"), data["moved"],
              ["date", "time", "session", "build", "leader", "band", "proven_strength", "formula_strength",
               "judged_against", "can_reach", "formula_said"])
    write_csv(os.path.join(out, "delve_judgements.csv"), data["judged"],
              ["session", "build", "date", "time", "offers_deeper", "offers_less_deep", "bands_kept_on_island",
               "parties_sent_elsewhere", "closed_by_weakest_member", "closed_by_room_gate"])
    write_csv(os.path.join(out, "deaths.csv"), data["deaths"],
              ["date", "time", "session", "build", "bot", "class", "x", "y", "z", "dungeon_side", "dungeon", "killer",
               "killer_might", "formula_power_at_death"])
    write_csv(os.path.join(out, "census.csv"), census,
              ["date", "time", "creatures_near_bots", "reach_tiles", "ai_on", "would_fight", "would_fight_ai_on",
               "in_dungeons_ai_on", "in_dungeons", "clients", "wake"])
    write_csv(os.path.join(out, "sessions.csv"), data["sessions"],
              ["session", "build", "basis", "first_line", "last_line", "world_wake_at_boot", "fights", "companies",
               "not_held", "traces"])
    write_csv(os.path.join(out, "bots.csv"), bots)
    awake = [f for f in data["fights"] if f["rings_awake"]]
    write_csv(os.path.join(out, "by_class_awake.csv"), group_table(awake, "class") if awake else [])
    write_csv(os.path.join(out, "by_creature_awake.csv"), group_table(awake, "creature") if awake else [])
    asleep = [f for f in data["fights"] if not f["rings_awake"]]
    write_csv(os.path.join(out, "by_creature_asleep.csv"), group_table(asleep, "creature") if asleep else [])
    with open(os.path.join(out, "summary.json"), "w", encoding="utf-8", newline="\n") as f:
        json.dump(summary, f, indent=1, ensure_ascii=True)
        f.write("\n")

    payload = page_payload(data, summary, trace_rows, bots)
    html_done = False if args.no_html else refresh_html(os.path.join(here, "index.html"), payload)

    print(f"fights {len(data['fights'])}, companies {len(data['rooms'])}, trace lines {len(data['traces'])}, "
          f"summaries {len(data['summaries'])}, ladder rows {len(data['ladders'])}, offers moved {len(data['moved'])}, "
          f"deaths {len(data['deaths'])}, census {len(census)}, unparsed proving lines {len(data['unparsed'])}")
    for u in data["unparsed"][:10]:
        print("  unparsed:", u)
    print(f"data written to {out}; index.html {'refreshed' if html_done else 'not refreshed'}")


if __name__ == "__main__":
    main()
