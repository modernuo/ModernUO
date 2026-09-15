---
name: modernuo-knowledge-graph
description: >
  Trigger when orienting in unfamiliar parts of the codebase, tracing what calls or depends on a
  type, assessing the blast radius of a change, or answering "where does X live / what touches Y".
  Use the hosted knowledge graph before fanning out greps across 4,000+ files.
---

# ModernUO Knowledge Graph

A prebuilt map of the codebase: 30,892 nodes and 64,845 directed edges over
every `.cs` file in `Projects/`, clustered into roughly 1,640 subsystems.

Full documentation: `dev-docs/knowledge-graph.md`.

## When This Activates
- Orienting in a subsystem you have not read yet
- "What calls this?" / "What does this depend on?" / "What breaks if I change it?"
- Finding where a feature lives without knowing its filenames
- Checking whether two systems are coupled before refactoring across them

## When NOT to use it
- **Anything you can answer by reading one known file.** Just read the file.
- **Verifying current behaviour.** The graph lags `main` — see below.
- **Runtime wiring.** Reflection, `[Constructible]` discovery, event
  registration and serialization migrations are invisible to a structural
  parse. ModernUO uses all of them heavily.

## Rule 1: Check freshness first

```sh
curl -s https://graph.muo.gg/manifest.json
```

`built_at_commit` is the commit the graph was extracted from. Compare it to
`main`. If the code you care about changed since, **read the source instead** —
the graph is refreshed manually and is a map, not ground truth. Say so plainly
rather than reporting stale structure as fact.

## Rule 2: Prefer the wiki for orientation

The per-community articles are plain markdown, need no tooling, and name real
file paths:

```sh
curl -s https://graph.muo.gg/wiki.tar.gz | tar xz
# then read wiki/index.md and follow the community you need
```

Each article lists the community's key types with paths, and the communities it
shares edges with. This is usually the fastest way to go from "I know nothing
about the crafting system" to "here are the eight files that matter".

## Rule 3: Query the graph for relationships

If the graph is built locally (`graphify update .` from the repo root, a few
minutes, no LLM) or `graph.json.gz` is unpacked into `graphify-out/`:

```sh
graphify affected "BaseHouse" --depth 1   # what breaks if I change this
graphify query "how does spell damage reach a Mobile"
graphify path "BaseHouse" "Item"          # shortest path between two types
graphify explain "MonsterAbility"         # plain-language summary of a node
```

`affected` is the one to reach for before a refactor. It walks edges in reverse
and reports every dependent with a `file.cs:Lnnn` location, so the output is
directly openable:

```
- ConfirmDemolishHouseGump [references] Projects/UOContent/Gumps/Houses/ConfirmDemolishHouseGump.cs:L8
- .ClearCoOwners_Callback() [references] Projects/UOContent/Gumps/Houses/HouseGumpAOS.cs:L720
```

Edges are directed, so caller and callee are distinguishable — "what calls
`SpellHelper.Damage`" and "what does it call" give different answers.

## Rule 4: Never present the graph as complete

Three limits change how you must phrase conclusions:

1. **~12% of edges are dropped** because they point at BCL types, generic
   parameters or symbols outside the corpus. **Absence of an edge is not
   evidence of no relationship.** Never say "nothing calls X" on graph evidence
   alone — confirm with a grep before making that claim.
2. **Multiplicity is lost.** Forty calls on forty lines collapse to one edge.
   The graph answers *whether*, never *how often*.
3. **Communities are statistical.** Clusters come from connectivity and their
   names are generated from the dominant directory. A community boundary is a
   hint, not an architectural statement.

Use the graph to find candidates fast, then verify in the source before acting
on what you found.

## God Nodes

The most connected types, worth knowing because a change to any of them has
wide reach:

| Type | Edges |
|---|---|
| `BaseCreature` | 889 |
| `PlayerMobile` | 558 |
| `Mobile` | 420 |
| `IGenericReader` | 314 |
| `IPropertyList` | 289 |
| `MLQuest` | 277 |
| `CommandEventArgs` | 231 |
| `IEntity` | 226 |
| `Item` | 219 |
| `BaseHouse` | 209 |

Touching one of these is a signal to widen review, not a reason to stop.

## Rebuilding

See `tools/knowledge-graph/README.md`. The corpus is pinned by `.graphifyignore`
at the repo root, so everyone who rebuilds gets the same file set.
