# ModernUO Local Development Setup Notes

Date: 2026-06-14
OS: macOS, Apple Silicon
Project: ModernUO
Repository used: https://github.com/modernuo/ModernUO

## Reproduction Process

### Environment Setup

#### Setup Path Chosen

ModernUO does not currently include a VS Code dev container, so I used the typical README setup path.

Relevant project files checked:

- `README.md`
- `CONTRIBUTING.md`
- `global.json`
- `.github/workflows/build-test.yml`

#### Commands Run

```sh
git clone https://github.com/modernuo/ModernUO.git
cd ModernUO
code .
dotnet restore
dotnet build
```

The repository's `global.json` requests .NET SDK `10.0.201` with roll-forward enabled. The local machine has .NET SDK `10.0.300`, which satisfies the requirement.

#### macOS Prerequisites

The README lists these macOS packages:

```sh
brew install icu4c libdeflate zstd argon2
```

Local status:

- `icu4c`: installed
- `libdeflate`: installed
- `zstd`: installed
- `argon2`: was missing, then installed with `brew install argon2`

#### Verification Results

Successful commands:

```sh
dotnet restore
dotnet build
dotnet run --project Projects/BuildTool -- --config Release --skip-prereqs
```

Results:

- `dotnet restore`: succeeded
- `dotnet build`: succeeded with 0 warnings and 0 errors
- CI-style build command: succeeded and generated release output in `Distribution/`

#### Test Result and Setup Caveat

I also ran:

```sh
dotnet test --no-restore
```

Partial result:

- `Server.Tests`: passed, with some skipped tests
- `UOContent.Tests`: failed because Ultima Online client data files are missing

Representative error:

```text
System.IO.FileNotFoundException : Data: tiledata.mul was not found
```

The test fixtures show two environment variables/paths used for client data:

- `MODERNUO_CLIENT_PATH`
- `MODERNUO_TEST_DATA_DIR`
- fallback on Windows: `C:\Ultima Online Classic`

To run the full test suite locally, install or provide the required Ultima Online/ClassicUO data files and point the environment variable at that directory. For example:

```sh
export MODERNUO_TEST_DATA_DIR="/absolute/path/to/Ultima Online Classic"
dotnet test --no-restore
```

#### Current Setup Status

Local development setup is complete for restoring and building ModernUO. The only remaining limitation is full test execution, which requires external game data files that are not included in the repository.

### Steps to Reproduce

Issue: https://github.com/modernuo/ModernUO/issues/1052

Title: Create regions for all vendor shops

Issue summary: ModernUO needs regions for vendor shops so shop-specific mechanics can be handled separately from broad town regions.

### Expected Behavior

Vendor shop locations should resolve to a shop-specific region, or at least a child region nested under the containing town. For example, a Britain baker, blacksmith, tailor, or banker should be distinguishable from the generic `Britain` town region.

### Actual Behavior

Vendor spawn locations in Britain resolve only to the broad `Britain [TownRegion]` entry in `Distribution/Data/regions.json`. This means the server data cannot distinguish those vendor shops as separate regions.

Numbered reproduction steps:

1. Open the ModernUO checkout on branch `fix-issue-1052`.
2. Confirm the project builds with `dotnet build`.
3. Inspect the vendor spawn data in `Distribution/Data/Spawns/shared/trammel/Vendors.json`.
4. Inspect the static region data in `Distribution/Data/regions.json`.
5. Run the reproduction command below from the repository root.
6. Confirm Britain shop vendor locations return only `Britain [TownRegion]` instead of shop-specific child regions.

### Reproduction Command

Run from the repository root:

```sh
node - <<'NODE'
const fs = require('fs');
const readJson = p => JSON.parse(fs.readFileSync(p, 'utf8').replace(/^\uFEFF/, ''));
const regions = readJson('Distribution/Data/regions.json');
const vendors = readJson('Distribution/Data/Spawns/shared/trammel/Vendors.json');

function contains(area, x, y) {
  return (area || []).some(r => x >= r.x1 && x <= r.x2 && y >= r.y1 && y <= r.y2);
}

function matchingRegions(map, x, y) {
  return regions
    .filter(r => r.Map === map && contains(r.Area, x, y))
    .map(r => `${r.Name} [${r.$type}]`);
}

const samples = [
  { type: 'Baker', x: 1450, y: 1617, z: 20 },
  { type: 'Blacksmith', x: 1418, y: 1547, z: 30 },
  { type: 'Tailor', x: 1467, y: 1686, z: 0 },
  { type: 'Banker', x: 1425, y: 1690, z: 0 },
];

for (const s of samples) {
  console.log(`${s.type} @ Trammel ${s.x},${s.y},${s.z}: ${matchingRegions('Trammel', s.x, s.y).join(' | ') || '(none)'}`);
}

const britainVendors = vendors.filter(v =>
  v.map === 'Trammel' &&
  v.location[0] >= 1410 && v.location[0] <= 1500 &&
  v.location[1] >= 1540 && v.location[1] <= 1740
);

const onlyTown = britainVendors.filter(v => {
  const matches = matchingRegions('Trammel', v.location[0], v.location[1]);
  return matches.length === 1 && matches[0].startsWith('Britain ');
}).length;

console.log(`Britain sample set: ${onlyTown}/${britainVendors.length} vendor spawns resolve only to the broad Britain town region.`);
NODE
```

### Confirmed Output

The reproduction was run twice with the same result:

```text
Baker @ Trammel 1450,1617,20: Britain [TownRegion]
Blacksmith @ Trammel 1418,1547,30: Britain [TownRegion]
Tailor @ Trammel 1467,1686,0: Britain [TownRegion]
Banker @ Trammel 1425,1690,0: Britain [TownRegion]
Britain sample set: 20/25 vendor spawns resolve only to the broad Britain town region.
```

### Related Files

- `Distribution/Data/regions.json`
- `Distribution/Data/Spawns/shared/trammel/Vendors.json`
- `Projects/Server/Regions/RegionJsonSerializer.cs`
- `Projects/Server/Regions/Region.cs`
- `Projects/UOContent/Regions/GuardedRegion.cs`

### Branch Link

Working branch: https://github.com/Jynx-hub/ModernUO/tree/fix-issue-1052

## Solution Approach

### Implementation Plan

#### Understand

The issue is not that ModernUO cannot resolve regions. The region system works, but most vendor shops are not represented as specific regions in the region data. When code asks for the region at a vendor shop coordinate, the most specific registered region is still only the broad town region, such as `Britain [TownRegion]`.

Expected behavior: vendor shop coordinates should resolve to a shop-specific child region, while still inheriting behavior from the containing town.

Actual behavior: vendor shop coordinates such as Britain Baker, Blacksmith, Tailor, and Banker resolve only to `Britain [TownRegion]`.

#### Root Cause

Regions are data-driven. `RegionJsonSerializer.LoadRegions()` loads only `Data/regions.json` at startup, deserializes it, and registers each region with `region.Register()`:

- `Projects/Server/Regions/RegionJsonSerializer.cs:96`
- `Projects/Server/Regions/RegionJsonSerializer.cs:104`
- `Projects/Server/Regions/RegionJsonSerializer.cs:111`

At runtime, `Region.Find(Point3D, Map)` scans the registered regions for the map sector and returns the first region that contains the point:

- `Projects/Server/Regions/Region.cs:291`
- `Projects/Server/Regions/Region.cs:298`
- `Projects/Server/Regions/Region.cs:301`

Region precedence already supports child regions: `Region.CompareTo()` sorts by dynamic status, priority, and child level, so child regions can win over parent regions when they cover the same coordinate:

- `Projects/Server/Regions/Region.cs:251`
- `Projects/Server/Regions/Region.cs:252`

The missing piece is static data. `Distribution/Data/regions.json` defines the broad Trammel Britain region around `Distribution/Data/regions.json:1288`, and it already has child regions for fields and other areas. However, shop-specific Britain regions are missing. The vendor spawn data exists separately in `Distribution/Data/Spawns/shared/trammel/Vendors.json`, but those vendor coordinates do not automatically create regions.

#### Match

The codebase already has the exact pattern needed:

- `Distribution/Data/regions.json:1527` defines New Haven shop/skill regions as `NoHousingRegion` children of `New Haven`.
- `Distribution/Data/regions.json:1586` defines `the New Haven Tailor` as a child region with a small shop footprint.
- `Distribution/Data/regions.json:1660` defines `the New Haven Bank`.
- `Distribution/Data/regions.json:1747` defines `The Haven Blacksmith`.

These entries use:

- `$type`: `NoHousingRegion`
- `Parent`: the containing town region
- `Name`: the shop-specific region name
- `RuneName`: when the in-game location name should be user-facing
- `Area`: one or more rectangles covering the shop footprint

`NoHousingRegion` is already registered for region JSON in `Projects/UOContent/Regions/RegionJsonRegistration.cs`, so no new region class should be necessary.

#### Plan

1. Add shop-specific child region entries to `Distribution/Data/regions.json`, starting with the reproduced Britain shops.
2. Use `NoHousingRegion` for normal shops, following the New Haven/Haven pattern.
3. Set `Parent` to `{ "Name": "Britain", "Map": "Trammel" }` for Trammel Britain shops.
4. Add equivalent Felucca entries where the same shop footprint exists under Felucca Britain, because the issue asks for vendor shops broadly, not only Trammel.
5. Use names and optional `RuneName` values that match known shop names where they are discoverable from existing data; otherwise use clear names such as `Britain Blacksmith`, `Britain Bakery`, `Britain Tailor`, and `First Bank of Britain`.
6. Keep all changes data-only unless a missing behavior requires code. The region engine already supports this through parent/child regions and JSON loading.
7. After the initial Britain fix is validated, expand the same pattern to other towns/maps in a controlled follow-up set rather than mixing every vendor shop into one hard-to-review edit.

#### Proposed Fix

Modify `Distribution/Data/regions.json` to add shop-specific `NoHousingRegion` child regions for vendor-shop footprints. These regions should cover the building/shop coordinates that currently resolve only to the parent town. Because the child regions inherit from the town through `Parent`, existing town behavior such as guards and travel restrictions remains intact.

#### Files Expected To Change

- `Distribution/Data/regions.json`
- `Projects/UOContent.Tests/Tests/Regions/VendorShopRegionTests.cs` or another focused test file under `Projects/UOContent.Tests/Tests/Regions/`
- `CONTRIBUTION_SETUP.md` for assignment documentation only

I do not expect to modify `RegionJsonSerializer`, `Region`, `GuardedRegion`, `TownRegion`, or `NoHousingRegion` unless implementation reveals a loader or sorting bug that the reproduction did not show.

#### Implement

Implementation will happen in Phase III.

Branch placeholder: `fix-issue-1052`

#### Review

I reviewed `CONTRIBUTING.md`. The project asks contributors to:

- ensure the repository builds and tests pass before submitting a PR
- follow project workflow and coding conventions
- update README only for interface/build/configuration/dependency changes
- ensure files have appropriate license headers where applicable

For this fix, `regions.json` data changes do not need a license header. A new C# test file should follow the existing test namespace/style and include the normal project file header only if nearby test files use one.

Self-review checklist before PR:

- Confirm each new region has the intended `Map`, `Parent`, `Name`, `Priority`, and `Area`.
- Confirm areas are tight shop footprints, not broad rectangles that accidentally cover streets or unrelated buildings.
- Confirm child regions still inherit town behavior through `Parent`.
- Confirm no duplicate region names are introduced for the same map.
- Confirm JSON formatting remains consistent with nearby entries.

#### Evaluate

Automated verification plan:

1. Add a focused test that loads/registers the relevant regions and asserts known vendor-shop coordinates resolve to the new shop-specific region instead of only `Britain`.
2. Include at least the reproduced coordinates:
   - Baker: Trammel `1450,1617,20`
   - Blacksmith: Trammel `1418,1547,30`
   - Tailor: Trammel `1467,1686,0`
   - Banker: Trammel `1425,1690,0`
3. Assert the resolved region is still part of `Britain`, proving the parent relationship is intact.
4. Run the reproduction command from Step 3 again and verify those coordinates no longer resolve only to `Britain [TownRegion]`.
5. Run:

```sh
dotnet build
dotnet test --no-restore --filter VendorShopRegion
```

If the focused test requires UOContent initialization and local game data is unavailable, run the data-only reproduction script as the minimum local verification and document the limitation. The earlier setup already showed full `UOContent.Tests` can fail locally without external Ultima Online data files.

## Phase III — Implementation

Phase III is complete. The fix is implemented, tested, pushed, and open as a **draft pull request** — [modernuo/ModernUO#2498](https://github.com/modernuo/ModernUO/pull/2498).

### Implementation Notes

The fix is **data-only**, exactly as planned in Phase II — no engine code was touched. I added shop-specific `NoHousingRegion` child regions to `Distribution/Data/regions.json`, following the existing New Haven / Haven shop pattern. The region engine already resolves a child region over its parent town (`Region.CompareTo` / `Region.Find`), so the only missing piece was the static data.

The work started as a Britain-first slice (the issue thread explicitly supports doing this "by steps") and grew to the full scope the issue title asks for — **regions for all vendor shops**:

- **394 new shop regions** across **23 towns** on **all 6 facets** (Trammel, Felucca, Ilshenar, Malas, Tokuno, TerMur). Towns covered: Britain, Trinsic, Vesper, Minoc, Cove, Yew, Skara Brae, Jhelom, Moonglow, Magincia, Nujel'm, Ocllo, Serpent's Hold, Buccaneer's Den, Delucia, Papua, Wind (Trammel/Felucca); Gargoyle City and Reg Volon (Ilshenar); Luna and Umbra (Malas); Zento (Tokuno); and Royal City (TerMur).
- Each region represents one shop **trade** (Bakery, Butcher, Blacksmith, Bowyer, Tinker, Tailor, Mage, Provisioner, Jeweler, Bank, Healer, Carpenter, Scribe, Bard, Tanner, Docks, etc.). Where a town has several buildings of the same trade, the region carries one tight footprint rectangle per vendor spawner.
- Footprints are derived from the actual vendor spawn coordinates in `Distribution/Data/Spawns/**/Vendors.json`, sized as small boxes centered on each vendor and clipped to stay inside the parent town polygon. They were produced by a generator that reads the spawn data, clusters vendors by trade per town, and emits non-overlapping footprints — then validated programmatically (see Testing Strategy).
- Every region uses `"$type": "NoHousingRegion"`, `"Priority": 50`, and `"Parent": { "Name": "<Town>", "Map": "<Facet>" }`, matching the existing shop entries. Names follow the existing convention (e.g. `the Britain Bakery`, `the Minoc Bank`, `the Luna Tailor`).
- **Nested towns are handled.** A few towns are themselves child regions (e.g. Jhelom sits under `Jhelom Islands`). Shops still nest correctly because resolution returns the deepest child; both the generator and the tests walk the full ancestor chain rather than assuming a town is top-level.
- **Taverns/inns were intentionally excluded.** Every tavern/inn vendor already stands inside the town's existing unnamed `NoLogoutDelay` region (the inn no-logout zone). Adding an overlapping equal-priority "Tavern" region would create ambiguous resolution or shadow the inn logout behavior, so taverns are left as future work.

Reusing the engine as-is: `NoHousingRegion` is already registered for region JSON in `Projects/UOContent/Regions/RegionJsonRegistration.cs`, so no new region class was needed and `RegionJsonSerializer` / `Region` were not modified.

### Code Changes

- **Branch:** [`fix-issue-1052`](https://github.com/Jynx-hub/ModernUO/tree/fix-issue-1052) (pushed; in sync with `origin`).
- **Draft PR:** [modernuo/ModernUO#2498 — feat(regions): create regions for all vendor shops (#1052)](https://github.com/modernuo/ModernUO/pull/2498)
- **Files changed:**
  - `Distribution/Data/regions.json` — added the 394 shop regions.
  - `Projects/Server.Tests/Tests/Regions/VendorShopRegionTests.cs` — new CI-safe test (note: this lives in `Server.Tests`, not `UOContent.Tests` as Phase II guessed, because the region/JSON infrastructure lives there and that project copies `Distribution/Data` and runs without client files).
- **Commits** (Conventional Commits style — `feat(regions):`, `test(regions):`):
  - [`b9cd4fd`](https://github.com/Jynx-hub/ModernUO/commit/b9cd4fd86e91c94889ab36547a0ef5fcc5224c0e) — feat(regions): add Britain vendor shop regions (Trammel + Felucca)
  - [`28d96bf`](https://github.com/Jynx-hub/ModernUO/commit/28d96bf35fef66b42d7f981e52d9eac1579d265c) — feat(regions): add Trinsic vendor shop regions (Trammel + Felucca)
  - [`72f0af4`](https://github.com/Jynx-hub/ModernUO/commit/72f0af4fbd0f63ff103e2a29ff5e6eff7e8e20be) — feat(regions): add Vesper vendor shop regions (Trammel + Felucca)
  - [`419c501`](https://github.com/Jynx-hub/ModernUO/commit/419c501800b4ad60e7eefae5035c11253b422f8a) — feat(regions): add Minoc vendor shop regions (Trammel + Felucca)
  - [`7a2f303`](https://github.com/Jynx-hub/ModernUO/commit/7a2f303224908f8b23b2095f0864f5bd520b60ea) — test(regions): verify vendor shops resolve to shop-specific regions
  - [`0cb400c`](https://github.com/Jynx-hub/ModernUO/commit/0cb400c6ab185b91d7fc041006628166d0c50f16) — feat(regions): add vendor shop regions for all remaining towns
  - [`c709faf`](https://github.com/Jynx-hub/ModernUO/commit/c709faf3bba4737349a18ce6a846c98f8666ca2f) — test(regions): cover new vendor shop regions and nested-town parents

### Testing Strategy

`VendorShopRegionTests.cs` is a pure data-validation test (xUnit). It parses `Data/regions.json` — copied next to the test assembly by the project's `CopyData` build target — with `System.Text.Json`, so it needs **no client map files** and runs cleanly in CI. This mirrors the original Node.js reproduction in C#. It covers:

1. **Resolution (the fix):** a `[Theory]` of **26 sample cases** over known vendor coordinates (the reproduced Britain coords plus samples spanning every facet — Trammel, Felucca, Ilshenar, Malas, Tokuno, TerMur — and the nested-town case Jhelom) asserts that exactly one new shop region covers the point, that it is the expected shop, and that the parent town region still contains the point (proving the nesting is intact).
2. **Structure:** every new shop region has a non-empty area, a `Parent` that resolves to a region on the same map, and a name that is unique per map.
3. **No overlaps:** new shop regions do not overlap any other region except their **ancestor chain** (parent town and that town's own parents, e.g. `Jhelom Islands`), guarding against loose or misplaced footprints while allowing legitimate child-over-parent nesting.

Results:

- `dotnet test --filter VendorShopRegion` → **28 passed, 0 failed** (26 theory cases + 2 facts).
- Full `Server.Tests` suite → **719 passed, 17 skipped** (skips require client tile data, unrelated), **0 failed** — no regressions.
- Solution compiles with **0 warnings / 0 errors** (`Server`, `UOContent`, `Application`, `Server.Tests`).
- Re-ran the Phase II reproduction, extended across all towns/facets: the sampled coordinates now resolve to e.g. `the Britain Bakery [NoHousingRegion]` instead of `Britain [TownRegion]`.

### Challenges Faced

- **Footprints without client map art.** There is no canonical OSI vendor-region list (noted in the issue thread). I approximated each shop footprint from vendor spawn coordinates, kept the boxes tight, and validated programmatically that none overlap each other or existing regions and that all stay inside the town. The footprints are intentionally conservative and open to maintainer correction (Discord: `muo.gg/discord`).
- **Tavern/inn overlap.** Discovered that tavern/inn vendors already sit inside the existing `NoLogoutDelay` inn regions; rather than create conflicting regions, I excluded taverns and documented it.
- **Test data staleness.** Building the `Server.Tests.csproj` directly leaves `$(SolutionDir)` undefined, so the `CopyData` target does not refresh `Data/regions.json` and the test reads a stale copy. Building via the solution (or passing `-p:SolutionDir=...`) fixes it; CI builds via the solution, so it is unaffected. Worth knowing for local runs.

### Out of Scope (follow-ups noted for the PR)

- Wiring these regions into `FillableContent.Acquire()` (see the `// TODO: Replace with vendor shop regions and a fallback override.` at `FillableContent.cs:96`), which is the mechanic that motivates the issue.
- Tavern/inn shop classification and a dedicated `VendorShopRegion` type (only needed once game logic consumes a per-shop content tag).
- Maintainer refinement of individual footprints — they are derived conservatively from spawn coordinates rather than client map art, and are open to correction.

**Phase III Complete.**
