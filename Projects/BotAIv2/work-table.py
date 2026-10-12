"""The README's work table, from the shard's session logs.

Usage:
    python work-table.py <logs folder> [<first session name, e.g. session-2026-09-25_17-27.log>] [--most 1000] [--markdown]

--markdown prints the table as the README has it: what each kind of work is, kinds taken on fewer than --rare times
(20) folded into one row, and the pooled row last.

Every "X took on KIND" line is paired with the line that ended it for the same bot and kind: "finished", "failed
at", "dropped" or "died doing". A take that never ended (the shard was stopped under it) is left out. For each kind the
most recent takes that ended are counted, up to --most, and the share finished is finished / taken on. The last row
pools every kind the same way.
"""

import collections
import glob
import io
import os
import re
import sys

WHAT = {
    "order": "putting money down on the market for what it cannot make",
    "restock": "buying what its class must carry — bandages, bottles, arrows, reagents, tools",
    "prowl": "walking out to ground where there is something to fight",
    "unload": "taking a full pack to a stall or a counter",
    "hunt": "killing a creature for its loot and hide",
    "mine": "digging ore",
    "flee": "running from a fight it is losing",
    "chop": "felling trees",
    "peddle": "selling goods to an NPC shopkeeper",
    "acquire": "buying off the market",
    "rescue": "going to the help of a bot under attack",
    "mend": "healing itself or another bot",
    "rally": "joining its guild's war company",
    "ward": "telling a stranger hunting the guild's land about the toll",
    "cook": "cooking meat at a fire",
    "forge": "smithing weapons and armour",
    "tinker": "making tools out of iron for the other trades",
    "bowyer": "making bows and crossbows out of logs",
    "weave": "shearing sheep, spinning, weaving and cutting cloth and bandages",
    "supply": "stocking its guild's counter from the shops",
    "herbs": "picking reagents",
    "pickings": "going through a corpse",
    "glean": "picking up its spent arrows",
    "sew": "tailoring leather and cloth",
    "brew": "brewing potions",
    "stake": "standing on a square its guild is claiming",
    "forage": "picking up reagents lying about",
    "delve": "going down into a dungeon as a party",
    "venture": "walking into a dungeon alone by its mouth",
    "travel": "walking to another town",
    "lodge": "walking to an inn to rest",
    "tame": "bringing a beast to heel",
    "reclaim": "going back to its own corpse for its things",
    "homeward": "walking home",
    "inscribe": "writing spell scrolls",
    "quarrel": "fighting a member of a guild it is at war with",
    "evict": "moving a stranger out of its guild's hall",
    "fletch": "making arrows and bows",
    "band": "calling a company together for something one bot cannot take",
    "enlist": "joining a Captain's company",
    "escort": "seeing another bot safely somewhere",
    "tutor": "teaching a class for a fee",
    "harrow": "a guild's or the Baron's company clearing a deadly square",
    "scout": "walking ground nobody has looked at",
    "housecall": "a healer going to a bot that sent for it",
}

TOOK = re.compile(r"^\[\d\d:\d\d:\d\d INF\] (.+?) took on ([a-z-]+): ")
ENDED = re.compile(r"^\[\d\d:\d\d:\d\d INF\] (.+?) (finished|failed at|dropped|died doing) ([a-z-]+): ")


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    most = 1000
    rare = 20
    markdown = "--markdown" in sys.argv

    if "--most" in sys.argv:
        most = int(sys.argv[sys.argv.index("--most") + 1])
        args = [a for a in args if a != str(most)]

    if "--rare" in sys.argv:
        rare = int(sys.argv[sys.argv.index("--rare") + 1])
        args = [a for a in args if a != str(rare)]

    folder = args[0]
    first = args[1] if len(args) > 1 else ""
    logs = sorted(p for p in glob.glob(os.path.join(folder, "session-*.log")) if os.path.basename(p) >= first)

    # Per kind, the outcomes of its takes in the order they were taken.
    outcomes = collections.defaultdict(list)

    for path in logs:
        # Open takes of this session only: a take the restart cut off never ends and is not counted.
        open_takes = collections.defaultdict(collections.deque)

        with io.open(path, encoding="utf-8", errors="replace") as f:
            for line in f:
                m = TOOK.match(line)

                if m:
                    bot, kind = m.group(1), m.group(2)
                    slot = [kind, None]
                    outcomes[kind].append(slot)
                    open_takes[(bot, kind)].append(slot)
                    continue

                m = ENDED.match(line)

                if m:
                    bot, how, kind = m.group(1), m.group(2), m.group(3)
                    queue = open_takes.get((bot, kind))

                    if queue:
                        queue.popleft()[1] = how

    rows = []
    pooled_taken = 0
    pooled_finished = 0

    for kind, slots in outcomes.items():
        ended = [how for _, how in slots if how is not None][-most:]

        if not ended:
            continue

        finished = sum(1 for how in ended if how == "finished")
        rows.append((kind, len(ended), finished))
        pooled_taken += len(ended)
        pooled_finished += finished

    rows.sort(key=lambda r: -r[1])

    if markdown:
        print("| Work | What it is | Taken on | Finished | Finished share |")
        print("|---|---|---:|---:|---:|")
        other = [r for r in rows if r[1] < rare or r[0] not in WHAT]
        for kind, taken, finished in rows:
            if (kind, taken, finished) in other:
                continue
            print(f"| {kind} | {WHAT[kind]} | {taken:,} | {finished:,} | {100.0 * finished / taken:.0f} % |")
        if other:
            taken = sum(r[1] for r in other)
            finished = sum(r[2] for r in other)
            print(f"| other | {len(other)} rarer kinds: {', '.join(r[0] for r in other)} | {taken:,} | {finished:,} | "
                  f"{100.0 * finished / taken:.0f} % |")
        print(f"| **all work** | | **{pooled_taken:,}** | **{pooled_finished:,}** | "
              f"**{(100.0 * pooled_finished / pooled_taken) if pooled_taken else 0.0:.0f} %** |")
        return

    print(f"{len(logs)} sessions, {len(rows)} kinds")
    for kind, taken, finished in rows:
        print(f"{kind}\t{taken}\t{finished}\t{100.0 * finished / taken:.0f}")

    print(f"ALL\t{pooled_taken}\t{pooled_finished}\t{100.0 * pooled_finished / pooled_taken:.0f}")


if __name__ == "__main__":
    main()
