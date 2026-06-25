// One-time migration: rename "type" -> "$type" (kept first), and convert legacy
// homeRange (+location) -> spawnBounds, dropping homeRange. Run once, then delete this tool.
//
// Usage: node tools/spawner-json-migrate/migrate.mjs Distribution/Data/Spawns
import { readdirSync, readFileSync, writeFileSync, statSync } from "node:fs";
import { join } from "node:path";

function* walk(dir) {
  for (const name of readdirSync(dir)) {
    const p = join(dir, name);
    if (statSync(p).isDirectory()) {
      yield* walk(p);
    } else if (name.endsWith(".json")) {
      yield p;
    }
  }
}

// Produce a spawnBounds object that deserializes to the SAME Rectangle3D as the
// runtime homeRange path in BaseSpawner.ApplyDto.
//
// Runtime formula:
//   homeRange == 0 -> Rectangle3D(x, y, z, 1, 1, 0)
//                  -> _start=(x,y,z), _end=(x+1,y+1,z)
//   homeRange  > 0 -> Rectangle3D(x-hr, y-hr, -128, hr*2+1, hr*2+1, 256)
//                  -> _start=(x-hr,y-hr,-128), _end=(x+hr+1,y+hr+1,128)
//
// Rectangle3DConverter x1/y1/z1/x2/y2/z2 form creates:
//   Rectangle3D(Point3D(x1,y1,z1), Point3D(x2,y2,z2))
// where _start=(x1,y1,z1) and _end=(x2,y2,z2).
// Rectangle3D.End is EXCLUSIVE (contains: x >= start.X && x < end.X).
function toBounds(loc, hr) {
  const [x, y, z] = loc;
  if (hr === 0) {
    // Single-tile bounds: _start=(x,y,z), _end=(x+1,y+1,z)
    return { x1: x, y1: y, z1: z, x2: x + 1, y2: y + 1, z2: z };
  }
  // hr*2+1 wide/tall: _start=(x-hr,y-hr,-128), _end=(x+hr+1,y+hr+1,128)
  return { x1: x - hr, y1: y - hr, z1: -128, x2: x + hr + 1, y2: y + hr + 1, z2: 128 };
}

function migrateOne(obj) {
  const out = {};
  // $type must be the first key (STJ polymorphism requires discriminator first).
  if ("type" in obj) {
    out["$type"] = obj["type"];
  } else if ("$type" in obj) {
    out["$type"] = obj["$type"];
  }
  for (const [k, v] of Object.entries(obj)) {
    if (k === "type" || k === "$type") {
      continue;
    }
    if (k === "homeRange") {
      // Convert homeRange -> spawnBounds (skip if spawnBounds already present).
      if (obj.spawnBounds === undefined && Array.isArray(obj.location)) {
        out.spawnBounds = toBounds(obj.location, v);
      }
      continue; // drop homeRange from output
    }
    out[k] = v;
  }
  return out;
}

let files = 0;
let records = 0;
for (const file of walk(process.argv[2])) {
  // Strip UTF-8 BOM if present (some editors write it, JSON.parse chokes on it).
  const raw = readFileSync(file, "utf8").replace(/^﻿/, "");
  const data = JSON.parse(raw);
  const arr = Array.isArray(data) ? data : [data];
  const migrated = arr.map((o) => {
    records++;
    return migrateOne(o);
  });
  const result = Array.isArray(data) ? migrated : migrated[0];
  writeFileSync(file, JSON.stringify(result, null, 2) + "\n", "utf8");
  files++;
}
console.log(`Migrated ${records} record(s) across ${files} file(s).`);
