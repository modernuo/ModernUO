# Knowledge Graph

A queryable map of the ModernUO codebase: every type, method and file in
`Projects/`, the edges between them, and 1,600-odd clusters grouping them into
subsystems. It exists so that a question like *"what actually touches
`BaseHouse` when a house is demolished?"* can be answered by traversing
relationships instead of grepping and guessing.

It is built with [graphify](https://github.com/safishamsi/graphify), which
parses C# structurally and clusters the result.

## What is in it

Built from `main` at `e52d54b7d`:

| | |
|---|---|
| Nodes | 30,892 |
| Edges | 64,845 (directed) |
| Communities | 1,643 |
| Corpus | 4,190 files — all 4,160 tracked `.cs` files, plus project files, top-level docs and CI workflows |

The most connected types are the ones you would expect, which is a decent
sanity check on the extraction:

`BaseCreature` (889 edges) · `PlayerMobile` (558) · `Mobile` (420) ·
`IGenericReader` (314) · `IPropertyList` (289) · `MLQuest` (277) ·
`CommandEventArgs` (231) · `IEntity` (226) · `Item` (219) · `BaseHouse` (209)

Edges are **directed**, so a caller is distinguishable from a callee: "what
calls `SpellHelper.Damage`" and "what does `SpellHelper.Damage` call" are
different questions with different answers.

## Where it lives

The graph is **not committed**. It is ~34 MB of derived JSON that is stale the
moment `main` moves, and git would keep every version of it forever — against a
repository whose largest tracked file is 768 KB. It is hosted instead:

| Artifact | URL | What it is |
|---|---|---|
| Interactive viewer | `https://graph.muo.gg` | Pan/zoom map of the community structure |
| `graph.json.gz` | `https://graph.muo.gg/graph.json.gz` | Full graph (~1.4 MB) for tooling |
| `wiki.tar.gz` | `https://graph.muo.gg/wiki.tar.gz` | One markdown article per community |
| `manifest.json` | `https://graph.muo.gg/manifest.json` | Source commit, counts, sizes |

**Always check `manifest.json` first.** Its `built_at_commit` says which commit
the hosted graph was extracted from. If that commit is far behind `main`,
anything the graph says about recently changed code may be wrong, and you
should fall back to reading the source. The graph is refreshed manually, not on
every merge — treat it as a map, not as ground truth.

## Using it

### The wiki (best for agents)

The per-community articles are plain markdown and need no tooling. Each one
lists the community's key types with their file paths, and links to the
communities it shares edges with:

```
# Housing System - BaseHouse
> 200 nodes · cohesion 0.02

## Key Concepts
- **BaseHouse** (209 connections) — `Projects/UOContent/Multis/Houses/BaseHouse.cs`
- **HouseGumpAOS** (22 connections) — `Projects/UOContent/Gumps/Houses/HouseGumpAOS.cs`
...
## Relationships
- [[Server: Regions]] (110 shared connections)
- [[Items / Misc - GlassItems.cs]] (24 shared connections)
```

Key Concepts is ordered by raw connection count, so a language primitive such
as `bool` can head the list ahead of the types you actually want. Skip past the
entries with no file path.

`wiki/index.md` is the entry point: communities sorted by size, each a link.
Start there, read the article for the subsystem you care about, then open the
files it names.

### Querying locally

With the graph built (see below) or `graph.json.gz` downloaded and unpacked:

```sh
graphify affected "BaseHouse" --depth 1                 # what depends on this
graphify query "how does spell damage reach a Mobile"   # BFS, broad context
graphify query "..." --dfs                              # trace one path
graphify path "BaseHouse" "Item"                        # shortest path
graphify explain "MonsterAbility"                       # plain-language summary
```

`affected` is the most useful of these before a refactor: it walks edges in
reverse and prints each dependent with a `file.cs:Lnnn` location, so the result
is a list you can open rather than a list you have to go find.

### As an MCP server

Exposes `query_graph`, `get_node`, `get_neighbors`, `get_community`,
`god_nodes`, `graph_stats` and `shortest_path` to any MCP-capable agent:

```sh
python -m graphify.serve graphify-out/graph.json
```

## Rebuilding it

Two paths, depending on whether you want the prose layer refreshed too.

**Code only** — no LLM, no agent, fully scriptable, a few minutes:

```sh
graphify update .                                   # re-extract C#, re-cluster
python tools/knowledge-graph/label_communities.py   # re-derive community names
graphify export html && graphify export wiki
python tools/knowledge-graph/package_artifacts.py
```

**Everything**, including the ~53 nodes extracted from `README.md`,
`CONTRIBUTING.md`, `CLAUDE.md` and the CI workflows: run `/graphify` from
Claude Code (it drives the LLM pass), then the same last three commands.

Run `label_communities.py` **after every rebuild**. Both paths re-cluster, and
clustering renumbers communities — labels from a previous run will not line up.

Full instructions, including the Cloudflare deploy, are in
[`tools/knowledge-graph/README.md`](../tools/knowledge-graph/README.md).

The corpus is pinned by `.graphifyignore` at the repo root, which graphify reads
automatically, so a rebuild covers the same files for everyone. It deliberately
excludes generated migration snapshots, `dev-docs/`, vendored material and
local-only directories.

Cost is low: the structural pass is deterministic C# parsing, and only the 16
prose/CI files reach an LLM (well under 100k tokens for a full rebuild).

## Known limitations

Be aware of these before trusting an answer:

- **Unresolved references are dropped.** About 12% of extracted edges point at
  something with no node — BCL types, generic parameters like `T`, and symbols
  outside the corpus. Those edges do not appear in the graph. Absence of an
  edge is therefore not proof that no relationship exists.
- **Edge multiplicity is lost.** A method that calls another forty times on
  forty lines yields one edge, not forty. Edges answer *whether*, never *how
  often*.
- **Communities are statistical, not architectural.** Clustering is run on
  connectivity, so a cluster can straddle what a developer would call two
  systems, or split one. Community names are generated from the dominant
  source directory — useful labels, not a design document.
- **Clustering is not deterministic.** The same corpus produces a different
  number of communities from run to run (1,621, 1,643 and 1,700 across three
  builds of the same commit), and members shift between them. Node and edge
  counts are stable; community structure is approximate. This is why community
  names are regenerated from content after every rebuild rather than written
  down once.
- **Nothing runtime is modelled.** Reflection, `[Constructible]` discovery,
  event wiring resolved at startup, serialization migrations and anything
  driven by config are invisible to a structural parse. ModernUO leans on all
  of these, so the graph understates coupling in exactly the places the engine
  is most dynamic.
- **The graph lags `main`.** See `manifest.json`.
