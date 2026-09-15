#!/usr/bin/env python3
"""Package a built graphify graph for hosting on Cloudflare Pages.

Everything lands in one directory, tools/knowledge-graph/dist/site/ (gitignored),
so publishing is a single `wrangler pages deploy` with no bucket to configure:

    index.html        the interactive graph
    graph.json.gz     the full graph for agents and tooling (~24x smaller)
    wiki.tar.gz       the per-community wiki articles
    GRAPH_REPORT.md   the audit report
    manifest.json     what commit this was built from, and how big it is
    _headers          content types, and no-cache on manifest.json

The whole set is a few MB and the largest file is well inside Pages' 25 MB
per-file limit, so R2 buys nothing here.

manifest.json is the staleness signal: it records the commit the graph was
extracted from, so a consumer can compare it against origin/main and know
how far behind the hosted copy has drifted. It is served no-cache so a
freshness check never reads a stale answer.

Usage:
    python package_artifacts.py [--graphify-out graphify-out] [--dist dist]
"""
from __future__ import annotations

import argparse
import gzip
import json
import shutil
import subprocess
import tarfile
from datetime import datetime, timezone
from pathlib import Path

HERE = Path(__file__).resolve().parent


def human(n):
    for unit in ("B", "KB", "MB", "GB"):
        if n < 1024 or unit == "GB":
            return f"{n:.1f}{unit}" if unit != "B" else f"{n}B"
        n /= 1024


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--graphify-out", default=None,
                    help="graphify output dir (default: <repo>/graphify-out)")
    ap.add_argument("--dist", default=None,
                    help="output dir (default: alongside this script)")
    args = ap.parse_args()

    repo = HERE.parent.parent
    out = Path(args.graphify_out) if args.graphify_out else repo / "graphify-out"
    dist = Path(args.dist) if args.dist else HERE / "dist"

    graph_json = out / "graph.json"
    if not graph_json.exists():
        raise SystemExit(f"{graph_json} not found - run /graphify first")

    if dist.exists():
        shutil.rmtree(dist)
    site = dist / "site"
    site.mkdir(parents=True)

    graph = json.loads(graph_json.read_text(encoding="utf-8"))
    commit = graph.get("built_at_commit")

    # graph.json.gz -- the payload agents actually fetch
    raw = graph_json.read_bytes()
    gz_path = site / "graph.json.gz"
    gz_path.write_bytes(gzip.compress(raw, 9))

    # site/index.html -- Cloudflare Pages serves index.html at the root
    html = out / "graph.html"
    if html.exists():
        shutil.copyfile(html, site / "index.html")

    # wiki.tar.gz -- thousands of small files, so archive rather than upload each
    wiki = out / "wiki"
    wiki_files = 0
    wiki_path = site / "wiki.tar.gz"
    if wiki.is_dir():
        with tarfile.open(wiki_path, "w:gz", compresslevel=9) as tar:
            tar.add(wiki, arcname="wiki")
        wiki_files = sum(1 for _ in wiki.rglob("*") if _.is_file())

    # The report is small enough to serve uncompressed next to the site.
    report = out / "GRAPH_REPORT.md"
    if report.exists():
        shutil.copyfile(report, site / "GRAPH_REPORT.md")

    describe = None
    try:
        describe = subprocess.run(
            ["git", "-C", str(repo), "describe", "--tags", "--always"],
            capture_output=True, text=True, check=False).stdout.strip() or None
    except OSError:
        pass

    manifest = {
        "built_at_commit": commit,
        "built_at_describe": describe,
        "generated_at": datetime.now(timezone.utc).isoformat(),
        "directed": graph.get("directed"),
        "nodes": len(graph.get("nodes", [])),
        "edges": len(graph.get("links", [])),
        "communities": len({n.get("community") for n in graph.get("nodes", [])
                            if n.get("community") is not None}),
        "artifacts": {
            "graph.json.gz": gz_path.stat().st_size,
            "wiki.tar.gz": wiki_path.stat().st_size if wiki_path.exists() else None,
            "index.html": (site / "index.html").stat().st_size
                          if (site / "index.html").exists() else None,
        },
        "wiki_articles": wiki_files or None,
    }
    (site / "manifest.json").write_text(
        json.dumps(manifest, indent=2) + "\n", encoding="utf-8")

    # Cloudflare Pages reads _headers at the site root. The .gz files must be
    # served as opaque downloads rather than transparently re-encoded, and the
    # freshness manifest must never be answered from cache.
    (site / "_headers").write_text(
        "/graph.json.gz\n"
        "  Content-Type: application/gzip\n"
        "  Cache-Control: public, max-age=3600\n"
        "/wiki.tar.gz\n"
        "  Content-Type: application/gzip\n"
        "  Cache-Control: public, max-age=3600\n"
        "/manifest.json\n"
        "  Content-Type: application/json\n"
        "  Cache-Control: no-cache\n"
        "/GRAPH_REPORT.md\n"
        "  Content-Type: text/markdown; charset=utf-8\n",
        encoding="utf-8")

    print(f"Packaged into {site}")
    print(f"  commit      {commit}")
    print(f"  graph       {manifest['nodes']:,} nodes / {manifest['edges']:,} edges "
          f"/ {manifest['communities']:,} communities")
    for name, size in manifest["artifacts"].items():
        if size:
            print(f"  {name:<18} {human(size)}")


if __name__ == "__main__":
    main()
