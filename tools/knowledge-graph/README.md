# ModernUO knowledge graph — build & deploy

Tooling that turns the ModernUO source tree into a queryable knowledge graph and
publishes it. For what the graph *is* and how to use it, read
[`dev-docs/knowledge-graph.md`](../../dev-docs/knowledge-graph.md).

The graph is a **derived artifact**. It is never committed: it is tens of
megabytes, and it goes stale the moment `main` moves. It is rebuilt on demand
and hosted, and `manifest.json` records the commit it came from so anyone can
tell how far behind the hosted copy has drifted.

## What lives here

| File | Purpose |
|---|---|
| `label_communities.py` | Names each community from its contents, deterministically |
| `package_artifacts.py` | Compresses the build into `dist/` for upload |
| `README.md` | This file |

`dist/` and `graphify-out/` are both gitignored.

## Rebuilding

Requires [graphify](https://github.com/safishamsi/graphify) (0.8.49+) and Python 3.10+:

```sh
uv tool install graphifyy     # or: pip install graphifyy
```

From the repository root:

```sh
graphify update .                                   # re-extract C#, rebuild, re-cluster
python tools/knowledge-graph/label_communities.py   # stable community names
graphify export html                                # interactive viewer
graphify export wiki                                # per-community articles
python tools/knowledge-graph/package_artifacts.py   # -> tools/knowledge-graph/dist/
```

`graphify update` is structural only: it re-parses C# with no LLM and no agent,
which makes it the path to script. It does **not** refresh the ~53 nodes
extracted from the prose and CI files — for those, run `/graphify` from Claude
Code, which drives the LLM pass, and then the same last three commands.

Two things about `update` are easy to get wrong:

- **It re-clusters.** Communities are renumbered on every run, so
  `label_communities.py` must run after it or the names will not line up with
  the communities they describe.
- **It skips `graph.html`** because the graph is far above graphify's 5,000-node
  viz limit. `graphify export html` handles that by aggregating to a
  community-level view, so run it explicitly.

The corpus is defined by [`.graphifyignore`](../../.graphifyignore) at the repo
root, which graphify reads automatically — no flags needed, and everyone who
rebuilds gets the same file set. It currently resolves to ~4,190 files: every
`.cs` file tracked in `Projects/`, the solution and project files, and the
top-level docs and CI workflows.

Expect roughly 5 minutes on a modern machine. The structural pass is pure C#
AST parsing — parallel, free, and identical run to run, so the `update` path
above costs no tokens at all. The full `/graphify` path adds only the 16
prose/CI files, which lands well under 100k tokens.

### Why labels are generated, not hand-written

graphify numbers communities arbitrarily; the same corpus can yield different
integer IDs run to run. Names written against those IDs would be wrong the
first time anyone regenerated the graph. `label_communities.py` therefore
derives every name from the community's dominant source directory plus its
most-connected member, so rebuilds reproduce the same names and the wiki stays
diffable. Names that would otherwise be unhelpfully generic are corrected
through the `OVERRIDES` table, keyed by directory rather than by ID.

## Deploying to Cloudflare

`package_artifacts.py` writes one self-contained directory, `dist/site/`:

```
index.html        the interactive graph
graph.json.gz     full graph, ~1.4 MB compressed
wiki.tar.gz       per-community articles
GRAPH_REPORT.md   audit report
manifest.json     source commit, counts, artifact sizes
_headers          content types, and no-cache on manifest.json
```

That is the whole deploy. With
[wrangler](https://developers.cloudflare.com/workers/wrangler/) authenticated:

```sh
wrangler pages deploy tools/knowledge-graph/dist/site --project-name modernuo-knowledge-graph
```

Then point the Pages project's custom domain at `graph.muo.gg`, the hostname
`dev-docs/knowledge-graph.md` and the skill send agents to. Keep the three in
sync if it ever changes.

### Why Pages and not R2

The full artifact set is a few MB, and the largest single file is the 2.3 MB
`index.html` — comfortably inside Pages' 25 MB per-file limit. Serving it all
from Pages means one command, one hostname, and no bucket to make public; R2
objects are private by default, which is an easy way to hand agents a 401
instead of a graph. Reach for R2 only if an artifact outgrows the per-file
limit or you want versioned history of past graphs.

### Serving notes

- `_headers` marks the `.gz` files `application/gzip` so they are served as
  opaque downloads. Consumers decompress them (`| tar xz`, `gunzip`) rather
  than relying on transparent decoding.
- `manifest.json` is served `no-cache`, so a freshness check never reads a
  stale commit. Everything else is cached for an hour.
- Deploys are atomic, so there is no window where `manifest.json` advertises a
  commit whose artifacts have not landed yet.
