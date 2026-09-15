#!/usr/bin/env python3
"""Assign stable, human-readable names to the communities in a graphify graph.

graphify's clusterer numbers communities arbitrarily -- the same corpus can
produce different integer IDs on different runs. Naming them by hand against
those IDs would therefore go stale the first time anyone regenerates the graph.

Instead every name here is a pure function of the community's *contents*:
its dominant source directory, plus its most-connected member when two
communities would otherwise collide. Regenerating the graph reproduces the
same names, so the wiki and report stay diffable across rebuilds.

Usage:
    python label_communities.py [--out graphify-out/.graphify_labels.json]

Reads graphify-out/graph.json and graphify-out/.graphify_analysis.json.
"""
from __future__ import annotations

import argparse
import collections
import json
from pathlib import Path

# Directories whose derived name would be unhelpfully generic or misleading.
# Keyed by dominant source directory, which is stable across rebuilds.
OVERRIDES = {
    "Projects/Server/Items": "Server Item Core",
    "Projects/Server/Mobiles": "Server Mobile Core",
    "Projects/Server/Collections": "Pooled Collections & Array Pools",
    "Projects/Server/Serialization": "Serialization Core",
    "Projects/Server/Network/Packets": "Server Network Packets",
    "Projects/Server/Json/Converters": "JSON Converters",
    "Projects/UOContent/Network/Packets": "Content Network Packets",
    "Projects/UOContent/Items/Weapons": "BaseWeapon Combat",
    "Projects/UOContent/Items/Weapons/Abilities": "Weapon Special Abilities",
    "Projects/UOContent/Multis/Houses": "Housing System",
    "Projects/UOContent/Engines/Craft/Core": "Crafting System Core",
    "Projects/UOContent/Engines/ML Quests/Definitions": "ML Quest Definitions",
    "Projects/UOContent/Mobiles/Vendors/SBInfo": "Vendor Stock Definitions",
    "Projects/UOContent/Mobiles/Abilities": "Monster Ability Framework",
    "Projects/UOContent/Commands": "Command Handlers",
    "Projects/UOContent/Gumps/Base": "Gump Builders",
    "Projects/UOContent/Spells/Base": "Spell Core",
}

PROJECTS = {
    "UOContent", "Server", "Server.Tests", "UOContent.Tests",
    "BuildTool", "Logger", "Application",
}


def dominant_dir(members, nodes):
    """Most common parent directory among a community's file-backed members."""
    dirs = collections.Counter()
    for m in members:
        node = nodes.get(m)
        if not node:
            continue
        src = (node.get("source_file") or "").replace("\\", "/")
        if "/" in src:
            dirs["/".join(src.split("/")[:-1])] += 1
    return dirs.most_common(1)[0][0] if dirs else None


def derive(path):
    """Turn 'Projects/UOContent/Items/Weapons' into 'Items / Weapons'."""
    parts = [p for p in path.split("/") if p and p != "Projects"]
    if parts and parts[0] in PROJECTS:
        project, rest = parts[0], parts[1:]
    else:
        project, rest = "", parts
    tail = " / ".join(rest[-2:]) if rest else project
    # Content lives in UOContent by default; name the other projects explicitly
    # so a test or engine community is never mistaken for game content.
    if project and project != "UOContent":
        tail = f"{project}: {tail}" if tail else project
    return tail or "Unclassified"


# Language primitives, ubiquitous BCL generics and test attributes. These are
# the most-connected node in many communities but say nothing about what the
# community *is*, so they never get to name one.
STOPLIST = {
    "bool", "byte", "sbyte", "char", "short", "ushort", "int", "uint", "long",
    "ulong", "float", "double", "decimal", "string", "object", "void", "var",
    "Type", "Types", "Array", "List", "IList", "Dictionary", "IDictionary",
    "HashSet", "IEnumerable", "ICollection", "IComparer", "IComparable",
    "Span", "ReadOnlySpan", "Memory", "Nullable", "Func", "Action", "Predicate",
    "DateTime", "TimeSpan", "Guid", "Exception", "Task", "Enum", "Struct",
    "Fact", "Theory", "InlineData", "MethodImpl", "Description", "Usage",
    "Aliases", "Serial", "T", "T1", "T2", "T3", "TKey", "TValue",
}


def hub(members, nodes, degree, scope=None):
    """Most-connected member that actually names the community.

    Prefers, in order: a type defined inside the community's own directory,
    any type outside the stoplist, then anything at all. Methods (labels
    starting with ".") are a last resort -- ".OnHit()" identifies a community
    far less well than "BaseWeapon" does.
    """
    ranked = sorted(members, key=lambda m: -degree[m])

    def candidates(in_scope, allow_stopped, allow_method):
        for m in ranked:
            node = nodes.get(m)
            if not node:
                continue
            label = (node.get("label") or "").strip()
            if not label:
                continue
            if not allow_method and label.startswith("."):
                continue
            if not allow_stopped and label.lstrip(".") in STOPLIST:
                continue
            if in_scope:
                src = (node.get("source_file") or "").replace("\\", "/")
                if not scope or not src.startswith(scope):
                    continue
            yield label

    for in_scope, allow_stopped, allow_method in (
        (True, False, False),    # a local type -- the best case
        (False, False, False),   # any meaningful type
        (True, False, True),     # a local method
        (False, True, True),     # anything, including primitives
    ):
        for label in candidates(in_scope, allow_stopped, allow_method):
            return label
    return None


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--graph", default="graphify-out/graph.json")
    ap.add_argument("--analysis", default="graphify-out/.graphify_analysis.json")
    ap.add_argument("--out", default="graphify-out/.graphify_labels.json")
    args = ap.parse_args()

    graph = json.loads(Path(args.graph).read_text(encoding="utf-8"))
    analysis = json.loads(Path(args.analysis).read_text(encoding="utf-8"))
    nodes = {n["id"]: n for n in graph["nodes"]}

    degree = collections.Counter()
    for link in graph["links"]:
        degree[link["source"]] += 1
        degree[link["target"]] += 1

    communities = analysis["communities"]

    # Pass 1: base name from the dominant directory.
    base = {}
    for cid, members in communities.items():
        path = dominant_dir(members, nodes)
        if path is None:
            label = hub(members, nodes, degree) or "Unclassified"
        else:
            label = OVERRIDES.get(path) or derive(path)
        base[cid] = label

    # Pass 2: several communities can share a directory. Disambiguate the
    # collisions with each one's hub so every name points somewhere distinct.
    counts = collections.Counter(base.values())
    labels = {}
    for cid, label in base.items():
        if counts[label] == 1:
            labels[cid] = label
            continue
        h = hub(communities[cid], nodes, degree,
                scope=dominant_dir(communities[cid], nodes))
        labels[cid] = f"{label} - {h}" if h else label

    Path(args.out).write_text(
        json.dumps(labels, indent=2, ensure_ascii=False), encoding="utf-8")

    distinct = len(set(labels.values()))
    print(f"Labelled {len(labels)} communities ({distinct} distinct) -> {args.out}")


if __name__ == "__main__":
    main()
