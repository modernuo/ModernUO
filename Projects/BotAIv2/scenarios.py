"""Scenario checks through Argus's door, run after every build (DECISIONS.md, section 5, S2).

Each scenario puts the running shard into a known state with a hand verb and checks that the lines which answer
that state appear in the session log before a deadline. A fixed defect becomes a scenario, so that a build which
brings it back fails here instead of hours later in the population.

Run from the repository root while the shard is up:

    python Projects/BotAIv2/scenarios.py              every quick scenario
    python Projects/BotAIv2/scenarios.py breaker jam  the named ones
    python Projects/BotAIv2/scenarios.py --slow       the quick ones and those that take minutes

The exit code is the number of scenarios that failed.
"""

import re
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DOOR_IN = ROOT / "Distribution" / "argus-in.txt"
DOOR_OUT = ROOT / "Distribution" / "argus-out.txt"
LOGS = ROOT / "logs"


def ask(line, timeout=40.0):
    """Writes one line into the door and returns what was answered to it."""
    start = DOOR_OUT.stat().st_size if DOOR_OUT.exists() else 0
    with DOOR_IN.open("a", encoding="utf-8") as door:
        door.write(line + "\n")
    deadline = time.time() + timeout
    while time.time() < deadline:
        time.sleep(1.0)
        answered = DOOR_OUT.exists() and DOOR_OUT.stat().st_size > start
        emptied = not DOOR_IN.exists() or DOOR_IN.stat().st_size == 0
        if answered and emptied:
            time.sleep(1.5)
            with DOOR_OUT.open("rb") as out:
                out.seek(start)
                return out.read().decode("utf-8", errors="replace").strip()
    return ""


def newest_log():
    logs = sorted(LOGS.glob("session-*.log"), key=lambda p: p.stat().st_mtime)
    return logs[-1] if logs else None


def log_end(log):
    return log.stat().st_size


def wait_for(log, since, pattern, timeout):
    """The first log line after byte offset `since` matching `pattern` within `timeout` seconds, or None."""
    rx = re.compile(pattern)
    deadline = time.time() + timeout
    offset = since
    tail = b""
    while time.time() < deadline:
        with log.open("rb") as f:
            f.seek(offset)
            chunk = f.read()
        if chunk:
            offset += len(chunk)
            text = (tail + chunk).decode("utf-8", errors="replace")
            lines = text.split("\n")
            tail = lines[-1].encode("utf-8")
            for line in lines[:-1]:
                if rx.search(line):
                    return line
        time.sleep(1.0)
    return None


def recent_bot(log, pattern, lookback=400_000):
    """The name of the most recent bot whose line matches `pattern` (a regex with one group for the name)."""
    size = log_end(log)
    with log.open("rb") as f:
        f.seek(max(0, size - lookback))
        text = f.read().decode("utf-8", errors="replace")
    names = re.findall(pattern, text)
    return names[-1] if names else None


def scenario_door(log):
    answer = ask("state")
    return bool(answer), (answer.splitlines()[0][:160] if answer else "the door gave no answer in 40 s")


def scenario_breaker(log):
    bot = recent_bot(log, r"\] ([A-Z][a-z]+(?: [A-Z][a-z]+)?) took on [a-z]+: ")
    if bot is None:
        return False, "no bot has taken on any work lately"
    since = log_end(log)
    answer = ask(f"do trip {bot} glean")
    if "rests" not in answer:
        return False, f"trip answered: {answer[:200]}"
    line = wait_for(log, since, rf"{re.escape(bot)} failed at glean \d+ times in \d+s for the same reason .* not offered to it", 15)
    if line is None:
        return False, f"no trip line for {bot} within 15 s; the verb answered: {answer[:160]}"
    listing = ask("do breaks")
    if bot not in listing or "glean" not in listing:
        return False, f"breaks does not list {bot}'s glean: {listing[:200]}"
    return True, f"{bot}: tripped, logged and listed"


def scenario_jam(log):
    bot = recent_bot(log, r"\] ([A-Z][a-z]+(?: [A-Z][a-z]+)?) took on (?:peddle|restock|sew|brew|fletch|inscribe): ")
    if bot is None:
        return False, "no bot has taken on counter work lately"
    since = log_end(log)
    answer = ask(f"do jam {bot}")
    if "room for a coin: False" not in answer:
        return False, f"jam answered: {answer[:200]}"
    taken = wait_for(log, since, rf"\] {re.escape(bot)} took on unload: ", 120)
    if taken is None:
        return False, f"{bot} was jammed and did not take on unload within 120 s"
    finished = wait_for(log, since, rf"\] {re.escape(bot)} finished unload: ", 300)
    if finished is None:
        return False, f"{bot} took on unload and did not finish it within 300 s"
    after = ask(f"do pack {bot}")
    reason = finished.split(" — ", 1)[1].split(" <s:", 1)[0] if " — " in finished else finished[-120:]
    return True, f"{bot}: jammed and unloaded ({reason}); pack now: {after[:120]}"


# Where Winug stood "shut in" on 14.09.2026, and the pathfinder answered "Sealed ... the far side says Enclosed" from the
# population's home. The hearth tile the barred box was written about, (2161, 1354), answered only "Partial", and a bot
# put on (2161, 1356) simply walked away: a pocket is taken on the pathfinder's word, asked again before every run.
POCKET = (2163, 1384)
HOME = (1440, 1470)


def recent_bots(log, pattern, lookback=400_000):
    """Every distinct bot name matching `pattern`, most recent first."""
    size = log_end(log)
    with log.open("rb") as f:
        f.seek(max(0, size - lookback))
        text = f.read().decode("utf-8", errors="replace")
    seen = []
    for name in reversed(re.findall(pattern, text)):
        if name not in seen:
            seen.append(name)
    return seen


def whereabouts(bot):
    """The bot's tile from the door's `where`, or None."""
    answer = ask(f"do where {bot}")
    m = re.search(rf"{re.escape(bot)} at \((\d+), (\d+), (-?\d+)\)", answer)
    return (int(m.group(1)), int(m.group(2))) if m else None


def scenario_stall(log):
    # Not free: the bot put in the pocket fails its homeward and hunts "no way through" until it is carried out, and those
    # failures land in whatever build window is being measured (Gerda Ashdown on build 42: eleven hunts, walks home and a
    # sale "no way through" in five minutes). Run it once a night rather than on every build, or discount the bot it names.
    #
    # The tele verb refuses anything further than 600 tiles, and the first version of this scenario read its refusal as
    # success and waited seven minutes for a bot that had never moved. So the bot is chosen near enough, and the answer
    # is read before anything is waited for.
    # "Sealed" needs the reach ledger, which starts empty after every boot; the far side's own look does not, and on a
    # fresh build it answered "Partial, 695 tiles ... the far side says Enclosed" for the same tile. Either is proof.
    proof = ask(f"do road {HOME[0]} {HOME[1]} {POCKET[0]} {POCKET[1]}", timeout=60)
    if "Sealed" not in proof and "far side says Enclosed" not in proof:
        return False, f"the pocket tile is not proven sealed, so nothing was moved: {proof[:200]}"
    bot = None
    for name in recent_bots(log, r"\] ([A-Z][a-z]+(?: [A-Z][a-z]+)?) took on (?:prowl|hunt|mine|chop|herbs|cook): ")[:8]:
        at = whereabouts(name)
        if at and max(abs(at[0] - POCKET[0]), abs(at[1] - POCKET[1])) <= 550:
            bot = name
            break
    if bot is None:
        return False, "no bot doing field work lately stands within 550 tiles of the pocket"
    since = log_end(log)
    answer = ask(f"do tele {bot} {POCKET[0]} {POCKET[1]}")
    if not answer or re.search(r"will not|no body fits|cannot|there is no bot", answer):
        return False, f"tele refused: {answer[:200]}"
    # A pocket this size is walked about in rather than stood still in, so either answer to it counts: the stall watch
    # with the road it read, or the population carrying the bot out after its roads are refused.
    line = wait_for(
        log,
        since,
        rf"\] {re.escape(bot)} the \w+ (has not moved or changed what it is doing .*(the road holds|no errand on the road).*mounted .*casting |could get nowhere at all from .* carried home)",
        420,
    )
    if line is None:
        return False, f"{bot} was put in the walled pocket east of Britain and neither a stall line nor a rescue came within 7 min"
    return True, f"{bot}: {line[line.find(bot):][:220]}"


def scenario_scrolls(log):
    # Build 42: the armoury topped a pack up to three attack scrolls, the keep list held none, and the next counter sold
    # them back - 471 harm scrolls stocked in one night. A bot that stocked lately must carry what it bought as kept.
    # Needs some minutes after a boot, since nobody has stocked anything before then.
    stocked = re.findall(
        r"\] ([A-Z][a-z]+(?: [A-Z][a-z]+)?) finished acquire: .*— stocked ([A-Za-z]+) for \d+gp",
        newest_text(log),
    )
    seen = []
    for name, kind in reversed(stocked):
        if name not in [s[0] for s in seen]:
            seen.append((name, kind))
    if not seen:
        return False, "no bot has stocked an attack scroll lately"
    for name, kind in seen[:6]:
        pack = ask(f"do pack {name}")
        m = re.search(rf"{kind} x(\d+) \((kept without limit|keeps (\d+)(?:, (\d+) surplus)?)\)", pack)
        if m is None:
            continue
        if m.group(3) == "0":
            return False, f"{name} carries {kind} x{m.group(1)} and keeps none of it: {pack[:200]}"
        return True, f"{name}: {kind} x{m.group(1)} ({m.group(2)})"
    return False, f"none of {', '.join(s[0] for s in seen[:6])} was carrying the scroll it stocked when asked"


def scenario_rewield(log):
    # Build 55, Patrick's order of 15.09.2026: a bot wields a better weapon of its own kind and keeps the bound one in its
    # pack. After a boot nobody carries one, so the hand verb makes the state: a vanquishing copy of the bot's own weapon.
    bot = recent_bot(log, r"\] ([A-Z][a-z]+(?: [A-Z][a-z]+)?) took on (?:hunt|band|prowl): ")
    if bot is None:
        return False, "no fighter has taken on a hunt, a band or a prowl lately"
    since = log_end(log)
    answer = ask(f"do arm {bot}")
    if "vanquishing" not in answer:
        return False, f"arm answered: {answer[:200]}"
    line = wait_for(log, since, rf"{re.escape(bot)} put \w+ away for \w+, (?:which lands|worth)", 45)
    if line is None:
        return False, f"no swap for {bot} within 45 s; arm answered: {answer[:160]}"
    return True, f"{bot}: {line[line.find(bot):][:200]}"


def newest_text(log, lookback=400_000):
    size = log_end(log)
    with log.open("rb") as f:
        f.seek(max(0, size - lookback))
        return f.read().decode("utf-8", errors="replace")


QUICK = {"door": scenario_door, "breaker": scenario_breaker}
SLOW = {"jam": scenario_jam, "stall": scenario_stall, "scrolls": scenario_scrolls, "rewield": scenario_rewield}

# Named so that they are not mistaken for passing: states no hand verb can make yet.
UNSCRIPTED = {"lethal": "bots dying in one place (BotPeril.Lethal) - no verb makes deaths"}


def main(argv):
    slow = "--slow" in argv
    names = [a for a in argv if not a.startswith("--")]
    table = {**QUICK, **SLOW}
    chosen = names or list(QUICK) + (list(SLOW) if slow else [])
    log = newest_log()
    if log is None:
        print("no session log")
        return 1
    failed = 0
    for name in chosen:
        run = table.get(name)
        if run is None:
            print(f"?    {name} — no such scenario")
            failed += 1
            continue
        started = time.time()
        ok, detail = run(log)
        failed += 0 if ok else 1
        print(f"{'PASS' if ok else 'FAIL'} {name} ({time.time() - started:.0f}s) — {detail}")
    for name, why in UNSCRIPTED.items():
        print(f"---- {name} — not scripted: {why}")
    return failed


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
