#!/usr/bin/env python3
"""Regenerate data/ for the economy investigation from a folder of shard session logs.

    python extract.py <logs-folder> [--out <folder>] [--until 2026-09-26T15:30] [--no-page]

The logs are the shard's own session files, one per process start:

    session-YYYY-MM-DD_HH-MM.log     lines like  [HH:MM:SS INF] text <s:Source>

What is read, and only what the shard printed itself:

  * the five-minute summary block written by BotBeat: the "The market:", "Trade:", "Money:", "City:",
    "Estate:", "Lessons:" and "The board:" lines. Their counters are running totals since the process
    started (the project's rule O2 in DECISIONS.md), so a window is the difference between two
    consecutive summaries and a session's total is its last summary. The stall, want, escrow, purse and
    treasury figures are snapshots and are copied as printed. A counter that goes down inside one process
    would mean the world was reset under it; the difference is then taken from zero (it never happened in
    the logs this was written against, and the "segments" column says so).
  * "The city bought N <thing> from <bot> for Ngp" lines. The city's purchases off the bots' stalls are
    booked by the market as ordinary sales (BotAuction.Purchase), so they are added up per window, in log
    order, and taken out of the market's turnover to leave what bots paid bots.
  * "N bots came back from the world save" at a boot, to tell a restart the bots survived.
  * want events for pieces of armour (opened, raised, filled, lowered, given up).

Outputs (money in gold pieces, gp; times are the shard's local time as written in the logs):

  data/windows.csv           one row per five-minute summary
  data/sessions.csv          one row per session log that printed at least one summary
  data/days.csv              sessions added up by the calendar day they started on
  data/wants_by_session.csv  per session and kind: wants opened, raised, filled, given up; the highest offer
                             per unit; coin put down, paid out on fills and taken back
  data/armour_wants.csv      every want event for a piece of armour
  data/summary.json          totals, peaks and checks quoted in README.md, and the series index.html draws

If index.html sits beside this script, its embedded data block is rewritten from summary.json as well
(pass --no-page to leave it alone). Only the Python standard library is used.
"""

import argparse
import csv
import datetime as dt
import json
import os
import re
import statistics
import sys
from collections import OrderedDict

LOG_NAME = re.compile(r"^session-(\d{4})-(\d{2})-(\d{2})_(\d{2})-(\d{2})\.log$")
LINE = re.compile(r"^\[(\d\d):(\d\d):(\d\d) ([A-Z]{3})\] (.*)$")

BEAT = "Server.BotAI.V2.BotBeat"
AUCTION = "Server.BotAI.V2.BotAuction"

# ---------------------------------------------------------------------------------------------------------
# The summary lines. Every field was checked against BotAuction.Describe, BotShops.Describe, BotPurse.Describe,
# BotCity.Describe, BotEstate.Describe, BotChest.Describe, BotToll.Describe and BotAuction.Board in the code.
# ---------------------------------------------------------------------------------------------------------

MARKET = re.compile(
    r"(?P<stalls>\d+) of (?P<stall_cap>\d+) stalls holding (?P<stall_things>\d+) things worth (?P<stall_worth_gp>\d+)gp"
    r" and (?P<wants>\d+) of (?P<want_cap>\d+) wants for (?P<want_things>\d+) things with (?P<escrow_gp>\d+)gp down;"
    r" (?P<sales>\d+) sales and (?P<fills>\d+) fills for (?P<turnover_gp>\d+)gp,"
    r" of which (?P<crossed_things>\d+) things went straight off a stall to a want on the board"
    r" and (?P<dear>\d+) wants found the thing on a stall dearer than they would pay;"
    r" (?P<raises>\d+) prices raised, (?P<cuts>\d+) cut, of which .*?, (?P<forgotten>\d+) forgotten,"
    r" (?P<wants_given_up>\d+) given up on;"
    r" (?P<refused_selling>\d+) orders refused to bots already selling the thing,"
    r" (?P<recalled>\d+) of them settled by taking it back off the stall"
    r" and (?P<unfunded>\d+) to bots that could not put the money down;"
    r" (?P<below_floor_things>\d+) things of (?P<condemned_kinds>\d+) kinds were worth less than the (?P<floor_gp>\d+)gp floor"
    r" and stayed in the pack \((?P<unpriced>\d+) of them because nothing could price them at all, which condemns no kind\);"
    r" the condemned kinds are (?P<condemned>[^;]*);"
    r" (?P<fetches>\d+) deliveries fetched off the board holding (?P<fetched_things>\d+) things;"
    r" the levy has taken (?P<levy_gp>\d+)gp over (?P<levy_sales>\d+) sales;"
    r" (?P<stuck_stalls>\d+) stalls have stood more than (?P<stuck_minutes>\d+) minutes holding (?P<stuck_things>\d+) things"
    r" at (?P<stuck_worth_gp>\d+)gp, the oldest for (?P<stuck_oldest_min>\d+) minutes;"
    r" (?P<taken_back_stalls>\d+) stalls were taken off the board at their lowest ask"
    r" and (?P<returned_things>\d+) things went back to their sellers, (?P<not_returned>\d+) could not be handed back"
)

TRADE = re.compile(
    r"(?P<shops_known>\d+) shopkeepers known from (?P<sweeps>\d+) sweeps;"
    r" (?P<npc_bought_things>\d+) things bought for (?P<npc_spent_gp>\d+)gp,"
    r" (?P<npc_sold_things>\d+) sold for (?P<npc_earned_gp>\d+)gp"
)

MONEY = re.compile(
    r"(?P<deposits>\d+) deposits worth (?P<deposited_gp>\d+)gp;"
    r" (?P<purses>\d+) purses that were earned: poorest (?P<purse_min_gp>\d+)gp, middling (?P<purse_median_gp>\d+)gp,"
    r" fattest (?P<purse_max_gp>\d+)gp held by (?P<purse_max_holder>.+?),"
    r" (?P<purses_total_gp>\d+)gp between them with (?P<pockets_gp>\d+)gp of it in pockets and (?P<accounts_gp>\d+)gp in accounts"
)

CITY = re.compile(
    r"the treasury holds (?P<city_purse_gp>\d+)gp of (?P<city_cap_gp>\d+)"
    r" \(minted (?P<city_minted_gp>\d+), taxed (?P<city_taxed_gp>\d+) off the guilds' ground,"
    r" spent (?P<city_spent_gp>\d+) on (?P<city_lots>\d+) lots and (?P<city_things>\d+) things,"
    r" (?P<city_order_things>\d+) of them against standing orders, (?P<city_orders_filled>\d+) orders filled,"
    r" (?P<city_refused>\d+) buys refused for want of coin;"
    r" (?P<bounties_ground>\d+) bounties on ground and (?P<bounties_heads>\d+) on heads paid, (?P<bounty_gp>\d+)gp in all,"
    r" (?P<bounties_lapsed>\d+) lapsed, (?P<blood_paid>\d+) murderers paid for at (?P<blood_gp>\d+)gp"
    r".*?\); (?P<errand_escrow_gp>\d+)gp held for the board's errands;"
    r".*? (?P<fairs>\d+) fairs held and (?P<fair_gp>\d+)gp spent at them"
)

ESTATE_PARTS = [
    re.compile(r"(?P<guild_levy_gp>\d+)gp levied off (?P<guild_levy_members>\d+) members"
               r" and (?P<crown_tax_gp>\d+)gp taken off (?P<crown_taxpayers>\d+) in tax"),
    re.compile(r"(?P<guild_counters>\d+) guild counters holding (?P<counter_lots>\d+) lots worth (?P<counter_worth_gp>\d+)gp"
               r" with (?P<counter_tills_gp>\d+)gp in their tills"),
    re.compile(r"(?P<counter_put_lots>\d+) lots put out for (?P<counter_put_gp>\d+)gp"),
    re.compile(r"(?P<counter_sold_lots>\d+) bought back off them for (?P<counter_sold_gp>\d+)gp,"
               r" (?P<counter_carried_gp>\d+)gp carried into the guilds and (?P<counter_wages_gp>\d+)gp paid in wages"),
    re.compile(r"(?P<ground_paid_gp>\d+)gp paid for ground"),
    re.compile(r"(?P<tithes>\d+) tithes of (?P<tithe_gp>\d+)gp paid to the guilds holding the ground hunted"
               r" \(\d+ % of the coin\), (?P<chest_draws>\d+) draws of (?P<chest_draw_gp>\d+)gp on the chests;"
               r" chests: (?P<chests>[^;]*)"),
    re.compile(r"tolls on hunting: (?P<tolls_posted>\d+) posted tolls of (?P<toll_posted_gp>\d+)gp at \d+ %,"
               r" (?P<tolls_told>\d+) of (?P<toll_told_gp>\d+)gp from strangers told at \d+ %"),
]

CHEST = re.compile(r"\s*(?P<guild>.+?) (?P<gp>\d+)gp\s*$")
LESSONS = re.compile(r"(?P<cooking_lessons>\d+) taught \((?P<cooking_lesson_points>[\d.]+) points for (?P<cooking_lessons_gp>\d+)gp\)")
BOARD = re.compile(r"^most wanted: (?P<board_most_wanted>.*?); most stocked: (?P<board_most_stocked>.*)$")

CITY_BOUGHT = re.compile(r"^The city bought (?P<units>\d+) (?P<label>.+?) from (?P<seller>.+?) for (?P<gp>\d+)gp, ")
RECLAIMED = re.compile(r"(\d+) bots came back from the world save")

# Want events. The opening line names the kind by its type (LeatherBustierArms); the others by its label
# (Leather Bustier Arms). Both are reduced to the type name by taking the spaces out.
WANT_OPEN = re.compile(r"^(?P<buyer>.+?) wants (?P<units>\d+) (?P<kind>[A-Za-z]+) and has put (?P<gp>\d+)gp down for them$")
WANT_RAISE = re.compile(r"^(?P<buyer>.+?) raised its offer for (?P<label>[A-Za-z ]+?) to (?P<offer>\d+)gp"
                        r" and put another (?P<gp>\d+)gp down after (?P<units>\d+) went unfilled$")
WANT_FILL = re.compile(r"^(?P<seller>.+?) filled (?P<buyer>.+?)'s want for (?P<units>\d+) (?P<label>[A-Za-z ]+?)"
                       r" and was paid (?P<gp>\d+)gp$")
WANT_LOWER = re.compile(r"^(?P<buyer>.+?) dropped its offer for (?P<label>[A-Za-z ]+?) to (?P<offer>\d+)gp"
                        r" after being filled again soon$")
WANT_GIVE_UP = re.compile(r"^(?P<buyer>.+?) gave up wanting (?P<label>[A-Za-z ]+?) at (?P<offer>\d+)gp"
                          r" of a possible (?P<ceiling>\d+) and took back (?P<gp>\d+)gp$")

ARMOUR = re.compile(r"^(?:(?:Leather|Studded|Ringmail|Chain|Plate|Bone|Female(?:Plate|Leather|Studded))[A-Z][A-Za-z]*"
                    r"|CloseHelm|Helmet|Bascinet|NorseHelm|OrcHelm)$")

# Running totals since the process started. Everything else in a summary is a snapshot.
CUMULATIVE = {
    "sales", "fills", "turnover_gp", "crossed_things", "dear", "raises", "cuts", "forgotten", "wants_given_up",
    "refused_selling", "recalled", "unfunded", "below_floor_things", "unpriced", "fetches", "fetched_things",
    "levy_gp", "levy_sales", "taken_back_stalls", "returned_things", "not_returned",
    "npc_bought_things", "npc_spent_gp", "npc_sold_things", "npc_earned_gp",
    "city_minted_gp", "city_taxed_gp", "city_spent_gp", "city_lots", "city_things", "bounty_gp", "blood_gp",
    "fairs", "fair_gp",
    "guild_levy_gp", "crown_tax_gp", "counter_put_gp", "counter_sold_gp", "counter_carried_gp", "counter_wages_gp",
    "ground_paid_gp", "tithes", "tithe_gp", "chest_draws", "chest_draw_gp", "toll_posted_gp", "toll_told_gp",
    "cooking_lessons", "cooking_lessons_gp",
}

SNAPSHOTS = ["stalls", "stall_things", "stall_worth_gp", "wants", "want_things", "escrow_gp", "stuck_stalls",
             "stuck_worth_gp", "city_purse_gp", "city_errand_escrow_gp", "purses", "purse_median_gp", "purse_max_gp",
             "purse_max_holder", "purses_total_gp", "pockets_gp", "accounts_gp", "chests_gp", "counter_tills_gp",
             "board_most_wanted", "board_most_stocked"]

DELTAS = ["sales", "fills", "turnover_gp", "crossed_things", "levy_gp", "wants_given_up", "taken_back_stalls",
          "below_floor_things", "npc_earned_gp", "npc_spent_gp", "city_minted_gp", "city_taxed_gp", "city_spent_gp",
          "fair_gp", "guild_levy_gp", "crown_tax_gp", "ground_paid_gp", "tithe_gp", "counter_sold_gp", "cooking_lessons_gp"]

WINDOW_COLUMNS = [
    "session", "time", "minutes_from_start", "window_min",
    # the board, as it stood
    "stalls", "stall_things", "stall_worth_gp", "wants", "want_things", "escrow_gp", "stuck_stalls", "stuck_worth_gp",
    # what the board did in the window
    "d_sales", "d_fills", "d_turnover_gp", "d_city_bought_gp", "d_bot_to_bot_gp", "d_crossed_things", "d_levy_gp",
    "d_wants_given_up", "d_taken_back_stalls", "d_below_floor_things",
    # the shopkeepers' counters
    "d_npc_earned_gp", "d_npc_spent_gp",
    # the city
    "city_purse_gp", "city_errand_escrow_gp", "d_city_minted_gp", "d_city_taxed_gp", "d_city_spent_gp", "d_fair_gp",
    # the purses, as they stood
    "purses", "purse_median_gp", "purse_max_gp", "purse_max_holder", "purses_total_gp", "pockets_gp", "accounts_gp",
    # guild money
    "d_guild_levy_gp", "d_crown_tax_gp", "d_ground_paid_gp", "d_tithe_gp", "d_toll_gp", "chests_gp",
    "counter_tills_gp", "d_counter_sold_gp",
    # cooking lessons bought from shopkeepers (BotTutor; the Lessons: line, printed from 24.09)
    "d_cooking_lessons_gp",
    # the six most wanted and most stocked kinds, in units
    "board_most_wanted", "board_most_stocked",
]

SESSION_TOTALS = [
    "sales", "fills", "turnover_gp", "city_bought_gp", "bot_to_bot_gp", "crossed_things", "levy_gp",
    "wants_given_up", "taken_back_stalls", "below_floor_things", "npc_earned_gp", "npc_spent_gp",
    "city_minted_gp", "city_taxed_gp", "city_spent_gp", "fair_gp", "guild_levy_gp", "crown_tax_gp", "ground_paid_gp",
    "tithe_gp", "toll_gp", "counter_sold_gp", "cooking_lessons_gp",
]

SESSION_END = ["stalls", "stall_things", "stall_worth_gp", "wants", "want_things", "escrow_gp", "purses",
               "purse_median_gp", "purse_max_gp", "purse_max_holder", "purses_total_gp", "pockets_gp", "accounts_gp",
               "city_purse_gp", "chests_gp", "counter_tills_gp"]

TEXT_FIELDS = ("condemned", "purse_max_holder", "chests", "board_most_wanted", "board_most_stocked")


def to_values(groups):
    out = {}
    for key, value in groups.items():
        if value is None:
            continue
        if key in TEXT_FIELDS:
            out[key] = value
        elif key == "cooking_lesson_points":
            out[key] = float(value)
        else:
            out[key] = int(value)
    return out


def chests_total(text):
    """'The Hammer 28gp, The Lantern 54gp' -> 82; 'none hold anything' -> 0."""
    if not text or "none hold" in text:
        return 0
    total = 0
    for part in text.split(","):
        m = CHEST.match(part)
        if m:
            total += int(m.group("gp"))
    return total


def iso(moment):
    return moment.strftime("%Y-%m-%dT%H:%M:%S")


class Session:
    """One session log: its summaries in order, what it said at boot, and when it began and ended."""

    def __init__(self, name):
        self.name = name
        self.log_start = None
        self.last_line = None
        self.summaries = []
        self.reclaimed = None


def read_session(path, name, date, until, armour_rows):
    session = Session(name)
    day = 0
    previous = None
    current = None
    city_since_last = 0

    with open(path, encoding="utf-8", errors="replace") as handle:
        for raw in handle:
            m = LINE.match(raw)
            if not m:
                continue
            hh, mm, ss, level, text = m.groups()
            seconds = int(hh) * 3600 + int(mm) * 60 + int(ss)
            # A session can run past midnight; the clock in the line is the only clock there is.
            if previous is not None and seconds < previous - 3600:
                day += 1
            previous = seconds
            moment = dt.datetime(date.year, date.month, date.day) + dt.timedelta(days=day, seconds=seconds)
            if until is not None and moment > until:
                break
            if session.log_start is None:
                session.log_start = moment
            session.last_line = moment

            if level != "INF":
                continue

            source = ""
            if text.endswith(">") and " <s:" in text:
                text, source = text.rsplit(" <s:", 1)
                source = source[:-1]

            if session.reclaimed is None:
                r = RECLAIMED.search(text)
                if r:
                    session.reclaimed = int(r.group(1))

            if source == BEAT:
                if text.startswith("The market: "):
                    parsed = MARKET.search(text)
                    if not parsed:
                        print(f"  unparsed market line in {name} at {iso(moment)}", file=sys.stderr)
                        current = None
                        continue
                    current = {"time": moment, "city_bought_gp": city_since_last}
                    city_since_last = 0
                    current.update(to_values(parsed.groupdict()))
                    session.summaries.append(current)
                    continue
                # The rest of the block follows the market line within the same second or two.
                if current is None or (moment - current["time"]).total_seconds() > 60:
                    continue
                if text.startswith("Trade: "):
                    parsed = TRADE.search(text)
                elif text.startswith("Money: "):
                    parsed = MONEY.search(text)
                elif text.startswith("City: "):
                    parsed = CITY.search(text)
                    if parsed:
                        values = to_values(parsed.groupdict())
                        values["city_errand_escrow_gp"] = values.pop("errand_escrow_gp")
                        current.update(values)
                    continue
                elif text.startswith("Lessons: "):
                    parsed = LESSONS.search(text)
                elif text.startswith("The board: "):
                    parsed = BOARD.search(text[len("The board: "):])
                elif text.startswith("Estate: "):
                    for part in ESTATE_PARTS:
                        found = part.search(text)
                        if found:
                            current.update(to_values(found.groupdict()))
                    if "chests" in current:
                        current["chests_gp"] = chests_total(current.pop("chests"))
                    continue
                else:
                    continue
                if parsed:
                    current.update(to_values(parsed.groupdict()))
                continue

            if source == AUCTION:
                bought = CITY_BOUGHT.match(text)
                if bought:
                    # Logged in the same call that books the sale, so log order places it exactly.
                    city_since_last += int(bought.group("gp"))
                    continue
                read_want_event(text, moment, name, armour_rows)

    return session


WANT_KINDS = {}   # (session, kind) -> tally, for data/wants_by_session.csv


def read_want_event(text, moment, session, rows):
    for event, pattern in (("open", WANT_OPEN), ("raise", WANT_RAISE), ("fill", WANT_FILL),
                           ("lower", WANT_LOWER), ("give_up", WANT_GIVE_UP)):
        m = pattern.match(text)
        if not m:
            continue
        g = m.groupdict()
        kind = g.get("kind") or g["label"].replace(" ", "")
        units = int(g.get("units") or 1)
        gp = int(g["gp"]) if g.get("gp") is not None else 0
        # An opening line and a fill carry the whole sum for all the units; a raise and a give-up name the offer.
        offer = gp // max(1, units) if event in ("open", "fill") else int(g["offer"])

        tally = WANT_KINDS.setdefault((session, kind), OrderedDict([
            ("session", session), ("kind", kind), ("opened", 0), ("raised", 0), ("filled", 0), ("lowered", 0),
            ("given_up", 0), ("highest_offer_gp", 0), ("highest_offer_at", ""), ("put_down_gp", 0),
            ("paid_on_fill_gp", 0), ("taken_back_gp", 0)]))
        tally[{"open": "opened", "raise": "raised", "fill": "filled", "lower": "lowered",
               "give_up": "given_up"}[event]] += 1
        if event in ("open", "raise"):
            tally["put_down_gp"] += gp
            if offer > tally["highest_offer_gp"]:
                tally["highest_offer_gp"] = offer
                tally["highest_offer_at"] = iso(moment)
        elif event == "fill":
            tally["paid_on_fill_gp"] += gp
        elif event == "give_up":
            tally["taken_back_gp"] += gp

        if ARMOUR.match(kind):
            rows.append({
                "time": iso(moment),
                "session": session,
                "event": event,
                "buyer": g["buyer"],
                "kind": kind,
                "units": units,
                "offer_gp": offer,
                "gp": gp if g.get("gp") is not None else "",
                "seller": g.get("seller", ""),
            })
        return


def windows_of(session):
    """A session's summaries as windows: snapshots as printed, running totals as differences."""
    rows = []
    previous = {}
    previous_time = session.log_start
    segments = 1
    for s in session.summaries:
        row = {"session": session.name, "time": iso(s["time"]),
               "minutes_from_start": round((s["time"] - session.log_start).total_seconds() / 60.0, 1),
               "window_min": round((s["time"] - previous_time).total_seconds() / 60.0, 1)}

        deltas = {}
        reset = False
        for key in CUMULATIVE:
            if key not in s:
                continue
            value = s[key]
            before = previous.get(key)
            if before is None or value < before:
                if before is not None and key in ("sales", "fills", "turnover_gp"):
                    reset = True
                deltas[key] = value
            else:
                deltas[key] = value - before
        if reset:
            segments += 1

        for key in SNAPSHOTS:
            row[key] = s.get(key, "")
        for key in DELTAS:
            row["d_" + key] = deltas.get(key, "")
        if "toll_posted_gp" in deltas or "toll_told_gp" in deltas:
            row["d_toll_gp"] = deltas.get("toll_posted_gp", 0) + deltas.get("toll_told_gp", 0)
        else:
            row["d_toll_gp"] = ""

        row["d_city_bought_gp"] = s["city_bought_gp"]
        row["d_bot_to_bot_gp"] = deltas.get("turnover_gp", 0) - s["city_bought_gp"]
        if row["d_bot_to_bot_gp"] < 0:
            print(f"  {session.name} {row['time']}: the city bought more than the market turned over", file=sys.stderr)

        rows.append(row)
        previous = {k: v for k, v in s.items() if k in CUMULATIVE}
        previous_time = s["time"]
    return rows, segments


def treasury_kept(summaries):
    """
    What the capped treasury kept of the tax paid into it between a session's first and last summary.

    Every change to the purse is one of: the mint (counted in "minted"), the tax (added only up to the cap),
    a payment (counted in "spent"), or coin moved between the purse and the errands' escrow (printed beside it).
    So the tax that stayed is the change in purse plus errand escrow, less what was minted, plus what was
    spent. Whatever was taxed beyond that met a full purse (BotCity.Tax adds at most Cap - Purse).
    """
    have = [s for s in summaries if "city_purse_gp" in s]
    if len(have) < 2:
        return "", ""
    a, b = have[0], have[-1]
    if b["city_minted_gp"] < a["city_minted_gp"] or b["city_spent_gp"] < a["city_spent_gp"]:
        return "", ""
    kept = ((b["city_purse_gp"] + b["city_errand_escrow_gp"]) - (a["city_purse_gp"] + a["city_errand_escrow_gp"])
            - (b["city_minted_gp"] - a["city_minted_gp"]) + (b["city_spent_gp"] - a["city_spent_gp"]))
    return b["city_taxed_gp"] - a["city_taxed_gp"], kept


def session_row(session, rows, segments):
    last = rows[-1]
    out = OrderedDict()
    out["session"] = session.name
    out["log_start"] = iso(session.log_start)
    out["last_summary"] = last["time"]
    out["minutes"] = round((dt.datetime.fromisoformat(last["time"]) - session.log_start).total_seconds() / 60.0, 1)
    out["summaries"] = len(rows)
    out["segments"] = segments
    out["bots_reclaimed_at_boot"] = session.reclaimed if session.reclaimed is not None else ""
    for key in SESSION_TOTALS:
        values = [r["d_" + key] for r in rows if r.get("d_" + key, "") != ""]
        out[key] = sum(values) if values else ""
    for key in SESSION_END:
        out["end_" + key] = last.get(key, "")
    peak = max(rows, key=lambda r: r["escrow_gp"])
    out["escrow_max_gp"] = peak["escrow_gp"]
    out["escrow_max_time"] = peak["time"]
    out["stall_worth_max_gp"] = max(r["stall_worth_gp"] for r in rows)
    purses = [r for r in rows if r["purse_max_gp"] != ""]
    top = max(purses, key=lambda r: r["purse_max_gp"]) if purses else None
    out["purse_max_peak_gp"] = top["purse_max_gp"] if top else ""
    out["purse_max_peak_holder"] = top["purse_max_holder"] if top else ""
    taxed, kept = treasury_kept(session.summaries)
    out["city_taxed_between_summaries_gp"] = taxed
    out["city_tax_kept_gp"] = kept
    out["city_summaries_at_cap"] = sum(1 for s in session.summaries
                                       if "city_purse_gp" in s and s["city_purse_gp"] >= s["city_cap_gp"])
    out["board_lost_at_restart"] = ""   # filled in once the next boot is known
    return out


def write_csv(path, rows, columns):
    with open(path, "w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=columns, extrasaction="ignore", lineterminator="\n")
        writer.writeheader()
        for row in rows:
            writer.writerow(row)


def by_day(sessions):
    days = OrderedDict()
    for s in sessions:
        day = s["log_start"][:10]
        d = days.setdefault(day, OrderedDict([("day", day), ("sessions", 0), ("minutes", 0.0)] +
                                             [(k, 0) for k in SESSION_TOTALS] +
                                             [("escrow_max_gp", 0), ("escrow_max_time", "")]))
        d["sessions"] += 1
        d["minutes"] = round(d["minutes"] + s["minutes"], 1)
        for k in SESSION_TOTALS:
            if s[k] != "":
                d[k] += s[k]
        if s["escrow_max_gp"] > d["escrow_max_gp"]:
            d["escrow_max_gp"] = s["escrow_max_gp"]
            d["escrow_max_time"] = s["escrow_max_time"]
    return list(days.values())


def armour_story(rows):
    """Per day: how far armour offers climbed and what the fills paid; the 26.09 burst in detail."""
    per_day = OrderedDict()
    for r in rows:
        day = r["time"][:10]
        d = per_day.setdefault(day, OrderedDict([("day", day), ("opened", 0), ("raised", 0), ("filled", 0),
                                                 ("given_up", 0), ("highest_offer_gp", 0), ("highest_offer_kind", ""),
                                                 ("highest_offer_at", ""), ("filled_gp", 0)]))
        if r["event"] == "open":
            d["opened"] += 1
        elif r["event"] == "raise":
            d["raised"] += 1
        elif r["event"] == "fill":
            d["filled"] += 1
            d["filled_gp"] += r["gp"]
        elif r["event"] == "give_up":
            d["given_up"] += 1
        if r["event"] in ("open", "raise") and r["offer_gp"] > d["highest_offer_gp"]:
            d["highest_offer_gp"] = r["offer_gp"]
            d["highest_offer_kind"] = r["kind"]
            d["highest_offer_at"] = r["time"]

    # Who was paid for the armour, by day: the three sellers paid most.
    sellers = OrderedDict()
    for r in rows:
        if r["event"] != "fill":
            continue
        day = sellers.setdefault(r["time"][:10], {})
        paid = day.setdefault(r["seller"], [0, 0])
        paid[0] += 1
        paid[1] += r["gp"]
    paid_by_day = [OrderedDict([("day", day), ("filled_gp", sum(v[1] for v in who.values())),
                                ("top_sellers", [OrderedDict([("seller", name), ("fills", v[0]), ("gp", v[1])])
                                                 for name, v in sorted(who.items(), key=lambda kv: -kv[1][1])[:3]])])
                   for day, who in sellers.items()]

    # 26.09.2026: the one tailor able to sew leather bought a sewing kit at 09:22:27 (DECISIONS.md, build 259).
    # What he filled in the ten minutes after it, read from the fill lines that name him.
    start, end = "2026-09-26T09:22:27", "2026-09-26T09:32:59"
    burst = [r for r in rows if r["event"] == "fill" and r["seller"] == "Faron" and start <= r["time"] <= end]
    day = "2026-09-26"
    points = [[r["time"][11:], r["kind"], r["offer_gp"], r["event"][0]] for r in rows
              if r["time"].startswith(day) and r["event"] in ("open", "raise", "fill")]
    return OrderedDict([
        ("by_day", list(per_day.values())),
        ("paid_by_day", paid_by_day),
        ("faron_fills_after_kit", OrderedDict([("from", start), ("to", end), ("fills", len(burst)),
                                               ("gp", sum(r["gp"] for r in burst)),
                                               ("highest_gp", max((r["gp"] for r in burst), default=0))])),
        ("points_2026_09_26", OrderedDict([("columns", ["time", "kind", "offer_gp", "event"]), ("rows", points)])),
    ])


def summarise(names, sessions, windows, armour, days, until):
    def total(key):
        return sum(s[key] for s in sessions if s[key] != "")

    minutes = sum(s["minutes"] for s in sessions)
    turnover, city_bought, bot_to_bot = total("turnover_gp"), total("city_bought_gp"), total("bot_to_bot_gp")
    earned, spent = total("npc_earned_gp"), total("npc_spent_gp")

    peak = max(windows, key=lambda r: r["escrow_gp"])
    at = {r["time"]: r for r in windows}
    escrow_peaks = []
    for s in sorted(sessions, key=lambda s: -s["escrow_max_gp"])[:6]:
        w = at[s["escrow_max_time"]]
        # The session hour by hour (twelve summaries at a time): what bots paid bots per hour, and the escrow at the end.
        own = [r for r in windows if r["session"] == s["session"]]
        hours = []
        for i in range(0, len(own), 12):
            block = own[i:i + 12]
            span = sum(r["window_min"] for r in block)
            hours.append(OrderedDict([("from", block[0]["time"]), ("to", block[-1]["time"]),
                                      ("bot_to_bot_gp_per_hour", round(sum(r["d_bot_to_bot_gp"] for r in block) / span * 60)),
                                      ("escrow_at_end_gp", block[-1]["escrow_gp"])]))
        escrow_peaks.append(OrderedDict([
            ("session", s["session"]), ("session_minutes", s["minutes"]), ("escrow_max_gp", s["escrow_max_gp"]),
            ("at", s["escrow_max_time"]), ("wants", w["wants"]), ("want_things", w["want_things"]),
            ("purses", w["purses"]), ("purses_total_gp", w["purses_total_gp"]),
            ("board_most_wanted", w["board_most_wanted"]), ("by_hour", hours)]))
    long_runs = [s for s in sessions if s["minutes"] >= 120]
    escrow_and_length = OrderedDict([
        ("sessions_of_two_hours_or_more", [OrderedDict([("session", s["session"]), ("minutes", s["minutes"]),
                                                        ("escrow_max_gp", s["escrow_max_gp"])]) for s in long_runs]),
        ("highest_escrow_in_shorter_sessions_gp", max((s["escrow_max_gp"] for s in sessions if s["minutes"] < 120),
                                                      default=0)),
    ])

    # The board is not written to the save (DECISIONS.md section 2.8, BotAuction/README.md). Since build 163 the
    # bots are, so what stood on the board at a restart the bots came back from is money and goods they lost.
    lost = [s for s in sessions if s["board_lost_at_restart"] == "yes"]
    restarts = OrderedDict([
        ("restarts_bots_survived", len(lost)),
        ("escrow_at_last_summary_gp", sum(s["end_escrow_gp"] for s in lost)),
        ("stall_worth_at_last_summary_gp", sum(s["end_stall_worth_gp"] for s in lost)),
        ("largest", [OrderedDict([("session", s["session"]), ("last_summary", s["last_summary"]),
                                  ("escrow_gp", s["end_escrow_gp"]), ("stall_worth_gp", s["end_stall_worth_gp"])])
                     for s in sorted(lost, key=lambda s: -s["end_escrow_gp"])[:6]]),
    ])

    taxed = sum(s["city_taxed_between_summaries_gp"] for s in sessions if s["city_taxed_between_summaries_gp"] != "")
    kept = sum(s["city_tax_kept_gp"] for s in sessions if s["city_tax_kept_gp"] != "")
    at_cap = sum(s["city_summaries_at_cap"] for s in sessions)

    purse_rows = [r for r in windows if r["purse_max_gp"] != "" and r["purses_total_gp"]]
    richest = max(purse_rows, key=lambda r: r["purse_max_gp"])
    share = max(purse_rows, key=lambda r: r["purse_max_gp"] / r["purses_total_gp"])
    architect = [r for r in purse_rows if r["purse_max_holder"].endswith(" the Architect")]
    architect_share = [r["purse_max_gp"] / r["purses_total_gp"] for r in architect]

    # Which wants climbed highest, day by day: the five kinds whose offer per unit went highest.
    by_day_kind = OrderedDict()
    for (session, kind), t in WANT_KINDS.items():
        if not t["highest_offer_at"]:
            continue
        day = by_day_kind.setdefault(t["highest_offer_at"][:10], {})
        best = day.get(kind)
        if best is None or t["highest_offer_gp"] > best["highest_offer_gp"]:
            day[kind] = {"kind": kind, "highest_offer_gp": t["highest_offer_gp"], "at": t["highest_offer_at"]}
        day[kind]["put_down_gp"] = day[kind].get("put_down_gp", 0) + t["put_down_gp"]
    highest_offers = [OrderedDict([("day", day), ("kinds", sorted(kinds.values(), key=lambda k: -k["highest_offer_gp"])[:5])])
                      for day, kinds in sorted(by_day_kind.items())]

    series_columns = ["time", "session", "window_min", "d_sales", "d_fills", "d_bot_to_bot_gp", "d_city_bought_gp",
                      "d_npc_earned_gp", "d_npc_spent_gp", "escrow_gp", "wants", "stall_worth_gp", "stall_things",
                      "purses_total_gp", "pockets_gp", "accounts_gp", "purse_max_gp", "purse_max_holder",
                      "purse_median_gp", "d_levy_gp", "city_purse_gp"]

    return OrderedDict([
        ("generated_from", OrderedDict([
            ("session_logs", len(names)), ("sessions_with_summaries", len(sessions)), ("summaries", len(windows)),
            ("until", until), ("first_summary", windows[0]["time"]), ("last_summary", windows[-1]["time"]),
            ("shard_hours_covered", round(minutes / 60.0, 1))])),
        ("totals_gp", OrderedDict([
            ("market_turnover", turnover),
            ("city_purchases_booked_as_sales", city_bought),
            ("bot_to_bot", bot_to_bot),
            ("npc_counters_paid_bots", earned),
            ("bots_paid_npc_counters", spent),
            ("levy_to_architect", total("levy_gp")),
            ("city_minted", total("city_minted_gp")),
            ("city_taxed", total("city_taxed_gp")),
            ("city_spent", total("city_spent_gp")),
            ("city_spent_at_fairs", total("fair_gp")),
            ("guild_levies", total("guild_levy_gp")),
            ("guild_ground_paid", total("ground_paid_gp")),
            ("crown_tax", total("crown_tax_gp")),
            ("tithes", total("tithe_gp")),
            ("tolls", total("toll_gp")),
            ("guild_counter_sales", total("counter_sold_gp")),
            ("cooking_lessons_paid_to_shopkeepers", total("cooking_lessons_gp")),
        ])),
        ("totals_count", OrderedDict([
            ("sales", total("sales")), ("fills", total("fills")), ("crossed_things", total("crossed_things")),
            ("wants_given_up", total("wants_given_up")), ("stalls_taken_back", total("taken_back_stalls")),
            ("below_floor_things", total("below_floor_things")),
        ])),
        ("shares", OrderedDict([
            ("bot_to_bot_of_the_three_flows", round(bot_to_bot / (bot_to_bot + earned + spent), 4)),
            ("city_of_market_turnover", round(city_bought / turnover, 4)),
        ])),
        ("escrow", OrderedDict([
            ("peak_gp", peak["escrow_gp"]), ("peak_at", peak["time"]), ("peak_wants", peak["wants"]),
            ("peak_purses_total_gp", peak["purses_total_gp"]), ("largest_by_session", escrow_peaks),
            ("against_session_length", escrow_and_length),
            ("highest_offers_by_day", highest_offers),
        ])),
        ("restarts", restarts),
        ("treasury_cap", OrderedDict([
            ("taxed_between_summaries_gp", taxed), ("kept_gp", kept), ("lost_at_cap_gp", taxed - kept),
            ("summaries_at_cap", at_cap), ("summaries", len(windows)),
        ])),
        ("purses", OrderedDict([
            ("largest_purse_gp", richest["purse_max_gp"]), ("largest_purse_holder", richest["purse_max_holder"]),
            ("largest_purse_at", richest["time"]), ("largest_purse_total_of_all_gp", richest["purses_total_gp"]),
            ("largest_share", round(share["purse_max_gp"] / share["purses_total_gp"], 4)),
            ("largest_share_holder", share["purse_max_holder"]), ("largest_share_at", share["time"]),
            ("largest_share_purse_gp", share["purse_max_gp"]), ("largest_share_total_gp", share["purses_total_gp"]),
            ("summaries_with_purses", len(purse_rows)),
            ("summaries_architect_richest", len(architect)),
            ("architect_median_share", round(statistics.median(architect_share), 4) if architect_share else None),
        ])),
        ("armour_wants", armour_story(armour)),
        ("days", days),
        ("series", OrderedDict([("columns", series_columns),
                                ("rows", [[r[c] for c in series_columns] for r in windows])])),
    ])


PAGE_DATA = re.compile(r'(<script id="economy-data" type="application/json">)(.*?)(</script>)', re.S)


def refresh_page(folder, summary):
    page = os.path.join(folder, "index.html")
    if not os.path.exists(page):
        return False
    with open(page, encoding="utf-8") as handle:
        html = handle.read()
    blob = json.dumps(summary, separators=(",", ":"), ensure_ascii=True).replace("</", "<\\/")
    if not PAGE_DATA.search(html):
        print("  index.html has no economy-data block; left alone", file=sys.stderr)
        return False
    html = PAGE_DATA.sub(lambda m: m.group(1) + blob + m.group(3), html, count=1)
    with open(page, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(html)
    return True


def main():
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("logs", help="folder holding session-*.log files")
    parser.add_argument("--out", default=os.path.join(os.path.dirname(os.path.abspath(__file__)), "data"),
                        help="where to write the data files (default: data/ beside this script)")
    parser.add_argument("--until", default=None,
                        help="ignore every line after this local time, e.g. 2026-09-26T15:30; a running shard's logs "
                             "keep growing, and the README's numbers were made with a fixed cut")
    parser.add_argument("--no-page", action="store_true", help="do not rewrite the data block in index.html")
    args = parser.parse_args()

    until = dt.datetime.fromisoformat(args.until) if args.until else None
    names = sorted(n for n in os.listdir(args.logs) if LOG_NAME.match(n))
    if not names:
        sys.exit(f"no session-*.log files in {args.logs}")
    os.makedirs(args.out, exist_ok=True)

    windows, sessions, armour, boots = [], [], [], []
    used = []
    for name in names:
        y, mo, d, h, mi = map(int, LOG_NAME.match(name).groups())
        if until is not None and dt.datetime(y, mo, d, h, mi) > until:
            continue
        used.append(name)
        session = read_session(os.path.join(args.logs, name), name[len("session-"):-len(".log")],
                               dt.date(y, mo, d), until, armour)
        boots.append((session.name, session.reclaimed))
        if not session.summaries:
            continue
        rows, segments = windows_of(session)
        windows.extend(rows)
        sessions.append(session_row(session, rows, segments))

    # A session's board was lost with its bots surviving when the next boot that says anything about the save
    # brought bots back. The last session in the logs has no next boot and is left blank.
    order = [n for n, _ in boots]
    for s in sessions:
        after = boots[order.index(s["session"]) + 1:]
        said = [r for _, r in after if r is not None]
        if said:
            s["board_lost_at_restart"] = "yes" if said[0] > 0 else "no"

    write_csv(os.path.join(args.out, "windows.csv"), windows, WINDOW_COLUMNS)
    write_csv(os.path.join(args.out, "sessions.csv"), sessions, list(sessions[0].keys()))
    write_csv(os.path.join(args.out, "armour_wants.csv"), armour,
              ["time", "session", "event", "buyer", "kind", "units", "offer_gp", "gp", "seller"])
    kinds = sorted(WANT_KINDS.values(), key=lambda t: (t["session"], -t["put_down_gp"], t["kind"]))
    write_csv(os.path.join(args.out, "wants_by_session.csv"), kinds, list(kinds[0].keys()))
    days = by_day(sessions)
    write_csv(os.path.join(args.out, "days.csv"), days, list(days[0].keys()))

    summary = summarise(used, sessions, windows, armour, days, args.until)
    with open(os.path.join(args.out, "summary.json"), "w", encoding="utf-8", newline="\n") as handle:
        json.dump(summary, handle, indent=1, ensure_ascii=True)
        handle.write("\n")

    refreshed = False if args.no_page else refresh_page(os.path.dirname(os.path.abspath(__file__)), summary)
    print(f"{len(used)} session logs read; wrote {len(windows)} windows, {len(sessions)} sessions, {len(days)} days, "
          f"{len(armour)} armour want events to {args.out}" + ("; index.html refreshed" if refreshed else ""),
          file=sys.stderr)


if __name__ == "__main__":
    main()
