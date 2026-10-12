#!/usr/bin/env python3
"""
Work weights: regenerate data/ from a folder of shard logs.

Every time a bot wins an auction, BotWill writes one line such as

    [07:20:17 INF] Rowan took on hunt: after a timber wolf: 119/min = 111 × 0.92, that being the fifth
    root of near 0.96 × new 1.00 × room 0.97 × safe 1.00 × purse 1.00 × load 1.00 × revel 1.00 ×
    ground 0.70 × charter 1.00; × 1.30 for its own trade; × 0.90 after the toll on this land; 2 of 2
    offers worth anything; over restock: after 60 Bandage at 10/min <s:Server.BotAI.V2.BotWill>

(the "take line"; see BotWill.Commit and BotWeigh.Describe). This script parses every such line into
data/takes.csv.gz, derives the tables in data/, reads the "overstated" alarms from alerts*.ndjson in the
same folder, and refreshes the JSON embedded in index.html.

Usage
    python extract.py LOGS_DIR [--until "YYYY-MM-DD HH:MM:SS"] [--no-html]

    LOGS_DIR   folder holding session-YYYY-MM-DD_HH-MM.log files and alerts*.ndjson
    --until    ignore every log line and alarm stamped after this moment (the shard keeps writing, so a
               cutoff is what makes a run repeatable)
    --no-html  leave index.html alone

Only the Python standard library is used. Nothing here reads anything but LOGS_DIR, and nothing is written
outside the folder this script lives in.
"""

import argparse
import collections
import csv
import datetime as dt
import glob
import gzip
import io
import json
import math
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
DATA = os.path.join(HERE, "data")

# Every factor the take line can name, in the order it names them. The last four are printed only when one of
# them is not at its neutral value (BotWeigh.Describe), so when they are absent all four are exactly 1.
FACTORS = ["near", "new", "room", "safe", "purse", "load", "revel", "ground", "charter"]

ROOTS = {"square": 2, "cube": 3, "fourth": 4, "fifth": 5, "sixth": 6, "seventh": 7, "eighth": 8, "ninth": 9}

SAMPLE_EVERY = 250  # every Nth parsed take goes to the plain-text sample

# ---------------------------------------------------------------------------------------------------------------
# The take line
# ---------------------------------------------------------------------------------------------------------------

LINE = re.compile(
    r"^\[(?P<time>\d\d:\d\d:\d\d) INF\] (?P<bot>.+?) took on (?P<deed>.+?): "
    r"(?P<rate>-?\d+)/min = (?P<est>-?\d+) × (?P<bend>[^,\s]+), that being the (?P<root>\w+) root of "
    r"(?P<rest>.*?)\s*<s:Server\.BotAI\.V2\.BotWill>\s*$"
)

FACTOR_RE = re.compile(
    r"^near (?P<near>[^\s;]+) × new (?P<new>[^\s;]+) × room (?P<room>[^\s;]+) × safe (?P<safe>[^\s;]+)"
    r" × purse (?P<purse>[^\s;]+)"
    r"(?: × load (?P<load>[^\s;]+) × revel (?P<revel>[^\s;]+) × ground (?P<ground>[^\s;]+)"
    r" × charter (?P<charter>[^\s;]+))?"
)
CALLING_RE = re.compile(r"^; × (?P<v>[^\s;]+) for (?P<w>its own trade|another's trade|anybody's work)")
TOLL_RE = re.compile(r"^; × (?P<v>[^\s;]+) after the toll on this land")
OFFERS_RE = re.compile(r"^; (?P<viable>\d+) of (?P<table>\d+) offers worth anything")
OVER_RE = re.compile(r"^; over (?P<ru>.+?) at (?P<rurate>-?\d+)/min(?=$|; (?:dropped|put down|finished) )")
GONE_RE = re.compile(r"^; (?P<as>dropped|put down|finished) (?P<what>.+)$")
CUT_RE = re.compile(r"; (?:over|dropped|put down|finished) ")
HOW_RE = re.compile(r"to take up again \((?P<how>\w+) by ")

# The refusals BotAppraisal.Weigh can name, in the order it asks them. Only the first refused offer of a review is
# printed on the take line ("refused: ..."), so these are counts of first refusals, not of all refusals.
VETOES = [
    ("nothing-to-weigh", "nothing to weigh", re.compile(r"^nothing to weigh")),
    ("other-map", "is on another map", re.compile(r"^(?P<k>\S+) is on another map")),
    ("dungeon-edge", "the other side of a dungeon's edge",
     re.compile(r"^(?P<k>\S+) is (?:on the island and this bot is underground|underground and this bot is on the island)")),
    ("outlay", "cannot pay to start",
     re.compile(r"^(?P<k>\S+) costs (?P<a>-?\d+)gp and it has (?P<b>-?\d+)gp")),
    ("no-room-for-coin", "a counter, and no room in the pack for a coin",
     re.compile(r"^(?P<k>\S+) goes through a counter and the pack has no room for a coin")),
    ("breaker", "rested after failing the same way again and again",
     re.compile(r"^(?P<k>\S+) failed (?P<a>\d+) times running for the same reason and rests (?P<b>\d+)s more")),
    ("becalmed", "a long walk from here lately closed nothing",
     re.compile(r"^(?P<k>\S+) is (?P<a>\d+) tiles off, and a walk of (?P<b>\d+) from about here closed nothing")),
    ("deaths-close", "bots died there lately: closed to all but running (until 26.09)",
     re.compile(r"^(?P<k>\S+) lies in or beside ground where (?P<a>[\d.]+) bots have died lately, against (?P<b>[\d.]+) that close it to everything but running")),
    ("deaths-keep-out", "bots died there lately: ordinary work kept out (until 26.09)",
     re.compile(r"^(?P<k>\S+) lies in or beside ground where (?P<a>[\d.]+) bots have died lately, against (?P<b>[\d.]+) that keep ordinary work out")),
    ("hostile-strength", "hostile strength 3x what goes there (from 26.09)",
     re.compile(r"^(?P<k>\S+) lies where the hostile strength is (?P<a>-?[\d.]+), (?P<r>[\d.]+) times the (?P<b>-?[\d.]+) going there")),
    ("red-in-town", "a red bot and a guarded town",
     re.compile(r"^(?P<k>\S+) lies in a guarded town and .+ is red$")),
    ("expected-nothing", "expected to pay nothing or less here",
     re.compile(r"^(?P<k>\S+) is expected to pay (?P<a>-?[\d.]+)/min here, against a claim of (?P<b>-?[\d.]+)")),
    ("weighed-nothing", "every factor together came to nothing",
     re.compile(r"^(?P<k>\S+) weighed out at nothing")),
]
VETO_LABEL = {code: label for code, label, _ in VETOES}
VETO_ORDER = [code for code, _, _ in VETOES] + ["unrecognised"]

# Bins for a factor's printed value. A factor is printed to two places, so 1.00 is exactly neutral. The edges keep the
# floors apart: 0.10 is the least room and the least purse, 0.15 is what caution leaves, 0.02 is the load factor. The
# combined factor (the root of the product) is binned the same way so the two can be read side by side.
BINS = [
    ("<0.10", lambda v: v < 0.10),
    ("0.10-0.29", lambda v: 0.10 <= v < 0.30),
    ("0.30-0.59", lambda v: 0.30 <= v < 0.60),
    ("0.60-0.89", lambda v: 0.60 <= v < 0.90),
    ("0.90-0.99", lambda v: 0.90 <= v < 1.00),
    ("1.00", lambda v: v == 1.00),
    (">1.00", lambda v: v > 1.00),
]

SESSION_NAME = re.compile(r"^session-(\d{4}-\d{2}-\d{2})_(\d{2})-(\d{2})\.log$")

# The census ("Will: ...", every five minutes) counts every refused offer for the vetoes that keep a counter, where the
# take line shows only the first refusal of a review. Its counters run from the world's load, so the last census of a
# session is that session's total (less whatever happened after it, at most one census interval). Phrases changed
# between builds; each is looked for on its own.
CENSUS = [
    ("taken_on", r"Will: (\d+) taken on"),
    ("finished", r"taken on, (\d+) finished"),
    ("failed", r"finished, (\d+) failed"),
    ("dropped", r"failed, (\d+) dropped"),
    ("died", r"(\d+) died doing it"),
    ("nothing_worth_doing", r"(\d+) times nothing was worth doing"),
    ("withheld_sworn_elsewhere", r"(\d+) offers withheld from classes sworn elsewhere"),
    ("marked_down_overloaded", r"(\d+) offers marked down because the bot was carrying more"),
    ("refused_no_room_for_coin", r"(\d+) offers refused because the work goes through a counter"),
    ("refused_dungeon_edge", r"(\d+) offers refused for lying across a dungeon's edge"),
    ("refused_becalmed", r"(\d+) offers of another long walk from the same footing were refused"),
    ("refused_breaker_resting", r"(\d+) offers refused while resting"),
    ("refused_deaths_keep_out", r"(\d+) offers refused because the work lay in or beside ground where bots had been dying"),
    ("refused_deaths_close", r"and (\d+) because so many had died there that it was closed to all work but running"),
    ("refused_hostile_strength", r"(\d+) offers refused because the hostile strength where the work lay"),
    ("prowls_let_in_restless", r"(\d+) prowls let in at the unpaid floor"),
    ("unpaid_let_past", r"(\d+) times work that is paid nothing on purpose was let past"),
    ("kept_committed", r"(\d+) better offers were turned down because the work in hand had already been paid for"),
    ("paperwork_filed", r"(\d+) offers of paperwork left for the next choice"),
    ("second_guesses", r"(\d+) second guesses of the same kind turned down"),
    ("grounded_rank", r"(\d+) times a full pack put the errand that needs no step ahead"),
    ("dislodged_rank", r"and (\d+) times it took what the bot was holding away for it"),
]
CENSUS_RE = [(name, re.compile(pattern)) for name, pattern in CENSUS]


def num(text):
    """A number as the log printed it, or None."""
    try:
        value = float(text)
    except (TypeError, ValueError):
        return None
    return value if math.isfinite(value) else None


def bin_of(value, bins):
    for label, test in bins:
        if test(value):
            return label
    return "?"


KIND_RE = re.compile(r"[^\s:,]+")


def kind_of(deed_text):
    """The kind of a deed as BotDeed.ToString prints it: 'kind' or 'kind: stage'."""
    if not deed_text:
        return deed_text
    m = KIND_RE.match(deed_text)
    return m.group(0) if m else deed_text


def classify_veto(text):
    """(code, refused kind, match) for a refusal sentence."""
    for code, _, pattern in VETOES:
        m = pattern.match(text)
        if m:
            return code, m.groupdict().get("k"), m
    first = text.split(" ", 1)[0] if text else None
    return "unrecognised", first, None


def after_refusal(text):
    """Parse what follows the refusal: an optional runner-up, then an optional note on the work put aside."""
    runner = runner_rate = gone = None
    rest = text
    if rest.startswith("; over "):
        m = OVER_RE.match(rest)
        if not m:
            return None
        runner, runner_rate = m.group("ru"), int(m.group("rurate"))
        rest = rest[m.end():]
    if rest:
        m = GONE_RE.match(rest)
        if not m:
            return None
        gone = (m.group("as"), m.group("what"))
    return runner, runner_rate, gone


def split_tail(tail):
    """
    Everything after "N of M offers worth anything": refusal, runner-up, displaced work. A refusal can quote a failure
    reason that itself contains "; ", so each possible end of the refusal is tried until the remainder parses.
    Returns (refused, runner, runner_rate, gone) or None.
    """
    if tail.startswith("; refused: "):
        body = tail[len("; refused: "):]
        cuts = [m.start() for m in CUT_RE.finditer(body)] + [len(body)]
        for cut in cuts:
            parsed = after_refusal(body[cut:])
            if parsed is not None:
                return (body[:cut],) + parsed
        return None
    parsed = after_refusal(tail)
    return None if parsed is None else (None,) + parsed


def parse_take(line):
    """One take line, as a dict, or None when it is not one (or not one this parser understands)."""
    m = LINE.match(line)
    if not m:
        return None
    rest = m.group("rest")
    fm = FACTOR_RE.match(rest)
    if not fm:
        return None
    row = {
        "bot": m.group("bot"),
        "deed": m.group("deed"),
        "rate": int(m.group("rate")),
        "estimate": int(m.group("est")),
        "factor": num(m.group("bend")),
        "root": ROOTS.get(m.group("root")),
    }
    for name in FACTORS:
        value = fm.group(name)
        row[name] = 1.0 if value is None else num(value)
    row["extra_printed"] = fm.group("load") is not None
    tail = rest[fm.end():]

    row["calling"], row["calling_word"] = 1.0, "anybody's work"
    cm = CALLING_RE.match(tail)
    if cm:
        row["calling"], row["calling_word"] = num(cm.group("v")), cm.group("w")
        tail = tail[cm.end():]
    row["toll"] = 1.0
    tm = TOLL_RE.match(tail)
    if tm:
        row["toll"] = num(tm.group("v"))
        tail = tail[tm.end():]
    om = OFFERS_RE.match(tail)
    if not om:
        return None
    row["viable"], row["offered"] = int(om.group("viable")), int(om.group("table"))
    split = split_tail(tail[om.end():])
    if split is None:
        return None
    refused, runner, runner_rate, gone = split
    row["refused"] = refused
    row["runner"] = runner
    row["runner_rate"] = runner_rate
    row["gone"] = gone
    return row


def smallest_factor(row):
    """The named factor with the lowest printed value, 'none' when every one is at or above 1, 'tie' when shared."""
    low = min(row[name] for name in FACTORS)
    if low >= 1.0:
        return "none", 1.0
    names = [name for name in FACTORS if row[name] == low]
    return (names[0] if len(names) == 1 else "tie"), low


def largest_effect(row):
    """
    What bent this score most, counting each factor as it reaches the score: under the root a factor f arrives as
    f^(1/root); the calling and the toll multiply the score whole. None when nothing lowered it.
    """
    root = row["root"] or 5
    effects = {name: row[name] ** (1.0 / root) for name in FACTORS if row[name] is not None and 0 < row[name] < 1.0}
    if row["calling"] is not None and row["calling"] < 1.0:
        effects["calling"] = row["calling"]
    if row["toll"] is not None and row["toll"] < 1.0:
        effects["toll"] = row["toll"]
    if not effects:
        return "none"
    low = min(effects.values())
    names = [name for name, value in effects.items() if value == low]
    return names[0] if len(names) == 1 else "tie"


def check_rate(row):
    """Whether rate = estimate x factor x calling x toll, within what the printed rounding allows."""
    est, bend, calling, toll = row["estimate"], row["factor"], row["calling"], row["toll"]
    if None in (bend, calling, toll):
        return False
    scale = calling * toll
    tolerance = 0.5 + 0.5 * bend * scale + abs(est) * 0.005 * scale + 0.01
    return abs(row["rate"] - est * bend * scale) <= tolerance


def check_factor(row):
    """Whether the printed combined factor is the root of the printed factors' product, within their rounding."""
    root = row["root"]
    values = [row[name] for name in FACTORS]
    if root is None or row["factor"] is None or any(v is None for v in values):
        return False
    lo = hi = 1.0
    for v in values:
        lo *= max(v - 0.005, 1e-9)
        hi *= v + 0.005
    return lo ** (1.0 / root) - 0.006 <= row["factor"] <= hi ** (1.0 / root) + 0.006


# ---------------------------------------------------------------------------------------------------------------
# Small statistics
# ---------------------------------------------------------------------------------------------------------------

def quantile(sorted_values, p):
    """Linear interpolation between closest ranks (the common default)."""
    if not sorted_values:
        return None
    if len(sorted_values) == 1:
        return sorted_values[0]
    h = (len(sorted_values) - 1) * p
    lo = math.floor(h)
    hi = math.ceil(h)
    return sorted_values[lo] + (h - lo) * (sorted_values[hi] - sorted_values[lo])


def r(value, places=2):
    return None if value is None else round(value, places)


def share(part, whole, places=4):
    return round(part / whole, places) if whole else None


# ---------------------------------------------------------------------------------------------------------------
# Reading the logs
# ---------------------------------------------------------------------------------------------------------------

def session_files(logs):
    found = []
    for path in glob.glob(os.path.join(logs, "session-*.log")):
        m = SESSION_NAME.match(os.path.basename(path))
        if m:
            start = dt.datetime.strptime(f"{m.group(1)} {m.group(2)}:{m.group(3)}", "%Y-%m-%d %H:%M")
            found.append((start, path))
    found.sort()
    return found


def stamp(day, clock):
    return dt.datetime.combine(day, dt.time.fromisoformat(clock))


class Tally:
    """Everything the tables need, gathered in one pass."""

    def __init__(self):
        self.takes = 0
        self.bots = set()
        self.sessions = []
        self.first = None
        self.last = None
        self.lines_seen = 0
        self.unparsed = 0
        self.unparsed_examples = []
        self.rate_bad = 0
        self.factor_bad = 0
        self.roots = collections.Counter()
        self.by_kind = collections.defaultdict(lambda: collections.defaultdict(list))
        self.kind_counts = collections.defaultdict(collections.Counter)
        self.factor_bins = {name: collections.Counter() for name in FACTORS}
        self.factor_low = {name: [] for name in FACTORS}
        self.factor_high = {name: collections.Counter() for name in FACTORS}
        self.bend_bins = collections.Counter()
        self.smallest = collections.Counter()
        self.smallest_by_kind = collections.defaultdict(collections.Counter)
        self.effect = collections.Counter()
        self.calling = collections.Counter()
        self.toll = collections.Counter()
        self.viable = collections.Counter()
        self.offered = collections.Counter()
        self.refusals = collections.Counter()
        self.refusal_kinds = collections.defaultdict(collections.Counter)
        self.refusal_dates = collections.defaultdict(collections.Counter)
        self.refusal_example = {}
        self.refusal_first = {}
        self.refusal_last = {}
        self.expected = collections.defaultdict(lambda: ([], []))
        self.hostile_ratio = []
        self.pairs = collections.Counter()
        self.pair_rates = collections.defaultdict(lambda: ([], []))
        self.margins = []
        self.gone = collections.Counter()
        self.put_down_how = collections.Counter()
        self.by_date = collections.defaultdict(collections.Counter)
        self.kind_bots = collections.defaultdict(set)
        self.extra_printed = 0
        self.date_bots = collections.defaultdict(set)
        self.bot_kind = collections.Counter()
        self.bot_kind_rows = collections.defaultdict(
            lambda: {"first": None, "last": None, "estimate": [], "rate": [], "new": [], "new0": 0})
        self.refusal_sessions = collections.defaultdict(collections.Counter)
        self.estimate_zero = collections.Counter()
        self.runner_higher = 0
        self.all_factor = []
        self.all_smallest = []
        self.census = []
        self.standing = {}


def read_takes(logs, until, writer, sample_writer, tally):
    for start, path in session_files(logs):
        if until is not None and start > until:
            continue
        name = os.path.basename(path)
        stem = name[len("session-"):-len(".log")]
        day = start.date()
        previous = None
        session_first = session_last = None
        session_takes = 0
        census_line = census_at = None
        with open(path, encoding="utf-8", errors="replace") as handle:
            for line in handle:
                if len(line) < 12 or line[0] != "[" or line[9] != " ":
                    continue
                clock = line[1:9]
                try:
                    seconds = int(clock[0:2]) * 3600 + int(clock[3:5]) * 60 + int(clock[6:8])
                except ValueError:
                    continue
                # The line carries only the time of day; a session that runs past midnight wraps.
                if previous is not None and seconds < previous - 6 * 3600:
                    day += dt.timedelta(days=1)
                previous = seconds
                if "<s:Server.BotAI.V2.BotWill>" not in line:
                    continue
                if "] Will: " in line:
                    when = stamp(day, clock)
                    if until is None or when <= until:
                        census_line, census_at = line, when
                    continue
                if " took on " not in line:
                    continue
                when = stamp(day, clock)
                if until is not None and when > until:
                    continue
                tally.lines_seen += 1
                row = parse_take(line.rstrip("\n"))
                if row is None:
                    tally.unparsed += 1
                    if len(tally.unparsed_examples) < 5:
                        tally.unparsed_examples.append(line.strip()[:300])
                    continue
                row["date"] = day.isoformat()
                row["time"] = clock
                row["session"] = stem
                record(row, tally)
                out = csv_row(row)
                writer.writerow(out)
                if tally.takes % SAMPLE_EVERY == 1:
                    sample_writer.writerow(out)
                session_takes += 1
                session_first = session_first or when
                session_last = when
                tally.first = when if tally.first is None or when < tally.first else tally.first
                tally.last = when if tally.last is None or when > tally.last else tally.last
        tally.sessions.append({
            "session": stem,
            "first_take": session_first.isoformat(sep=" ") if session_first else "",
            "last_take": session_last.isoformat(sep=" ") if session_last else "",
            "takes": session_takes,
        })
        if census_line is not None:
            counts = {"session": stem, "last_census": census_at.isoformat(sep=" ")}
            for key, pattern in CENSUS_RE:
                m = pattern.search(census_line)
                counts[key] = int(m.group(1)) if m else 0
            tally.census.append(counts)


CSV_COLUMNS = [
    "date", "time", "session", "bot", "kind", "stage", "rate", "estimate", "factor",
    *FACTORS, "calling", "calling_word", "toll", "viable", "offered",
    "refused_kind", "refused_class", "refused_reason", "runner_kind", "runner_rate",
    "displaced_as", "displaced_kind", "smallest_factor", "smallest_value",
]


def csv_row(row):
    deed = row["deed"]
    kind = kind_of(deed)
    stage = deed[len(kind) + 2:] if deed.startswith(kind + ": ") else ""
    refused_kind = refused_class = refused_reason = ""
    if row["refused"] is not None:
        refused_class, refused_kind, _ = classify_veto(row["refused"])
        refused_kind = refused_kind or ""
        refused_reason = row["refused"][len(refused_kind) + 1:] if refused_kind and row["refused"].startswith(refused_kind + " ") else row["refused"]
    runner_kind = kind_of(row["runner"]) if row["runner"] else ""
    displaced_as = displaced_kind = ""
    if row["gone"]:
        displaced_as = {"dropped": "dropped", "put down": "put down", "finished": "met"}[row["gone"][0]]
        displaced_kind = kind_of(row["gone"][1]) or ""
    smallest, low = row["smallest"]
    return [
        row["date"], row["time"], row["session"], row["bot"], kind, stage, row["rate"], row["estimate"],
        f"{row['factor']:.2f}", *[f"{row[name]:.2f}" for name in FACTORS], f"{row['calling']:.2f}",
        row["calling_word"], f"{row['toll']:.2f}", row["viable"], row["offered"], refused_kind, refused_class,
        refused_reason, runner_kind, "" if row["runner_rate"] is None else row["runner_rate"], displaced_as,
        displaced_kind, smallest, f"{low:.2f}",
    ]


def record(row, tally):
    tally.takes += 1
    kind = kind_of(row["deed"])
    row["kind"] = kind
    tally.bots.add(row["bot"])
    tally.roots[row["root"]] += 1
    if row["extra_printed"]:
        tally.extra_printed += 1
    if not check_rate(row):
        tally.rate_bad += 1
    if not check_factor(row):
        tally.factor_bad += 1

    pair = tally.bot_kind_rows[(row["bot"], kind)]
    tally.bot_kind[(row["bot"], kind)] += 1
    when = f"{row['date']} {row['time']}"
    pair["first"] = pair["first"] or when
    pair["last"] = when
    pair["estimate"].append(row["estimate"])
    pair["rate"].append(row["rate"])
    pair["new"].append(row["new"])
    pair["new0"] += row["new"] == 0.0
    if row["estimate"] == 0:
        tally.estimate_zero[kind] += 1
    tally.all_factor.append(row["factor"])

    k = tally.by_kind[kind]
    k["estimate"].append(row["estimate"])
    k["rate"].append(row["rate"])
    k["factor"].append(row["factor"])
    counts = tally.kind_counts[kind]
    counts["takes"] += 1
    tally.kind_bots[kind].add(row["bot"])
    if row["calling"] > 1.0:
        counts["own"] += 1
    elif row["calling"] < 1.0:
        counts["another"] += 1
    if row["toll"] < 1.0:
        counts["toll"] += 1
    if row["refused"] is not None:
        counts["refusal"] += 1
    if row["viable"] == 1:
        counts["alone"] += 1
    k["offered"].append(row["offered"])
    if row["runner"] is not None:
        counts["runner"] += 1
        k["runner_rate"].append(row["runner_rate"])
        if row["runner_rate"] > 0:
            k["margin"].append(row["rate"] / row["runner_rate"])
    for name in FACTORS:
        if row[name] < 1.0:
            counts["bites:" + name] += 1
    for name in ("near", "new", "room"):
        k[name].append(row[name])

    for name in FACTORS:
        value = row[name]
        tally.factor_bins[name][bin_of(value, BINS)] += 1
        if value < 1.0:
            tally.factor_low[name].append(value)
        elif value > 1.0:
            tally.factor_high[name][f"{value:.2f}"] += 1
    tally.bend_bins[bin_of(row["factor"], BINS)] += 1

    row["smallest"] = smallest_factor(row)
    tally.all_smallest.append(row["smallest"][1])
    tally.smallest[row["smallest"][0]] += 1
    tally.smallest_by_kind[kind][row["smallest"][0]] += 1
    tally.effect[largest_effect(row)] += 1
    tally.calling[row["calling_word"]] += 1
    tally.toll[f"{row['toll']:.2f}"] += 1
    tally.viable[row["viable"]] += 1
    tally.offered[row["offered"]] += 1

    date = row["date"]
    day = tally.by_date[date]
    day["takes"] += 1
    tally.date_bots[date].add(row["bot"])
    if row["runner"] is not None:
        day["runner"] += 1
    if row["viable"] == 1:
        day["alone"] += 1

    if row["refused"] is not None:
        code, refused_kind, m = classify_veto(row["refused"])
        tally.refusals[code] += 1
        tally.refusal_kinds[code][refused_kind or "?"] += 1
        tally.refusal_dates[date][code] += 1
        tally.refusal_sessions[row["session"]][code] += 1
        day["refusal"] += 1
        tally.refusal_example.setdefault(code, row["refused"][:240])
        when = f"{date} {row['time']}"
        tally.refusal_first.setdefault(code, when)
        tally.refusal_last[code] = when
        if code == "expected-nothing" and m:
            expected, claim = tally.expected[refused_kind]
            expected.append(float(m.group("a")))
            claim.append(float(m.group("b")))
            # The same reading, to the tenth, printed again and again in one session: a record that has not moved.
            key = (row["session"], refused_kind, m.group("a"), m.group("b"))
            seen = tally.standing.get(key)
            stamp_now = dt.datetime.fromisoformat(when)
            if seen is None:
                tally.standing[key] = [stamp_now, stamp_now, 1, {row["bot"]}]
            else:
                seen[1] = stamp_now
                seen[2] += 1
                seen[3].add(row["bot"])
        if code == "hostile-strength" and m:
            tally.hostile_ratio.append(float(m.group("r")))

    if row["runner"] is not None:
        runner_kind = kind_of(row["runner"])
        tally.pairs[(kind, runner_kind)] += 1
        rates, runner_rates = tally.pair_rates[(kind, runner_kind)]
        rates.append(row["rate"])
        runner_rates.append(row["runner_rate"])
        if row["runner_rate"] > 0:
            tally.margins.append(row["rate"] / row["runner_rate"])
        if row["runner_rate"] > row["rate"]:
            tally.runner_higher += 1

    if row["gone"]:
        tally.gone[row["gone"][0]] += 1
        if row["gone"][0] == "put down":
            m = HOW_RE.search(row["gone"][1])
            tally.put_down_how[m.group("how") if m else "?"] += 1


# ---------------------------------------------------------------------------------------------------------------
# The overstated alarm
# ---------------------------------------------------------------------------------------------------------------

READ_RE = re.compile(r"^(?P<trade>\S+) is read by the auction at (?P<read>-?\d+)/min and pays (?P<pays>-?\d+)/min over (?P<n>\d+) outcomes")
CLAIMS_RE = re.compile(r"^(?P<trade>\S+) claims (?P<read>-?\d+)/min and pays (?P<pays>-?\d+)/min over (?P<n>\d+) outcomes")
CLEAR_RE = re.compile(r"^(?P<trade>\S+)'s claim is back within")


def read_alarms(logs, until):
    seen = set()
    rows = []
    for path in sorted(glob.glob(os.path.join(logs, "alerts*.ndjson"))):
        with open(path, encoding="utf-8", errors="replace") as handle:
            for raw in handle:
                raw = raw.strip()
                if not raw or raw in seen:
                    continue
                seen.add(raw)
                try:
                    alarm = json.loads(raw)
                except json.JSONDecodeError:
                    continue
                if alarm.get("kind") != "overstated":
                    continue
                try:
                    at = dt.datetime.fromisoformat(alarm.get("at", ""))
                except ValueError:
                    continue
                if until is not None and at > until:
                    continue
                say = alarm.get("say", "")
                trade = read = pays = outcomes = None
                wording = ""
                m = READ_RE.match(say) or CLAIMS_RE.match(say)
                if m:
                    trade, read, pays, outcomes = m.group("trade"), int(m.group("read")), int(m.group("pays")), int(m.group("n"))
                    wording = "read by the auction" if m.re is READ_RE else "typed claim"
                else:
                    m = CLEAR_RE.match(say)
                    if m:
                        trade, wording = m.group("trade"), "cleared"
                rows.append({
                    "at": at.isoformat(sep=" "),
                    "state": alarm.get("state", ""),
                    "trade": trade or "",
                    "reads": "" if read is None else read,
                    "pays": "" if pays is None else pays,
                    "outcomes": "" if outcomes is None else outcomes,
                    "compared": wording,
                    "held_minutes": alarm.get("heldMinutes", ""),
                    "say": say,
                })
    rows.sort(key=lambda row: row["at"])
    return rows


def alarm_tables(rows):
    by_trade = collections.OrderedDict()
    for row in rows:
        if row["compared"] in ("", "cleared"):
            continue
        trade = by_trade.setdefault(row["trade"], {
            "trade": row["trade"], "lines": 0, "raised": 0, "first": row["at"], "last": row["at"],
            "reads": [], "pays": [], "outcomes": [], "dates": set(), "wordings": set(),
        })
        trade["lines"] += 1
        trade["raised"] += row["state"] == "raised"
        trade["last"] = row["at"]
        trade["reads"].append(row["reads"])
        trade["pays"].append(row["pays"])
        trade["outcomes"].append(row["outcomes"])
        trade["dates"].add(row["at"][:10])
        trade["wordings"].add(row["compared"])
    table = []
    for trade in by_trade.values():
        reads, pays = sorted(trade["reads"]), sorted(trade["pays"])
        table.append({
            "trade": trade["trade"],
            "alarm_lines": trade["lines"],
            "times_raised": trade["raised"],
            "first_seen": trade["first"],
            "last_seen": trade["last"],
            "days_seen": len(trade["dates"]),
            "reads_min": reads[0], "reads_median": r(quantile(reads, 0.5), 1), "reads_max": reads[-1],
            "pays_min": pays[0], "pays_median": r(quantile(pays, 0.5), 1), "pays_max": pays[-1],
            "outcomes_max": max(trade["outcomes"]),
            "compared": "+".join(sorted(trade["wordings"])),
        })
    table.sort(key=lambda row: -row["alarm_lines"])
    return table


# ---------------------------------------------------------------------------------------------------------------
# Writing
# ---------------------------------------------------------------------------------------------------------------

def write_csv(name, header, rows):
    with open(os.path.join(DATA, name), "w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle, lineterminator="\n")
        writer.writerow(header)
        for row in rows:
            writer.writerow([row.get(column, "") for column in header] if isinstance(row, dict) else row)


def build_tables(tally, alarms):
    total = tally.takes
    kinds = sorted(tally.by_kind, key=lambda kind: -tally.kind_counts[kind]["takes"])

    # Per kind.
    kind_rows = []
    for kind in kinds:
        k, c = tally.by_kind[kind], tally.kind_counts[kind]
        est, rate, bend = sorted(k["estimate"]), sorted(k["rate"]), sorted(k["factor"])
        runner, margin, offered = sorted(k["runner_rate"]), sorted(k["margin"]), sorted(k["offered"])
        bots = len(tally.kind_bots[kind])
        row = {
            "kind": kind, "takes": c["takes"], "share_of_takes": share(c["takes"], total), "bots": bots,
            "estimate_p25": r(quantile(est, 0.25), 1), "estimate_median": r(quantile(est, 0.5), 1),
            "estimate_p75": r(quantile(est, 0.75), 1),
            "rate_p25": r(quantile(rate, 0.25), 1), "rate_median": r(quantile(rate, 0.5), 1),
            "rate_p75": r(quantile(rate, 0.75), 1),
            "factor_p10": r(quantile(bend, 0.10)), "factor_median": r(quantile(bend, 0.5)),
            "share_own_trade": share(c["own"], c["takes"]), "share_another_trade": share(c["another"], c["takes"]),
            "share_tolled": share(c["toll"], c["takes"]),
            "share_with_refusal": share(c["refusal"], c["takes"]),
            "share_only_offer": share(c["alone"], c["takes"]),
            "offers_median": r(quantile(offered, 0.5), 1),
            "share_with_runner_up": share(c["runner"], c["takes"]),
            "runner_up_rate_median": r(quantile(runner, 0.5), 1),
            "margin_over_runner_up_median": r(quantile(margin, 0.5)),
        }
        for name in FACTORS:
            row["bites_" + name] = share(c["bites:" + name], c["takes"])
        for name in ("near", "new", "room"):
            row[name + "_median"] = r(quantile(sorted(k[name]), 0.5))
        top_bot, top_n = max(((bot, n) for (bot, kk), n in tally.bot_kind.items() if kk == kind),
                             key=lambda item: (item[1], item[0]))
        row["top_bot"] = top_bot
        row["top_bot_share"] = share(top_n, c["takes"])
        kind_rows.append(row)

    # Factors.
    factor_rows = []
    for name in FACTORS:
        low = sorted(tally.factor_low[name])
        above = sum(tally.factor_high[name].values())
        factor_rows.append({
            "factor": name,
            "takes": total,
            "below_1": len(low), "share_below_1": share(len(low), total),
            "above_1": above, "share_above_1": share(above, total),
            "median_when_below_1": r(quantile(low, 0.5)), "p10_when_below_1": r(quantile(low, 0.10)),
            "min": r(low[0]) if low else 1.0,
            "values_above_1": " ".join(f"{value}:{count}" for value, count in sorted(tally.factor_high[name].items())),
        })
    bin_rows = []
    for name in FACTORS:
        for label, _ in BINS:
            bin_rows.append({"factor": name, "bin": label, "takes": tally.factor_bins[name][label],
                             "share": share(tally.factor_bins[name][label], total)})
    for label, _ in BINS:
        bin_rows.append({"factor": "combined", "bin": label, "takes": tally.bend_bins[label],
                         "share": share(tally.bend_bins[label], total)})

    # Which factor bent the score most.
    decide_rows = []
    for name in FACTORS + ["calling", "toll", "tie", "none"]:
        if name in ("calling", "toll"):
            under = None
        else:
            under = tally.smallest.get(name, 0)
        effect = tally.effect.get(name, 0)
        decide_rows.append({
            "factor": name,
            "smallest_under_root": "" if under is None else under,
            "share_smallest_under_root": "" if under is None else share(under, total),
            "largest_effect_on_score": effect,
            "share_largest_effect_on_score": share(effect, total),
        })
    decide_kind_rows = []
    for kind in kinds:
        counts = tally.smallest_by_kind[kind]
        n = tally.kind_counts[kind]["takes"]
        row = {"kind": kind, "takes": n}
        for name in FACTORS + ["tie", "none"]:
            row[name] = share(counts.get(name, 0), n)
        decide_kind_rows.append(row)

    # Refusals.
    refused_total = sum(tally.refusals.values())
    refusal_rows = []
    for code in VETO_ORDER:
        n = tally.refusals.get(code, 0)
        if not n:
            continue
        top = tally.refusal_kinds[code].most_common(5)
        refusal_rows.append({
            "class": code, "meaning": VETO_LABEL.get(code, "not recognised by this parser"),
            "first_refusals": n, "share_of_refusals": share(n, refused_total), "share_of_takes": share(n, total),
            "first_seen": tally.refusal_first.get(code, ""), "last_seen": tally.refusal_last.get(code, ""),
            "top_refused_kinds": "; ".join(f"{kind} {count}" for kind, count in top),
            "example": tally.refusal_example.get(code, ""),
        })
    refusal_rows.sort(key=lambda row: -row["first_refusals"])
    refusal_kind_rows = []
    for code in VETO_ORDER:
        for kind, count in tally.refusal_kinds[code].most_common():
            refusal_kind_rows.append({"class": code, "refused_kind": kind, "first_refusals": count})
    expected_rows = []
    for kind, (expected, claim) in sorted(tally.expected.items(), key=lambda item: -len(item[1][0])):
        e, c = sorted(expected), sorted(claim)
        expected_rows.append({
            "refused_kind": kind, "refusals": len(e),
            "expected_median": r(quantile(e, 0.5), 1), "expected_p10": r(quantile(e, 0.10), 1),
            "expected_min": r(e[0], 1),
            "claim_median": r(quantile(c, 0.5), 1), "claim_p10": r(quantile(c, 0.10), 1),
            "claim_p90": r(quantile(c, 0.90), 1),
            "share_expected_below_0": share(sum(1 for v in e if v < 0), len(e)),
        })

    # Runner-up pairs.
    pair_rows = []
    for (kind, runner), n in tally.pairs.most_common(60):
        rates, runner_rates = tally.pair_rates[(kind, runner)]
        rates, runner_rates = sorted(rates), sorted(runner_rates)
        pair_rows.append({
            "winner": kind, "runner_up": runner, "takes": n, "share_of_takes": share(n, total),
            "winner_rate_median": r(quantile(rates, 0.5), 1), "runner_up_rate_median": r(quantile(runner_rates, 0.5), 1),
        })

    # By date.
    date_rows = []
    for date in sorted(tally.by_date):
        day = tally.by_date[date]
        row = {"date": date, "takes": day["takes"], "bots": len(tally.date_bots[date]),
               "share_with_refusal": share(day["refusal"], day["takes"]),
               "share_with_runner_up": share(day["runner"], day["takes"]),
               "share_only_offer": share(day["alone"], day["takes"])}
        for code in VETO_ORDER:
            row[code] = tally.refusal_dates[date].get(code, 0)
        date_rows.append(row)

    bot_kind_rows = []
    for (bot, kind), n in sorted(tally.bot_kind.items(), key=lambda item: (-item[1], item[0]))[:40]:
        pair = tally.bot_kind_rows[(bot, kind)]
        bot_kind_rows.append({
            "bot": bot, "kind": kind, "takes": n, "share_of_kind": share(n, tally.kind_counts[kind]["takes"]),
            "first_take": pair["first"], "last_take": pair["last"],
            "estimate_median": r(quantile(sorted(pair["estimate"]), 0.5), 1),
            "rate_median": r(quantile(sorted(pair["rate"]), 0.5), 1),
            "new_median": r(quantile(sorted(pair["new"]), 0.5)),
            "takes_with_new_0": pair["new0"],
        })

    standing_rows = []
    for (session, kind, expected, claim), (first, last, times, bots) in tally.standing.items():
        hours = (last - first).total_seconds() / 3600.0
        if hours >= 1.0:
            standing_rows.append({
                "session": session, "refused_kind": kind, "expected": expected, "claim": claim,
                "first_seen": first.isoformat(sep=" "), "last_seen": last.isoformat(sep=" "),
                "hours": round(hours, 2), "times": times, "bots": len(bots),
                "bot_names": " / ".join(sorted(bots)) if len(bots) <= 6 else "",
            })
    standing_rows.sort(key=lambda row: (-row["hours"], row["session"], row["refused_kind"], row["expected"]))

    session_rows = []
    for s in tally.sessions:
        row = {"session": s["session"], "first_take": s["first_take"], "last_take": s["last_take"], "takes": s["takes"]}
        refused = tally.refusal_sessions[s["session"]]
        for code in VETO_ORDER:
            row["first_refusals_" + code] = refused.get(code, 0)
        session_rows.append(row)

    return {
        "bot_kind": bot_kind_rows, "sessions": session_rows, "census": tally.census, "standing": standing_rows,
        "kinds": kind_rows, "factors": factor_rows, "bins": bin_rows, "decide": decide_rows,
        "decide_kind": decide_kind_rows, "refusals": refusal_rows, "refusal_kinds": refusal_kind_rows,
        "expected": expected_rows, "pairs": pair_rows, "dates": date_rows,
        "alarms_by_trade": alarm_tables(alarms), "refused_total": refused_total,
    }


def summary(tally, tables, alarms, until, files):
    total = tally.takes
    margins = sorted(tally.margins)
    all_neutral = tally.smallest.get("none", 0)
    runner_total = sum(tally.pairs.values())
    return {
        "made_by": "extract.py",
        "until": until.isoformat(sep=" ") if until else None,
        "window": {"first_take": tally.first.isoformat(sep=" ") if tally.first else None,
                   "last_take": tally.last.isoformat(sep=" ") if tally.last else None},
        "session_files_read": files,
        "sessions_with_takes": sum(1 for s in tally.sessions if s["takes"]),
        "take_lines_seen": tally.lines_seen,
        "take_lines_parsed": total,
        "take_lines_unparsed": tally.unparsed,
        "unparsed_examples": tally.unparsed_examples,
        "self_check": {
            "rate_not_equal_estimate_x_factor_x_multipliers": tally.rate_bad,
            "factor_not_root_of_product": tally.factor_bad,
            "roots_seen": {str(k): v for k, v in tally.roots.items()},
        },
        "bots": len(tally.bots),
        "kinds": len(tally.by_kind),
        "share_all_factors_neutral": share(all_neutral, total),
        "share_extra_factors_printed": share(tally.extra_printed, total),
        "share_with_refusal": share(tables["refused_total"], total),
        "first_refusals": tables["refused_total"],
        "share_with_runner_up": share(runner_total, total),
        "share_only_offer": share(tally.viable.get(1, 0), total),
        "offers_made": {str(k): v for k, v in sorted(tally.offered.items())},
        "offers_worth_anything": {str(k): v for k, v in sorted(tally.viable.items())},
        "margin_over_runner_up": {"p10": r(quantile(margins, 0.10)), "p25": r(quantile(margins, 0.25)),
                                  "median": r(quantile(margins, 0.5)), "p75": r(quantile(margins, 0.75)),
                                  "p90": r(quantile(margins, 0.90)),
                                  "share_below_1_25": share(sum(1 for m in margins if m < 1.25), len(margins))},
        "runner_up_scored_higher": tally.runner_higher,
        "combined_factor_median": r(quantile(sorted(tally.all_factor), 0.5)),
        "smallest_factor_median": r(quantile(sorted(tally.all_smallest), 0.5)),
        "estimate_printed_0": {"total": sum(tally.estimate_zero.values()), **dict(tally.estimate_zero.most_common())},
        "expected_nothing_readings": {
            "distinct_in_a_session": len(tally.standing),
            "standing_an_hour_or_more": len(tables["standing"]),
            "longest_hours": tables["standing"][0]["hours"] if tables["standing"] else 0,
            "standing_for_several_bots": sum(1 for row in tables["standing"] if row["bots"] > 1),
        },
        "census_totals": {key: sum(row[key] for row in tally.census) for key, _ in CENSUS},
        "census_sessions": len(tally.census),
        "calling": dict(tally.calling),
        "toll": dict(tally.toll),
        "displaced": dict(tally.gone),
        "put_down_how": dict(tally.put_down_how),
        "hostile_strength_ratio": {"n": len(tally.hostile_ratio),
                                   "median": r(quantile(sorted(tally.hostile_ratio), 0.5), 1)},
        "overstated_alarm_lines": sum(1 for row in alarms if row["compared"] not in ("", "cleared")),
        "overstated_cleared_lines": sum(1 for row in alarms if row["compared"] == "cleared"),
    }


EMBED_START = '<script id="work-weights-data" type="application/json">'
EMBED_END = "</script>"


def embed(summary_doc, tables, alarms):
    """Put the numbers the page draws into index.html, between its data markers."""
    page = os.path.join(HERE, "index.html")
    if not os.path.exists(page):
        print("index.html not found beside the script; nothing embedded")
        return
    top_kinds = tables["kinds"][:24]
    payload = {
        "summary": summary_doc,
        "kinds": [{key: row[key] for key in ("kind", "takes", "share_of_takes", "estimate_p25", "estimate_median",
                                             "estimate_p75", "rate_p25", "rate_median", "rate_p75", "factor_median",
                                             "share_own_trade", "share_another_trade", "share_with_refusal",
                                             "share_only_offer", "margin_over_runner_up_median")}
                  for row in top_kinds],
        "factors": tables["factors"],
        "bins": tables["bins"],
        "decide": tables["decide"],
        "refusals": [{key: row[key] for key in ("class", "meaning", "first_refusals", "share_of_refusals",
                                                "first_seen", "last_seen", "top_refused_kinds", "example")}
                     for row in tables["refusals"]],
        "expected": tables["expected"][:12],
        "alarms": tables["alarms_by_trade"],
        "alarm_lines": [[row["at"], row["state"], row["trade"], row["reads"], row["pays"], row["outcomes"], row["compared"]]
                        for row in alarms],
        "dates": tables["dates"],
        "bot_kind": tables["bot_kind"][:12],
        "pairs": tables["pairs"][:15],
        # The readings repeated most often; the longest-standing one is named in the summary.
        "standing": [{key: row[key] for key in ("session", "refused_kind", "expected", "claim", "first_seen", "last_seen",
                                                 "hours", "times", "bots")}
                     for row in sorted(tables["standing"], key=lambda row: (-row["times"], -row["hours"]))[:8]],
    }
    text = json.dumps(payload, ensure_ascii=False, separators=(",", ":"))
    text = text.replace("</", "<\\/")
    with open(page, encoding="utf-8") as handle:
        html = handle.read()
    start = html.find(EMBED_START)
    end = html.find(EMBED_END, start + len(EMBED_START)) if start >= 0 else -1
    if start < 0 or end < 0:
        print("index.html has no data markers; nothing embedded")
        return
    html = html[:start + len(EMBED_START)] + text + html[end:]
    with open(page, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(html)
    print("embedded the data in index.html")


def main():
    parser = argparse.ArgumentParser(description="Regenerate the work-weights data from a folder of shard logs.")
    parser.add_argument("logs", help="folder with session-*.log and alerts*.ndjson")
    parser.add_argument("--until", help='ignore everything stamped after this, "YYYY-MM-DD HH:MM:SS"')
    parser.add_argument("--no-html", action="store_true", help="do not refresh the data inside index.html")
    args = parser.parse_args()

    if not os.path.isdir(args.logs):
        sys.exit(f"not a folder: {args.logs}")
    until = dt.datetime.fromisoformat(args.until) if args.until else None
    os.makedirs(DATA, exist_ok=True)

    tally = Tally()
    files = sum(1 for start, _ in session_files(args.logs) if until is None or start <= until)
    # mtime=0 keeps the compressed file byte-for-byte the same when the rows are the same.
    with io.TextIOWrapper(gzip.GzipFile(os.path.join(DATA, "takes.csv.gz"), "wb", compresslevel=9, mtime=0),
                          encoding="utf-8", newline="") as full, \
            open(os.path.join(DATA, "takes-sample.csv"), "w", encoding="utf-8", newline="") as sample:
        writer = csv.writer(full, lineterminator="\n")
        sample_writer = csv.writer(sample, lineterminator="\n")
        writer.writerow(CSV_COLUMNS)
        sample_writer.writerow(CSV_COLUMNS)
        read_takes(args.logs, until, writer, sample_writer, tally)

    alarms = read_alarms(args.logs, until)
    tables = build_tables(tally, alarms)
    doc = summary(tally, tables, alarms, until, files)

    write_csv("by_kind.csv", list(tables["kinds"][0].keys()) if tables["kinds"] else ["kind"], tables["kinds"])
    write_csv("factors.csv", list(tables["factors"][0].keys()), tables["factors"])
    write_csv("factor_bins.csv", ["factor", "bin", "takes", "share"], tables["bins"])
    write_csv("smallest_factor.csv", list(tables["decide"][0].keys()), tables["decide"])
    write_csv("smallest_factor_by_kind.csv", ["kind", "takes", *FACTORS, "tie", "none"], tables["decide_kind"])
    write_csv("refusals.csv", ["class", "meaning", "first_refusals", "share_of_refusals", "share_of_takes",
                               "first_seen", "last_seen", "top_refused_kinds", "example"], tables["refusals"])
    write_csv("refusals_by_kind.csv", ["class", "refused_kind", "first_refusals"], tables["refusal_kinds"])
    write_csv("expected_to_pay_nothing.csv", list(tables["expected"][0].keys()) if tables["expected"] else ["refused_kind"],
              tables["expected"])
    write_csv("runner_up_pairs.csv", ["winner", "runner_up", "takes", "share_of_takes", "winner_rate_median",
                                      "runner_up_rate_median"], tables["pairs"])
    write_csv("by_date.csv", ["date", "takes", "bots", "share_with_refusal", "share_with_runner_up", "share_only_offer",
                              *VETO_ORDER], tables["dates"])
    write_csv("sessions.csv", ["session", "first_take", "last_take", "takes", *["first_refusals_" + c for c in VETO_ORDER]],
              tables["sessions"])
    write_csv("census.csv", ["session", "last_census", *[key for key, _ in CENSUS]], tables["census"])
    write_csv("unchanged_expectations.csv", ["session", "refused_kind", "expected", "claim", "first_seen", "last_seen",
                                             "hours", "times", "bots", "bot_names"], tables["standing"])
    write_csv("bot_kind_top.csv", ["bot", "kind", "takes", "share_of_kind", "first_take", "last_take", "estimate_median",
                                   "rate_median", "new_median", "takes_with_new_0"], tables["bot_kind"])
    write_csv("overstated_alarms.csv", ["at", "state", "trade", "reads", "pays", "outcomes", "compared", "held_minutes",
                                        "say"], alarms)
    write_csv("overstated_by_trade.csv", list(tables["alarms_by_trade"][0].keys()) if tables["alarms_by_trade"] else ["trade"],
              tables["alarms_by_trade"])
    with open(os.path.join(DATA, "summary.json"), "w", encoding="utf-8", newline="\n") as handle:
        json.dump(doc, handle, ensure_ascii=False, indent=2)
        handle.write("\n")

    print(f"{tally.takes} takes parsed of {tally.lines_seen} take lines ({tally.unparsed} not understood); "
          f"{len(alarms)} overstated alarm lines; window {doc['window']['first_take']} to {doc['window']['last_take']}")
    print(f"self-check: {tally.rate_bad} rates and {tally.factor_bad} combined factors that do not add up")
    if not args.no_html:
        embed(doc, tables, alarms)


if __name__ == "__main__":
    main()
