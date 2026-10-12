#!/usr/bin/env python3
"""Regenerate the pathfinding data of this folder from a folder of shard logs.

Usage:
    python extract.py LOGS_DIR [--out DATA_DIR] [--html INDEX_HTML | --no-html]

LOGS_DIR holds the shard's session logs, named session-YYYY-MM-DD_HH-MM.log, one per boot of the shard,
with lines of the form "[HH:MM:SS INF] text <s:Source>". The date of a line comes from the file name; a
session that runs past midnight is followed across it. LOGS_DIR may also hold bot-debugger-commands.log,
the record of questions asked of the shard through its console ("the door"); the routes asked there with
the `chart` and `nav` commands, and the benchmarks asked with `navbench`, are read from it.

Writes, into DATA_DIR (default: data/ next to this script):
    sessions.csv            one row per session: its span, the last "Getting about:" summary, the summary
                            nearest ten minutes of age, and walk failures counted by message shape
    summaries.csv           every "Getting about:" summary of every session (they are cumulative)
    roads.csv               every "Roads:" line (the road map drawn from home)
    chart.csv               every "Chart:" line (the coarse chart of build 262)
    navigation_graph.csv    every "Navigation:" line about the graph itself: drawing, drawn, regions built,
                            written to disk, read from disk
    navigation_summary.csv  every five-minute "Navigation: tiered navigation: ..." line
    door_routes.csv         routes asked through the door with `chart` and `nav`
    door_benchmarks.csv     benchmarks asked through the door with `navbench`
    builds.csv              the builds marked on the charts (from the decision log, not the logs)
    key_numbers.json        the numbers the README quotes, computed from the rows above

and refreshes the JSON embedded in index.html (next to this script) unless --no-html is given.

Standard library only. Nothing here depends on the machine it runs on.
"""

import argparse
import csv
import json
import os
import re
import sys
from datetime import datetime, timedelta

# ---------------------------------------------------------------------------------------------------------------------
# Line shapes

TS = re.compile(r"^\[(\d\d):(\d\d):(\d\d) [A-Z]{3}\] ")
SOURCE = re.compile(r"<s:([^>]+)>\s*$")
SESSION_NAME = re.compile(r"^session-(\d{4}-\d{2}-\d{2})_(\d{2})-(\d{2})\.log$")

# "Getting about:" is BotBeat's five-minute summary of the walker (BotPath.Describe and what follows it). Every
# counter in it is cumulative since the world loaded. Clauses were added over the weeks, so each is optional.
GETTING_ABOUT = [
    (re.compile(r"(\d+) searches, (\d+) tiles examined, (\d+)ms total \(([\d.]+)ms each, worst ([\d.]+)ms\), "
                r"(\d+) reached, (\d+) partial, (\d+) refused outright"),
     ["searches", "tiles_examined", "search_ms", "ms_each", "worst_ms", "reached", "partial", "refused_outright"]),
    (re.compile(r"(\d+) were handed less clock than they asked for and (\d+) asked for the whole ceiling"),
     ["handed_less_clock", "asked_whole_ceiling"]),
    (re.compile(r"of the partials (\d+) were going somewhere within (\d+) tiles and (\d+) ended no nearer than they "
                r"started(?: and (\d+) never began[^,]*)?, the average one (\d+) tiles out"),
     ["partial_within_near", "near_tiles", "partial_no_nearer", "partial_never_began", "partial_avg_tiles_out"]),
    (re.compile(r"proofs of a pocket lost: (\d+) to the clock, (\d+) to the box"),
     ["pocket_proofs_lost_to_clock", "pocket_proofs_lost_to_box"]),
    (re.compile(r"(\d+) looks at the far side costing (\d+)ms"),
     ["far_side_looks", "far_side_ms"]),
    (re.compile(r"(\d+) steps taken, (\d+) refused by the engine, (\d+) doors opened, (\d+) tiles gone round, "
                r"(\d+) improvised, (\d+) journeys given up, (\d+) destinations dropped as no good"),
     ["steps_taken", "steps_refused_by_engine", "doors_opened", "tiles_gone_round", "steps_improvised",
      "journeys_given_up", "destinations_dropped"]),
    (re.compile(r"\((\d+) station searches held to"),
     ["station_searches_held_short"]),
    (re.compile(r"(\d+) searches at the stranded ceiling for a way round, (\d+) found one and (\d+) ended the errand"),
     ["stranded_searches", "stranded_found", "stranded_ended_errand"]),
    (re.compile(r"(\d+) plans drawn to a leg of (?:the chart's|a) route and (\d+) legs that could not be walked"),
     ["leg_plans", "legs_failed"]),
    (re.compile(r"routes drawn: (\d+) over the navigation graph, (\d+) over the old chart[^,]*, "
                r"(\d+) with no way on the ground"),
     ["routes_over_graph", "routes_over_chart", "routes_no_way"]),
    (re.compile(r"with no way on the ground \((\d+) errands dropped"),
     ["no_way_errands_dropped"]),
    (re.compile(r"with no way on the ground \((\d+) plans the graph said had none"),
     ["no_way_plans_walked_straight"]),
    (re.compile(r"(\d+) journeys refused without a search"),
     ["journeys_refused_without_search"]),
]

SUMMARY_FIELDS = [name for _, names in GETTING_ABOUT for name in names]

# Two ranked lists in the same line, kept as text: what burned the whole search ceiling, and what was lost.
BURNED_BY = re.compile(r"the whole ceiling was burned by: (.*?) \(\d+ station searches held to")
LOST_BY = re.compile(r"errands lost as hopeless or without a way round, by kind: (.*?);")
RANKED_ITEM = re.compile(r"(.+?) (\d+)(?:, |$)")

ROADS = re.compile(r"Roads: (\d+) tiles reached from home at \((\d+), (\d+), (-?\d+)\) within (\d+) tiles each way, "
                   r"the farthest (\d+) steps of road; (\d+) slices, (\d+)ms of the loop over ([\d.]+)s")
ROADS_FIELDS = ["tiles", "home_x", "home_y", "home_z", "reach", "farthest_steps", "slices", "loop_ms", "seconds"]

CHART = re.compile(r"Chart: (\d+) nodes and (\d+) gates on (\d+) squares of (\d+) tiles, (\d+) ways between them; "
                   r"(\d+) slices, (\d+)ms of the loop over ([\d.]+)s")
CHART_FIELDS = ["nodes", "gates", "squares", "side", "ways", "slices", "loop_ms", "seconds"]

NAV_DRAWING = re.compile(r"Navigation: drawing the graph of (\w+), (\d+) clusters of (\d+) tiles, "
                         r"outward from \((\d+), (\d+), (-?\d+)\)")
NAV_DRAWN = re.compile(r"Navigation: the graph of (\w+) is drawn: (\d+) of (\d+) clusters built \(([^)]*)\), (\d+) nodes, "
                       r"(\d+) gates, (\d+) edges, (\d+) components, (\d+) cells probed; (\d+)ms of the loop over (\d+)s")
NAV_REGIONS = re.compile(r"Navigation: the regions of (\w+) are built: (\d+) regions of (\d+) tiles")
NAV_WRITTEN = re.compile(r"Navigation: the graph of (\w+) was written to disk in (\d+)ms")
NAV_READ = re.compile(r"Navigation: the graph of (\w+) was read from disk: (\d+) of (\d+) clusters built \(([^)]*)\), "
                      r"(\d+) nodes, (\d+) gates, (\d+) edges, (?:(\d+) components|components not counted), "
                      r"(\d+) cells probed; (\d+) clusters by houses drawn again; (\d+)ms of the loop to put it in place")
NAV_UNREAD = re.compile(r"Navigation: the graph of (\w+) on disk could not be read")
NAV_SUMMARY = re.compile(r"Navigation: tiered navigation: (\w+): (\d+) of (\d+) clusters built \(([^)]*)\), (\d+) nodes, "
                         r"(\d+) gates, (\d+) edges, (?:(\d+) components|components not counted), (\d+) cells probed, "
                         r"(\d+)ms of the loop; (\d+) routes asked in (\d+)ms \(worst ([\d.]+)ms, (\d+) nodes expanded\): "
                         r"(\d+) routed, (\d+) direct, (\d+) unreachable, (\d+) not drawn yet, (\d+) over budget, "
                         r"(\d+) unplaced; windows (\d+) kept and (\d+) probed; (\d+) gates shunned")
NAV_SUMMARY_FIELDS = ["map", "clusters_built", "clusters", "state", "nodes", "gates", "edges", "components",
                      "cells_probed", "draw_loop_ms", "routes_asked", "routes_ms", "worst_route_ms", "nodes_expanded",
                      "routed", "direct", "unreachable", "not_drawn_yet", "over_budget", "unplaced", "windows_kept",
                      "windows_probed", "gates_shunned"]
NAV_LONG_PART = re.compile(r"the long tier took (\d+) routes over budget \((\d+) region nodes expanded, "
                           r"(\d+) regions built\)")
NAV_CHART_PART = re.compile(r"the chart: .*?; (\d+) routes asked, (\d+) found, (\d+) joined inside one square, "
                            r"(\d+) with no way between, (\d+) off the chart")
NAV_EXTRA_FIELDS = ["long_routes", "long_nodes_expanded", "long_regions_built", "chart_asked", "chart_found",
                    "chart_joined_inside", "chart_no_way", "chart_off"]

# Walk failures, counted by the shape of the message and only from the two sources that write them: the walker
# (BotWalk, "... has dropped (x, y, z) because ...") and the will (BotWill, "... failed at <work>: ... — <why>").
# One event can write a line in both, so the columns overlap and must not be added together. The last shape is not
# a failure: from build 265 the walker logs, on every plan, that the graph said "no way" and it planned straight.
FAILURE_SHAPES = [
    ("no_way_round", "no way round it was found", "BotWalk"),
    ("not_one_tile_closer", "could not get one tile closer", "BotWalk"),
    ("has_dropped", "has dropped", "BotWalk"),
    ("no_way_through", "no way through to", "BotWill"),
    ("stopped_closing", "the walk stopped closing", "BotWill"),
    ("ground_no_way", "the ground has no way there", "BotWalk"),
    ("graph_says_no_way", "the navigation graph says there is no way", "BotWalk"),
]
GRAPH_NO_WAY = re.compile(r"^(.+?): the navigation graph says there is no way from \(([^)]*)\) to \(([^)]*)\)")

# Why the walker dropped a destination: the reason after "because", or the "could not get one tile closer" form.
# The first two are the bounded search giving up; the rest are answers proved about the ground.
DROP_REASONS = [
    ("drop_no_way_round", "no way round it was found", "gave_up"),
    ("drop_not_one_tile_closer", "could not get one tile closer", "gave_up"),
    ("drop_no_way_from_here", "there is no way from here", "proven"),
    ("drop_nowhere_to_stand", "there is nowhere there to stand", "proven"),
    ("drop_shut_in", "it is shut in and this bot is outside it", "proven"),
    ("drop_ground_no_way", "the ground has no way there", "proven"),
]

# The first minutes of a session, for comparing sessions at the same age (the decision log's rule O2).
EARLY_MIN = 10.0

# The door's answers to `chart x1 y1 x2 y2` (build 262 on) and `nav x1 y1 x2 y2` (build 264 on).
DOOR_COMMAND = re.compile(r"^\[(\d\d):(\d\d):(\d\d)\] the console: (chart|nav) (-?\d+) (-?\d+) (-?\d+) (-?\d+)\s*$")
DOOR_CHART = re.compile(r"-> from \((\d+), (\d+), (-?\d+)\) to \((\d+), (\d+), (-?\d+)\): (\d+) points, about (\d+) steps "
                        r"over the chart for (\d+) straight, (\d+) nodes expanded in ([\d.]+)ms")
DOOR_NAV = re.compile(r"-> from \((\d+), (\d+), (-?\d+)\) to \((\d+), (\d+), (-?\d+)\): (\d+) points, cost (\d+) "
                      r"\((\d+) steps\) for (\d+) straight, (\d+) nodes expanded in ([\d.]+)ms")
DOOR_BOOT = re.compile(r"^\[(\d\d):(\d\d):(\d\d)\] Argus has hands from this moment")
# The door's `navbench n radius checked` (build 267 on): random routes over the graph on the live map.
DOOR_BENCH_COMMAND = re.compile(r"^\[(\d\d):(\d\d):(\d\d)\] the console: navbench (\d+) (\d+) (\d+)\s*$")
DOOR_BENCH = re.compile(r"-> (\d+) random routes within (\d+) of home: (.*?); (\d+) handed to the long tier; "
                        r"([\d.]+)ms a route on average, ([\d.]+)ms at the 95th percentile, ([\d.]+)ms at worst; "
                        r"(\d+) medium nodes expanded in all; of (\d+) routes walked leg by leg, (\d+) legs and (\d+) the "
                        r"precise search could not walk; (\d+) also planned end to end by the precise search, the route "
                        r"(-?[\d.]+)% longer on average and (-?[\d.]+)% at the 95th percentile")
BENCH_FIELDS = ["session", "time", "routes", "radius", "ok", "direct", "unreachable", "pending", "over_budget",
                "unplaced", "handed_to_long", "mean_ms", "p95_ms", "worst_ms", "medium_nodes_expanded",
                "routes_walked", "legs_walked", "legs_failed", "compared_end_to_end", "longer_mean_pct",
                "longer_p95_pct"]
DOOR_FIRST_POINTS = re.compile(r"first points (.*?)(?: …)?; legs:")
DOOR_LEG = re.compile(r"to \((-?\d+),(-?\d+),(-?\d+)\) (\w+) in ([\d.]+)ms")

# The builds marked on the charts. From the decision log (Projects/BotAIv2/DECISIONS.md), not from the logs: a
# build is not named anywhere in a session log; a deploy starts a new session file.
BUILDS = [
    {"build": 262, "session": "2026-09-26_14-25", "label": "262: chart in shadow",
     "what": "coarse chart (BotChart) drawn at boot and asked through the door; walking unchanged"},
    {"build": 263, "session": "2026-09-26_14-32", "label": "263: walks on the chart",
     "what": "far walks go over the chart a leg at a time; progress measured along the route; work priced by road"},
    {"build": 264, "session": "2026-09-26_15-18", "label": "264: tiered navigation",
     "what": "engine graph over all of Felucca (clusters of 16, strata, components); bots route over it"},
    {"build": 265, "session": "2026-09-26_15-30", "label": "265: long tier",
     "what": "regions of 128 tiles and a corridor over the graph; the graph's 'no way' logged, not acted on"},
    {"build": 266, "session": "2026-09-26_15-37", "label": "266: route to within reach",
     "what": "a route ends anywhere within the errand's arrival distance of its goal"},
    {"build": 267, "session": "2026-09-26_15-42", "label": "267: graph on disk",
     "what": "the graph written to disk and read back at boot; the console's navbench"},
    {"build": 268, "session": "2026-09-26_15-55", "label": "268: read from disk",
     "what": "the first boot that read the graph back from disk; the medium tier's ties lean towards the goal"},
]

# The session the README calls "before": the last full session of the old walker with the world awake.
BEFORE = "2026-09-26_13-16"


# ---------------------------------------------------------------------------------------------------------------------
# Helpers

def to_number(text):
    if text is None:
        return None
    return float(text) if "." in text else int(text)


def parse_fields(pattern_fields, body):
    row = {}
    for pattern, names in pattern_fields:
        m = pattern.search(body)
        for i, name in enumerate(names):
            row[name] = to_number(m.group(i + 1)) if m else None
    return row


def ranked(pattern, body):
    m = pattern.search(body)
    if not m or m.group(1).strip() in ("", "nobody"):
        return ""
    return "; ".join(f"{name}={count}" for name, count in RANKED_ITEM.findall(m.group(1)))


def fmt(value):
    if value is None:
        return ""
    if isinstance(value, float):
        return f"{value:.4f}".rstrip("0").rstrip(".")
    return str(value)


def write_csv(path, rows, fields):
    with open(path, "w", encoding="utf-8", newline="") as fh:
        w = csv.writer(fh, lineterminator="\n")
        w.writerow(fields)
        for row in rows:
            w.writerow([fmt(row.get(f)) for f in fields])


def ratio(num, den, scale=1.0, digits=2):
    if num is None or den in (None, 0):
        return None
    return round(scale * num / den, digits)


def minutes_between(a, b):
    return (b - a).total_seconds() / 60.0


# ---------------------------------------------------------------------------------------------------------------------
# One session file

def read_session(path, name):
    m = SESSION_NAME.match(name)
    date = datetime.strptime(m.group(1), "%Y-%m-%d")
    named = timedelta(hours=int(m.group(2)), minutes=int(m.group(3)))
    session = f"{m.group(1)}_{m.group(2)}-{m.group(3)}"

    first = last = None
    prev_tod = None
    day = 0
    summaries, roads, charts, navs, nav_summaries, no_way = [], [], [], [], [], []
    failures = {key: 0 for key, _, _ in FAILURE_SHAPES}
    drops = {key: 0 for key, _, _ in DROP_REASONS}
    drops["drop_other"] = 0
    early = {"gave_up": 0, "proven": 0, "has_dropped": 0}

    with open(path, encoding="utf-8", errors="replace") as fh:
        for line in fh:
            t = TS.match(line)
            if not t:
                continue
            tod = timedelta(hours=int(t.group(1)), minutes=int(t.group(2)), seconds=int(t.group(3)))
            if prev_tod is None:
                # A file named late in the evening whose first line is written after midnight belongs to the next day.
                if tod < named - timedelta(hours=12):
                    day = 1
            elif tod < prev_tod - timedelta(hours=12):
                day += 1
            prev_tod = tod
            at = date + timedelta(days=day) + tod
            if first is None:
                first = at
            last = at

            text = line[t.end():]
            s = SOURCE.search(text)
            source = s.group(1) if s else ""
            body = text[:s.start()].rstrip() if s else text.rstrip()

            if "Getting about:" in body and source.endswith("BotBeat"):
                tail = body.split("Getting about:", 1)[1]
                row = parse_fields(GETTING_ABOUT, tail)
                if row["searches"] is not None:
                    row["ceiling_burned_by"] = ranked(BURNED_BY, tail)
                    row["errands_lost_by_kind"] = ranked(LOST_BY, tail)
                    row["time"] = at
                    summaries.append(row)
                continue

            if body.startswith("Roads:") and source.endswith("BotRoads"):
                r = ROADS.search(body)
                if r:
                    row = {k: to_number(v) for k, v in zip(ROADS_FIELDS, r.groups())}
                    row["time"] = at
                    roads.append(row)
                continue

            if body.startswith("Chart:") and source.endswith("BotChart"):
                r = CHART.search(body)
                if r:
                    row = {k: to_number(v) for k, v in zip(CHART_FIELDS, r.groups())}
                    row["time"] = at
                    charts.append(row)
                continue

            if body.startswith("Navigation:"):
                r = NAV_DRAWING.search(body)
                if r:
                    navs.append({"time": at, "event": "drawing", "map": r.group(1), "clusters": int(r.group(2)),
                                 "cluster_side": int(r.group(3)),
                                 "from": f"({r.group(4)}, {r.group(5)}, {r.group(6)})"})
                    continue
                r = NAV_DRAWN.search(body)
                if r:
                    navs.append({"time": at, "event": "drawn", "map": r.group(1), "clusters_built": int(r.group(2)),
                                 "clusters": int(r.group(3)), "state": r.group(4), "nodes": int(r.group(5)),
                                 "gates": int(r.group(6)), "edges": int(r.group(7)), "components": int(r.group(8)),
                                 "cells_probed": int(r.group(9)), "loop_ms": int(r.group(10)),
                                 "seconds": int(r.group(11))})
                    continue
                r = NAV_REGIONS.search(body)
                if r:
                    navs.append({"time": at, "event": "regions", "map": r.group(1), "regions": int(r.group(2)),
                                 "region_side": int(r.group(3))})
                    continue
                r = NAV_WRITTEN.search(body)
                if r:
                    navs.append({"time": at, "event": "written", "map": r.group(1), "loop_ms": None,
                                 "file_ms": int(r.group(2))})
                    continue
                r = NAV_READ.search(body)
                if r:
                    navs.append({"time": at, "event": "read", "map": r.group(1), "clusters_built": int(r.group(2)),
                                 "clusters": int(r.group(3)), "state": r.group(4), "nodes": int(r.group(5)),
                                 "gates": int(r.group(6)), "edges": int(r.group(7)),
                                 "components": to_number(r.group(8)), "cells_probed": int(r.group(9)),
                                 "rebuilt_by_houses": int(r.group(10)), "loop_ms": int(r.group(11))})
                    continue
                r = NAV_UNREAD.search(body)
                if r:
                    navs.append({"time": at, "event": "unreadable", "map": r.group(1)})
                    continue
                r = NAV_SUMMARY.search(body)
                if r:
                    row = {}
                    for k, v in zip(NAV_SUMMARY_FIELDS, r.groups()):
                        row[k] = v if k in ("map", "state") else to_number(v)
                    extra = NAV_LONG_PART.search(body)
                    chart = NAV_CHART_PART.search(body)
                    values = (list(extra.groups()) if extra else [None] * 3) + \
                             (list(chart.groups()) if chart else [None] * 5)
                    for k, v in zip(NAV_EXTRA_FIELDS, values):
                        row[k] = to_number(v)
                    row["time"] = at
                    nav_summaries.append(row)
                continue

            if source.endswith("BotWalk") or source.endswith("BotWill"):
                short = "BotWalk" if source.endswith("BotWalk") else "BotWill"
                for key, phrase, src in FAILURE_SHAPES:
                    if src == short and phrase in body:
                        failures[key] += 1
                if short == "BotWalk":
                    g = GRAPH_NO_WAY.search(body)
                    if g:
                        no_way.append({"time": at, "bot": g.group(1), "from": g.group(2), "to": g.group(3)})
                    if "has dropped" in body:
                        is_early = minutes_between(first, at) <= EARLY_MIN
                        for key, phrase, kind in DROP_REASONS:
                            if phrase in body:
                                drops[key] += 1
                                if is_early:
                                    early[kind] += 1
                                break
                        else:
                            drops["drop_other"] += 1
                        if is_early:
                            early["has_dropped"] += 1

    return {
        "session": session, "file": name, "date": m.group(1), "first": first, "last": last,
        "summaries": summaries, "roads": roads, "charts": charts, "navs": navs, "nav_summaries": nav_summaries,
        "failures": failures, "drops": drops, "early": early, "no_way": no_way,
    }


# ---------------------------------------------------------------------------------------------------------------------
# The door's record of routes asked

def read_door(path, sessions):
    """Routes asked with `chart` and `nav`, and benchmarks asked with `navbench`. The door's file has times but no
    dates: each boot writes a banner, and the banner is matched to the session file that began up to five minutes
    before it, in order."""
    if not os.path.exists(path):
        return [], []

    starts = [(s["session"], s["first"]) for s in sessions if s["first"] is not None]
    pointer = 0
    current = None
    rows = []
    bench = []
    pending = None
    bench_pending = None

    with open(path, encoding="utf-8", errors="replace") as fh:
        for line in fh:
            b = DOOR_BOOT.match(line)
            if b:
                tod = timedelta(hours=int(b.group(1)), minutes=int(b.group(2)), seconds=int(b.group(3)))
                match = None
                for i in range(pointer, len(starts)):
                    name, start = starts[i]
                    start_tod = timedelta(hours=start.hour, minutes=start.minute, seconds=start.second)
                    if 0 <= (tod - start_tod).total_seconds() <= 300:
                        match = i
                        break
                if match is not None:
                    pointer = match + 1
                    current = starts[match]
                else:
                    current = None
                pending = None
                bench_pending = None
                continue

            c = DOOR_COMMAND.match(line)
            if c:
                pending = {"tool": c.group(4), "asked": ":".join(c.group(1, 2, 3))}
                bench_pending = None
                continue

            c = DOOR_BENCH_COMMAND.match(line)
            if c:
                bench_pending = ":".join(c.group(1, 2, 3))
                pending = None
                continue

            if bench_pending and "-> " in line:
                r = DOOR_BENCH.search(line)
                if r:
                    g = r.groups()
                    statuses = dict((k.lower(), int(v)) for k, v in re.findall(r"(\w+) (\d+)", g[2]))
                    bench.append({
                        "session": current[0] if current else "", "time": bench_pending,
                        "routes": int(g[0]), "radius": int(g[1]),
                        "ok": statuses.get("ok", 0), "direct": statuses.get("direct", 0),
                        "unreachable": statuses.get("unreachable", 0), "pending": statuses.get("pending", 0),
                        "over_budget": statuses.get("budgetexceeded", 0), "unplaced": statuses.get("unplaced", 0),
                        "handed_to_long": int(g[3]), "mean_ms": float(g[4]), "p95_ms": float(g[5]),
                        "worst_ms": float(g[6]), "medium_nodes_expanded": int(g[7]), "routes_walked": int(g[8]),
                        "legs_walked": int(g[9]), "legs_failed": int(g[10]), "compared_end_to_end": int(g[11]),
                        "longer_mean_pct": float(g[12]), "longer_p95_pct": float(g[13]),
                    })
                bench_pending = None
                continue

            if pending and "-> from (" in line:
                tool = pending["tool"]
                r = (DOOR_CHART if tool == "chart" else DOOR_NAV).search(line)
                if r:
                    g = r.groups()
                    row = {
                        "session": current[0] if current else "",
                        "time": pending["asked"],
                        "tool": tool,
                        "from": f"({g[0]}, {g[1]}, {g[2]})",
                        "to": f"({g[3]}, {g[4]}, {g[5]})",
                        "points": int(g[6]),
                    }
                    if tool == "chart":
                        row.update({"steps": int(g[7]), "steps_exact": "about", "cost": None,
                                    "straight": int(g[8]), "nodes_expanded": int(g[9]), "ms": float(g[10])})
                    else:
                        row.update({"steps": int(g[8]), "steps_exact": "yes", "cost": int(g[7]),
                                    "straight": int(g[9]), "nodes_expanded": int(g[10]), "ms": float(g[11])})
                    fp = DOOR_FIRST_POINTS.search(line)
                    pts = re.findall(r"\((-?\d+),(-?\d+)(?:,(-?\d+))?\)", fp.group(1)) if fp else []
                    row["first_point"] = f"({pts[0][0]}, {pts[0][1]})" if pts else ""
                    row["point_12"] = f"({pts[11][0]}, {pts[11][1]})" if len(pts) >= 12 else ""
                    legs = DOOR_LEG.findall(line.split("legs:", 1)[1]) if "legs:" in line else []
                    row["legs_tried"] = len(legs)
                    row["legs_reached"] = sum(1 for leg in legs if leg[3] == "Reached")
                    row["legs_worst_ms"] = max((float(leg[4]) for leg in legs), default=None)
                    rows.append(row)
                pending = None
    return rows, bench


# ---------------------------------------------------------------------------------------------------------------------
# Assembly

def nearest(summaries, first, minutes, slack=1.5):
    best = None
    for row in summaries:
        age = minutes_between(first, row["time"])
        if abs(age - minutes) <= slack and (best is None or abs(age - minutes) < abs(best[0] - minutes)):
            best = (age, row)
    return best


def session_row(s):
    length = minutes_between(s["first"], s["last"])
    row = {"session": s["session"], "date": s["date"], "start": s["first"].isoformat(), "end": s["last"].isoformat(),
           "length_min": round(length, 2), "summaries": len(s["summaries"])}
    row["build"] = next((b["build"] for b in BUILDS if b["session"] == s["session"]), None)

    if s["summaries"]:
        last = s["summaries"][-1]
        age = minutes_between(s["first"], last["time"])
        row["summary_time"] = last["time"].isoformat()
        row["summary_age_min"] = round(age, 2)
        for f in SUMMARY_FIELDS:
            row[f] = last.get(f)
        row["search_s_per_min"] = ratio(last["search_ms"], age * 1000.0, digits=3)
        row["loop_share_pct"] = ratio(last["search_ms"], age * 60000.0, 100.0)
        row["partial_share_pct"] = ratio(last["partial"], last["searches"], 100.0)
        row["no_nearer_share_pct"] = ratio(last["partial_no_nearer"], last["searches"], 100.0)
        row["whole_ceiling_share_pct"] = ratio(last["asked_whole_ceiling"], last["searches"], 100.0)
        row["ceiling_burned_by"] = last["ceiling_burned_by"]
        row["errands_lost_by_kind"] = last["errands_lost_by_kind"]
        ten = nearest(s["summaries"], s["first"], EARLY_MIN)
        if ten:
            age10, r10 = ten
            row["s10_age_min"] = round(age10, 2)
            row["s10_searches"] = r10["searches"]
            row["s10_search_ms"] = r10["search_ms"]
            row["s10_partial"] = r10["partial"]
            row["s10_no_nearer"] = r10["partial_no_nearer"]
            row["s10_search_s_per_min"] = ratio(r10["search_ms"], age10 * 1000.0, digits=3)
            row["s10_partial_share_pct"] = ratio(r10["partial"], r10["searches"], 100.0)

    hours = length / 60.0
    per_h = (lambda n: ratio(n, hours)) if length >= 1.0 else (lambda n: None)
    for key in s["failures"]:
        row["f_" + key] = s["failures"][key]
        row["f_" + key + "_per_h"] = per_h(s["failures"][key])
    for key in s["drops"]:
        row[key] = s["drops"][key]
    gave_up = sum(s["drops"][k] for k, _, kind in DROP_REASONS if kind == "gave_up")
    proven = sum(s["drops"][k] for k, _, kind in DROP_REASONS if kind == "proven")
    row["drops_gave_up"] = gave_up
    row["drops_gave_up_per_h"] = per_h(gave_up)
    row["drops_proven"] = proven
    row["drops_proven_per_h"] = per_h(proven)
    if length >= EARLY_MIN:
        early_h = EARLY_MIN / 60.0
        row["e10_has_dropped_per_h"] = ratio(s["early"]["has_dropped"], early_h)
        row["e10_drops_gave_up_per_h"] = ratio(s["early"]["gave_up"], early_h)
        row["e10_drops_proven_per_h"] = ratio(s["early"]["proven"], early_h)
    return row


SESSION_FIELDS = (
    ["session", "date", "start", "end", "length_min", "build", "summaries", "summary_time", "summary_age_min"]
    + SUMMARY_FIELDS
    + ["search_s_per_min", "loop_share_pct", "partial_share_pct", "no_nearer_share_pct", "whole_ceiling_share_pct",
       "s10_age_min", "s10_searches", "s10_search_ms", "s10_partial", "s10_no_nearer", "s10_search_s_per_min",
       "s10_partial_share_pct"]
    + [x for key, _, _ in FAILURE_SHAPES for x in ("f_" + key, "f_" + key + "_per_h")]
    + [key for key, _, _ in DROP_REASONS] + ["drop_other"]
    + ["drops_gave_up", "drops_gave_up_per_h", "drops_proven", "drops_proven_per_h",
       "e10_has_dropped_per_h", "e10_drops_gave_up_per_h", "e10_drops_proven_per_h",
       "ceiling_burned_by", "errands_lost_by_kind"]
)


def summary_at(sessions_by_name, name, minutes=None):
    """The summary of a session nearest an age in minutes, or its last one; with its age and derived shares."""
    s = sessions_by_name.get(name)
    if not s or not s["summaries"]:
        return None
    if minutes is None:
        row = s["summaries"][-1]
        age = minutes_between(s["first"], row["time"])
    else:
        hit = nearest(s["summaries"], s["first"], minutes)
        if not hit:
            return None
        age, row = hit
    out = {k: row[k] for k in SUMMARY_FIELDS if row.get(k) is not None}
    out["ceiling_burned_by"] = row["ceiling_burned_by"]
    out["session"] = name
    out["time"] = row["time"].isoformat()
    out["age_min"] = round(age, 2)
    out["search_s"] = round(row["search_ms"] / 1000.0, 1)
    out["search_s_per_min"] = ratio(row["search_ms"], age * 1000.0, digits=2)
    out["loop_share_pct"] = ratio(row["search_ms"], age * 60000.0, 100.0, 1)
    out["partial_share_pct"] = ratio(row["partial"], row["searches"], 100.0, 1)
    out["no_nearer_share_pct"] = ratio(row["partial_no_nearer"], row["searches"], 100.0, 1)
    out["asked_whole_ceiling_share_pct"] = ratio(row["asked_whole_ceiling"], row["searches"], 100.0, 1)
    out["tiles_per_search"] = ratio(row["tiles_examined"], row["searches"], digits=0)
    out["us_per_tile"] = ratio(row["search_ms"] * 1000.0, row["tiles_examined"], digits=3)
    return out


def key_numbers(sessions, rows, door, bench, extracted_at):
    by_name = {s["session"]: s for s in sessions}
    row_by_name = {r["session"]: r for r in rows}
    k = {"extracted_from_logs_up_to": extracted_at,
         "note": "a session still running when the logs were read has a 'last' that is a snapshot; the figures "
                 "at a fixed age (at_5_min, at_10_min, at_45_min) do not change once that age is logged"}

    k["before_tiers"] = {
        "session": BEFORE,
        "at_5_min": summary_at(by_name, BEFORE, 5.0),
        "at_10_min": summary_at(by_name, BEFORE, 10.0),
        "at_45_min": summary_at(by_name, BEFORE, 45.0),
        "last": summary_at(by_name, BEFORE),
    }
    for b in BUILDS:
        name = b["session"]
        k[f"build_{b['build']}"] = {
            "session": name,
            "at_5_min": summary_at(by_name, name, 5.0),
            "at_10_min": summary_at(by_name, name, 10.0),
            "at_45_min": summary_at(by_name, name, 45.0),
            "last": summary_at(by_name, name),
        }

    def rates(name):
        r = row_by_name.get(name)
        if not r:
            return None
        out = {"length_min": r["length_min"]}
        for key, _, _ in FAILURE_SHAPES:
            out[key] = r.get("f_" + key)
            out[key + "_per_h"] = r.get("f_" + key + "_per_h")
        for key in ("drops_gave_up", "drops_gave_up_per_h", "drops_proven", "drops_proven_per_h",
                    "e10_has_dropped_per_h", "e10_drops_gave_up_per_h", "e10_drops_proven_per_h"):
            out[key] = r.get(key)
        return out

    k["failures"] = {name: rates(name) for name in [BEFORE] + [b["session"] for b in BUILDS]}

    # The nine days of logs before the chart: every session's last summary covers that session.
    cut = BUILDS[0]["session"]
    earlier_all = [r for r in rows if r["session"] < cut]
    earlier = [r for r in earlier_all if r.get("searches") is not None]
    minutes = sum(r["summary_age_min"] for r in earlier)
    searches = sum(r["searches"] for r in earlier)
    k["logs_before_the_chart"] = {
        "first_session": rows[0]["session"] if rows else None,
        "last_session": earlier_all[-1]["session"] if earlier_all else None,
        "sessions": len(earlier_all),
        "sessions_with_a_summary": len(earlier),
        "hours_logged": round(sum(r["length_min"] for r in earlier_all) / 60.0, 1),
        "summarised_hours": round(minutes / 60.0, 1),
        "searches": searches,
        "tiles_examined": sum(r["tiles_examined"] for r in earlier),
        "search_s": round(sum(r["search_ms"] for r in earlier) / 1000.0),
        "loop_share_pct": ratio(sum(r["search_ms"] for r in earlier), minutes * 60000.0, 100.0, 1),
        "partial_share_pct": ratio(sum(r["partial"] for r in earlier), searches, 100.0, 1),
        "no_nearer_share_pct": ratio(sum(r["partial_no_nearer"] for r in earlier), searches, 100.0, 1),
        "asked_whole_ceiling": sum(r["asked_whole_ceiling"] for r in earlier),
        "stranded_searches": sum(r["stranded_searches"] or 0 for r in earlier),
        "stranded_found": sum(r["stranded_found"] or 0 for r in earlier),
        "walker_drops": sum(r["f_has_dropped"] for r in earlier_all),
        "walker_drops_gave_up": sum(r["drops_gave_up"] for r in earlier_all),
        "walker_drops_proven": sum(r["drops_proven"] for r in earlier_all),
        "no_way_round": sum(r["f_no_way_round"] for r in earlier_all),
        "not_one_tile_closer": sum(r["f_not_one_tile_closer"] for r in earlier_all),
        "no_way_through": sum(r["f_no_way_through"] for r in earlier_all),
        "stopped_closing": sum(r["f_stopped_closing"] for r in earlier_all),
        "sessions_over_10_min": len([r for r in earlier if r["summary_age_min"] >= 10]),
        "sessions_over_10_min_with_loop_share_over_10_pct": len(
            [r for r in earlier if r["summary_age_min"] >= 10 and (r["loop_share_pct"] or 0) > 10]),
        "highest_loop_share_session_over_10_min": max(
            ([r["loop_share_pct"], r["session"]] for r in earlier if r["summary_age_min"] >= 10), default=None),
        "highest_worst_ms": max(([r["worst_ms"], r["session"]] for r in earlier), default=None),
        "drop_other": sum(r["drop_other"] for r in earlier_all),
    }

    roads = [r for s in sessions for r in s["roads"]]
    if roads:
        k["roads"] = {
            "lines": len(roads),
            "tiles_min": min(r["tiles"] for r in roads), "tiles_max": max(r["tiles"] for r in roads),
            "farthest_steps_min": min(r["farthest_steps"] for r in roads),
            "farthest_steps_max": max(r["farthest_steps"] for r in roads),
            "loop_ms_min": min(r["loop_ms"] for r in roads), "loop_ms_max": max(r["loop_ms"] for r in roads),
            "seconds_min": min(r["seconds"] for r in roads), "seconds_max": max(r["seconds"] for r in roads),
            "reach": sorted({r["reach"] for r in roads}),
        }

    def dated(items):
        return [{kk: (v.isoformat() if isinstance(v, datetime) else v) for kk, v in r.items()} for r in items]

    k["chart"] = dated([dict(r, session=s["session"]) for s in sessions for r in s["charts"]])
    charts = [r for s in sessions for r in s["charts"]]
    if charts:
        k["chart_range"] = {
            "boots": len(charts),
            "nodes": sorted({r["nodes"] for r in charts}), "gates": sorted({r["gates"] for r in charts}),
            "ways": sorted({r["ways"] for r in charts}),
            "loop_ms_min": min(r["loop_ms"] for r in charts), "loop_ms_max": max(r["loop_ms"] for r in charts),
            "seconds_min": min(r["seconds"] for r in charts), "seconds_max": max(r["seconds"] for r in charts),
        }
    k["graph"] = dated([dict(r, session=s["session"]) for s in sessions for r in s["navs"] if r["event"] != "drawing"])
    k["navigation_summaries"] = dated([dict(r, session=s["session"]) for s in sessions for r in s["nav_summaries"]])

    no_way = {}
    for s in sessions:
        if not s["no_way"]:
            continue
        drawn = next((r["time"] for r in s["navs"] if r["event"] == "drawn"), None)
        pairs = {(x["bot"], x["to"]) for x in s["no_way"]}
        no_way[s["session"]] = {
            "lines": len(s["no_way"]),
            "bot_target_pairs": len(pairs),
            "before_the_graph_was_drawn": len([x for x in s["no_way"] if drawn is None or x["time"] < drawn]),
            "graph_drawn_at": drawn.isoformat() if drawn else None,
            "first": s["no_way"][0]["time"].isoformat(),
        }
    k["graph_said_no_way"] = no_way
    k["door_routes"] = door
    k["door_benchmarks"] = bench
    return k


def to_rows(sessions, key):
    out = []
    for s in sessions:
        for r in s[key]:
            row = dict(r)
            row["session"] = s["session"]
            row["age_min"] = round(minutes_between(s["first"], r["time"]), 2)
            row["time"] = r["time"].isoformat()
            out.append(row)
    return out


def viz_payload(rows, sessions, k):
    """The compact data the page draws from."""
    keep = ["session", "start", "length_min", "build", "summary_age_min", "searches", "search_ms", "partial",
            "partial_no_nearer", "asked_whole_ceiling", "worst_ms", "search_s_per_min", "loop_share_pct",
            "partial_share_pct", "no_nearer_share_pct", "s10_age_min", "s10_searches", "s10_search_ms", "s10_partial",
            "s10_search_s_per_min", "s10_partial_share_pct", "f_has_dropped", "f_has_dropped_per_h",
            "f_no_way_round", "f_not_one_tile_closer", "f_no_way_through", "f_no_way_through_per_h",
            "f_stopped_closing", "f_ground_no_way", "drops_gave_up", "drops_gave_up_per_h", "drops_proven",
            "drops_proven_per_h", "e10_has_dropped_per_h", "e10_drops_gave_up_per_h", "e10_drops_proven_per_h",
            "leg_plans", "legs_failed", "ceiling_burned_by"]
    series = [{f: r.get(f) for f in keep} for r in rows]
    # The same-age comparison leaves out a session in which a benchmark was run through the console: the benchmark's
    # own searches share the population's allowance and its counters.
    benched = sorted({b["session"] for b in k.get("door_benchmarks") or [] if b["session"]})
    trace = {}
    for name in [BEFORE] + [b["session"] for b in BUILDS]:
        s = next((x for x in sessions if x["session"] == name), None)
        if s and s["summaries"] and name not in benched:
            trace[name] = [[round(minutes_between(s["first"], x["time"]), 2), x["searches"], x["search_ms"],
                            x["partial"], x["partial_no_nearer"]] for x in s["summaries"]]
    picked = ("extracted_from_logs_up_to", "before_tiers", "build_262", "build_263", "build_264", "build_265",
              "build_266", "build_267", "logs_before_the_chart", "chart", "graph", "door_routes", "door_benchmarks",
              "failures", "graph_said_no_way")
    return {"sessions": series, "builds": BUILDS, "trace": trace, "benched": benched,
            "key": {kk: k.get(kk) for kk in picked}}


def refresh_html(path, payload):
    with open(path, encoding="utf-8") as fh:
        html = fh.read()
    start_tag = '<script id="data" type="application/json">'
    i = html.find(start_tag)
    j = html.find("</script>", i)
    if i < 0 or j < 0:
        print(f"no data block in {os.path.basename(path)}; left as it is", file=sys.stderr)
        return False
    blob = json.dumps(payload, separators=(",", ":"), ensure_ascii=True).replace("</", "<\\/")
    html = html[:i + len(start_tag)] + blob + html[j:]
    with open(path, "w", encoding="utf-8", newline="\n") as fh:
        fh.write(html)
    return True


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    ap = argparse.ArgumentParser(description="Regenerate the pathfinding data from a folder of shard logs.")
    ap.add_argument("logs", help="folder holding session-*.log (and optionally bot-debugger-commands.log)")
    ap.add_argument("--out", default=os.path.join(here, "data"), help="where to write the data (default: data/)")
    ap.add_argument("--html", default=os.path.join(here, "index.html"), help="page whose embedded data to refresh")
    ap.add_argument("--no-html", action="store_true", help="do not touch the page")
    args = ap.parse_args()

    names = sorted(n for n in os.listdir(args.logs) if SESSION_NAME.match(n))
    if not names:
        sys.exit("no session-*.log files in that folder")
    sessions = [read_session(os.path.join(args.logs, n), n) for n in names]
    sessions = [s for s in sessions if s["first"] is not None]
    os.makedirs(args.out, exist_ok=True)

    rows = [session_row(s) for s in sessions]
    write_csv(os.path.join(args.out, "sessions.csv"), rows, SESSION_FIELDS)

    summaries = to_rows(sessions, "summaries")
    write_csv(os.path.join(args.out, "summaries.csv"), summaries,
              ["session", "time", "age_min"] + SUMMARY_FIELDS + ["ceiling_burned_by", "errands_lost_by_kind"])

    write_csv(os.path.join(args.out, "roads.csv"), to_rows(sessions, "roads"),
              ["session", "time", "age_min"] + ROADS_FIELDS)
    write_csv(os.path.join(args.out, "chart.csv"), to_rows(sessions, "charts"),
              ["session", "time", "age_min"] + CHART_FIELDS)
    write_csv(os.path.join(args.out, "navigation_graph.csv"), to_rows(sessions, "navs"),
              ["session", "time", "age_min", "event", "map", "clusters", "cluster_side", "from", "clusters_built",
               "state", "nodes", "gates", "edges", "components", "cells_probed", "loop_ms", "seconds", "regions",
               "region_side", "file_ms", "rebuilt_by_houses"])
    write_csv(os.path.join(args.out, "navigation_summary.csv"), to_rows(sessions, "nav_summaries"),
              ["session", "time", "age_min"] + NAV_SUMMARY_FIELDS + NAV_EXTRA_FIELDS)

    door, bench = read_door(os.path.join(args.logs, "bot-debugger-commands.log"), sessions)
    write_csv(os.path.join(args.out, "door_routes.csv"), door,
              ["session", "time", "tool", "from", "to", "points", "steps", "steps_exact", "cost", "straight",
               "nodes_expanded", "ms", "first_point", "point_12", "legs_tried", "legs_reached", "legs_worst_ms"])
    write_csv(os.path.join(args.out, "door_benchmarks.csv"), bench, BENCH_FIELDS)

    write_csv(os.path.join(args.out, "builds.csv"), BUILDS, ["build", "session", "label", "what"])

    extracted_at = max(s["last"] for s in sessions).isoformat()
    k = key_numbers(sessions, rows, door, bench, extracted_at)
    with open(os.path.join(args.out, "key_numbers.json"), "w", encoding="utf-8", newline="\n") as fh:
        json.dump(k, fh, indent=2, ensure_ascii=True)
        fh.write("\n")

    if not args.no_html and os.path.exists(args.html):
        refresh_html(args.html, viz_payload(rows, sessions, k))

    print(f"{len(rows)} sessions, {len(summaries)} summaries, {sum(len(s['roads']) for s in sessions)} road lines, "
          f"{sum(len(s['charts']) for s in sessions)} chart lines, {sum(len(s['navs']) for s in sessions)} graph lines, "
          f"{sum(len(s['nav_summaries']) for s in sessions)} navigation summaries, {len(door)} door routes, "
          f"{len(bench)} door benchmarks; logs read up to {extracted_at}")


if __name__ == "__main__":
    main()
