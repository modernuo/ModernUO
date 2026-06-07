# T2A Crafting Menus Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Produce a definitive `feat/t2a-crafting-menus` branch that brings Jack's packet-based T2A crafting menus onto current `main`, corrects the known mechanic bugs (stacked-gem jewelry, gem null-safety, gating coherence), and verifies inscription consumption — all behind the `T2ACraftMenus` feature flag.

**Architecture:** This is primarily a *reconciliation*, not greenfield code. Jack's branch (`jackuoll/t2a_crafting_menus`) is a superset of Delphi's and touches 39 files; only 2 of them drifted on `main` (both additive FeatureFlags edits). The plan: (1) cherry-pick Jack's 13 commits onto our branch, resolving the 2 trivial conflicts; (2) apply targeted fixes as new commits on top; (3) audit, build, test, and hand off a manual in-client checklist. The full design rationale lives in `docs/superpowers/specs/2026-06-07-t2a-crafting-menus-design.md` — read it first.

**Tech Stack:** .NET 10, ModernUO server engine, xUnit (`Projects/UOContent.Tests`), `ModernUO.Serialization` source generators, the existing `0x7C`/`0x7D` menu packets.

---

## Pre-flight

- The branch `feat/t2a-crafting-menus` already exists (created off `origin/main`) and contains the design-doc commits.
- Source branch: `jackuoll/t2a_crafting_menus`. Reference branch: `delphi/T2A_CraftingMenus`.
- Merge-base: `e1ae9706` (2026-03-15). Jack's 13 feature commits = `e1ae9706..jackuoll/t2a_crafting_menus`, cumulatively touching 39 files / ~4197 insertions.
- Feature flag: `ContentFeatureFlags.T2ACraftMenus` (config key `"t2a_craft_menus"`, default off).

---

## File Structure

**Brought in wholesale by the cherry-pick (Jack's authored files — do not re-type):**
- `Projects/Server/Menus/{BaseMenu,ItemListMenu,QuestionMenu}.cs` — additive engine menu changes (authorized).
- `Projects/UOContent/Engines/Craft/T2A/*.cs` — `T2ACraftSystem`, `T2ACraftToolTarget`, and 8 `*Menu.cs` files.
- `Projects/UOContent/Engines/Craft/Core/*.cs`, `Def*.cs`, `Skills/{Inscribe,Cartography}.cs`, `Items/Jewels/{BaseJewel,Ring}.cs`, migration JSON, `Network/Packets/IncomingPlayerPackets.cs`, FeatureFlags, `BaseTool.cs`.

**Modified by this plan's fix tasks:**
- `Projects/UOContent/Items/Jewels/BaseJewel.cs` — gem consumption (Task 3) + null-safety (Task 4).
- `Projects/UOContent/Engines/FeatureFlags/FeatureFlagManager.cs` — coherence warning (Task 6).

**Created by this plan:**
- `Projects/UOContent.Tests/Tests/Items/Jewels/T2AJewelGemCraftTests.cs` — gem-consumption tests (Task 2/3).

---

## Phase 1 — Assemble the branch

### Task 1: Cherry-pick Jack's commits onto the branch

**Files:** all 39 (mechanical); conflicts expected only in `ContentFeatureFlags.cs` and `FeatureFlagManager.cs`.

- [ ] **Step 1: Confirm you are on the branch with a clean tree**

Run:
```bash
git checkout feat/t2a-crafting-menus
git status --short
```
Expected: on `feat/t2a-crafting-menus`; no staged changes (untracked pathfinding artifacts under `Distribution/`, `docs/`, `TestResults/` are fine — leave them).

- [ ] **Step 2: Cherry-pick the 13-commit range**

Run:
```bash
git cherry-pick e1ae9706..jackuoll/t2a_crafting_menus
```
Expected: stops with a conflict on the commit `4b3821f13` ("Add: T2ACraftMenus feature flag…") in the two FeatureFlags files. (Earlier commits apply cleanly.)

- [ ] **Step 3: Resolve `ContentFeatureFlags.cs`**

Keep both `main`'s new flags and Jack's. The merged class body must contain all three additions:
```csharp
    public static bool YoungPlayerSystem { get; set; } = true;
    public static bool BitmapPathfindingCache { get; set; } = true;
    public static bool T2ACraftMenus { get; set; }
```
Resolve so all existing flags plus these three are present (order not significant; place `T2ACraftMenus` last).

- [ ] **Step 4: Resolve `FeatureFlagManager.cs`**

Keep both `main`'s new switch arms and Jack's. The switch must contain all three arms:
```csharp
            "young_player_system"      => ContentFeatureFlags.YoungPlayerSystem = enabled,
            "bitmap_pathfinding_cache" => ContentFeatureFlags.BitmapPathfindingCache = enabled,
            "t2a_craft_menus"          => ContentFeatureFlags.T2ACraftMenus = enabled,
```

- [ ] **Step 5: Continue the cherry-pick**

Run:
```bash
git add Projects/UOContent/Engines/FeatureFlags/ContentFeatureFlags.cs Projects/UOContent/Engines/FeatureFlags/FeatureFlagManager.cs
git cherry-pick --continue --no-edit
```
Expected: the remaining commits apply with no further conflicts; cherry-pick completes.

- [ ] **Step 6: Sanity-check the result matches Jack's content**

Run:
```bash
git diff HEAD jackuoll/t2a_crafting_menus -- Projects/UOContent/Engines/Craft/T2A Projects/UOContent/Items/Jewels Projects/Server/Menus
```
Expected: **empty** (our branch now has byte-identical T2A content to Jack's for those paths). If non-empty, investigate before proceeding.

- [ ] **Step 7: Build**

Run:
```bash
dotnet build ModernUO.slnx -c Debug
```
Expected: build succeeds (0 errors). Warnings acceptable. If the serialization generator complains about `BaseJewel`, confirm `Server.Items.BaseJewel.v5.json` exists under `Projects/UOContent/Migrations/`.

- [ ] **Step 8: Run the existing serialization/migration test as a smoke check**

Run:
```bash
dotnet test Projects/UOContent.Tests/UOContent.Tests.csproj --filter "FullyQualifiedName~Migration" -c Debug
```
Expected: PASS (confirms the v5 jewel migration didn't break existing migration coverage).

No commit step — the cherry-pick already produced commits.

---

## Phase 2 — Fix the stacked-gem jewelry mechanic (B3a)

> **Design ref:** spec §9 / D4. Jack deliberately consumes 1 gem but names the piece by the full stack ("a 1000 diamond ring"). We change consumption to match the name: consume the full `PendingGemCount`.

### Task 2: Failing test — full gem stack is consumed

**Files:**
- Create: `Projects/UOContent.Tests/Tests/Items/Jewels/T2AJewelGemCraftTests.cs`

- [ ] **Step 1: Write the failing test**

Create the file with:
```csharp
using Server;
using Server.Engines.Craft;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class T2AJewelGemCraftTests
{
    // Y=500 keeps us inside Felucca bounds; X offset avoids other sequential tests.
    private static PlayerMobile CreatePlayerMobile(Map map, Point3D location)
    {
        var m = new PlayerMobile();
        m.MoveToWorld(location, map);
        m.AddItem(new Backpack());
        return m;
    }

    // Builds a minimal tinkering ring recipe (2 iron ingots) without relying on
    // DefTinkering.InitCraftList, which is gated on the feature flag at startup.
    private static CraftItem MakeRingRecipe()
    {
        var item = new CraftItem(typeof(GoldRing), "ring", "gold ring");
        item.AddRes(typeof(IronIngot), "iron ingot", 2, "You do not have enough ingots.");
        return item;
    }

    [Fact]
    public void OnCraft_ConsumesEntireTargetedGemStack_AndNamesByCount()
    {
        var map = Map.Felucca;
        var player = CreatePlayerMobile(map, new Point3D(4100, 500, 0));

        try
        {
            var pack = player.Backpack;
            pack.AddItem(new IronIngot(10));
            pack.AddItem(new Diamond(50)); // a stack of 50 diamonds

            var system = DefTinkering.CraftSystem;
            var context = system.GetContext(player);
            context.PendingGemType = GemType.Diamond;
            context.PendingGemCount = 50;

            var ring = new GoldRing();
            ring.OnCraft(1, false, player, system, typeof(IronIngot), null, MakeRingRecipe(), 0);

            Assert.Equal(0, pack.GetAmount(typeof(Diamond)));   // all 50 consumed
            Assert.Equal(GemType.Diamond, ring.GemType);
            Assert.Equal(50, ring.GemCount);
            // pending state cleared so the next craft starts fresh
            Assert.Equal(GemType.None, context.PendingGemType);
            Assert.Equal(0, context.PendingGemCount);
        }
        finally
        {
            player.Delete();
        }
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run:
```bash
dotnet test Projects/UOContent.Tests/UOContent.Tests.csproj --filter "FullyQualifiedName~T2AJewelGemCraftTests.OnCraft_ConsumesEntireTargetedGemStack_AndNamesByCount" -c Debug
```
Expected: FAIL — `Assert.Equal(0, …)` fails with actual `49` (Jack's code consumed only 1 diamond).

- [ ] **Step 3: Commit the failing test**

```bash
git add Projects/UOContent.Tests/Tests/Items/Jewels/T2AJewelGemCraftTests.cs
git commit -m "test(t2a-craft): jewelry should consume the full targeted gem stack"
```

### Task 3: Fix — consume the full stack (B3a) with null-safety (B3)

**Files:**
- Modify: `Projects/UOContent/Items/Jewels/BaseJewel.cs` (the `OnCraft` gem block, ~lines 192–206)

- [ ] **Step 1: Replace the gem-consumption block**

Find this block in `OnCraft`:
```csharp
        // T2A jewelry: read gem info from craft context (set by GemSelectTarget)
        if (context is { PendingGemType: not GemType.None, PendingGemCount: > 0 })
        {
            var gemItemType = GetGemItemType(context.PendingGemType);
            // Only 1 gem is consumed per craft, even if the targeted stack was larger.
            // GemCount records the original stack size for the item description.
            if (gemItemType != null && from.Backpack?.ConsumeTotal(gemItemType, 1) == true)
            {
                GemType = context.PendingGemType;
                GemCount = context.PendingGemCount;
            }

            context.PendingGemType = GemType.None;
            context.PendingGemCount = 0;
        }
```
Replace it with (consume the full count; guard null gem type; name only by what was actually consumed):
```csharp
        // T2A jewelry: read gem info from craft context (set by GemSelectTarget).
        // The entire targeted gem stack is consumed and the piece is named by that
        // count (e.g. "a 1000 diamond ring"). See design spec D4 / §9.
        if (context is { PendingGemType: not GemType.None, PendingGemCount: > 0 })
        {
            var gemItemType = GetGemItemType(context.PendingGemType);
            var gemCount = context.PendingGemCount;

            if (gemItemType != null && from.Backpack?.ConsumeTotal(gemItemType, gemCount) == true)
            {
                GemType = context.PendingGemType;
                GemCount = gemCount;
            }
            else
            {
                // Gems were no longer available (or unknown type): craft a plain piece
                // rather than naming it for gems that were never consumed.
                from.SendAsciiMessage("You lack the gemstones to set into this piece.");
            }

            context.PendingGemType = GemType.None;
            context.PendingGemCount = 0;
        }
```

- [ ] **Step 2: Run the Task 2 test to verify it passes**

Run:
```bash
dotnet test Projects/UOContent.Tests/UOContent.Tests.csproj --filter "FullyQualifiedName~T2AJewelGemCraftTests.OnCraft_ConsumesEntireTargetedGemStack_AndNamesByCount" -c Debug
```
Expected: PASS.

- [ ] **Step 3: Add a null-safety regression test (B3)**

Append this `[Fact]` to `T2AJewelGemCraftTests.cs` (above the closing brace):
```csharp
    [Fact]
    public void OnCraft_WithUnsetGemContext_LeavesPlainPiece()
    {
        var map = Map.Felucca;
        var player = CreatePlayerMobile(map, new Point3D(4120, 500, 0));

        try
        {
            player.Backpack.AddItem(new IronIngot(10));

            var system = DefTinkering.CraftSystem;
            var context = system.GetContext(player);
            context.PendingGemType = GemType.None; // no gem targeted
            context.PendingGemCount = 0;

            var ring = new GoldRing();
            ring.OnCraft(1, false, player, system, typeof(IronIngot), null, MakeRingRecipe(), 0);

            Assert.Equal(GemType.None, ring.GemType);
            Assert.Equal(0, ring.GemCount);
        }
        finally
        {
            player.Delete();
        }
    }
```

- [ ] **Step 4: Run both jewelry tests**

Run:
```bash
dotnet test Projects/UOContent.Tests/UOContent.Tests.csproj --filter "FullyQualifiedName~T2AJewelGemCraftTests" -c Debug
```
Expected: 2 PASS.

- [ ] **Step 5: Commit**

```bash
git add Projects/UOContent/Items/Jewels/BaseJewel.cs Projects/UOContent.Tests/Tests/Items/Jewels/T2AJewelGemCraftTests.cs
git commit -m "fix(t2a-craft): consume full gem stack for jewelry; null-safe gem type (B3a/B3)"
```

---

## Phase 3 — Inscription consumption verification (AI-1)

> **Design ref:** spec §8. Confirmed reality: reagents + blank scroll consumed on success *and* failure; mana only on success. We verify Jack's path honors this; if it does, document; if not, fix.

### Task 4: Verify (and if needed, correct) inscription consumption

**Files:**
- Read: `Projects/UOContent/Skills/Inscribe.cs`, `Projects/UOContent/Engines/Craft/DefInscription.cs`, `Projects/UOContent/Engines/Craft/Core/CraftItem.cs` (`ConsumeRes`/failure path)

- [ ] **Step 1: Trace the consumption path**

Read the three files and answer in writing (paste into the commit message or a scratch note):
- On a **failed** scroll inscription, are the reagents consumed? (Look for the `isFailure` branch in `CraftItem.ConsumeRes`/`CompleteCraft` and whether reagents are in the resource list vs. consumed separately.)
- Is the blank scroll consumed on failure?
- Is mana consumed only on success?

- [ ] **Step 2: Decide**

- If all three already hold → no code change. Proceed to Step 3 (documentation only).
- If any is wrong → write a failing test first (model it on `T2AJewelGemCraftTests` using a scribe player with reagents + `BlankScroll`, invoking the inscription craft and asserting backpack counts after a forced failure via `PredictableRandom`), then fix the minimal logic, then make it pass. Do **not** skip the test.

- [ ] **Step 3: Record the finding**

Append a short "Inscription consumption — verified" note to the design spec §8 (replace the "verify" action item AI-1 with the confirmed behavior and file:line references).

- [ ] **Step 4: Build + commit**

```bash
dotnet build ModernUO.slnx -c Debug
git add -A
git commit -m "verify(t2a-craft): inscription consumes reagents+scroll on success/failure (AI-1)"
```

---

## Phase 4 — Gating coherence guard (spec §5.4)

### Task 5: Warn when `T2ACraftMenus` is enabled outside the T2A era

**Files:**
- Modify: `Projects/UOContent/Engines/FeatureFlags/FeatureFlagManager.cs`

- [ ] **Step 1: Locate the flag application**

Find the `"t2a_craft_menus" => ContentFeatureFlags.T2ACraftMenus = enabled,` arm added in Task 1.

- [ ] **Step 2: Add a startup coherence warning**

In whatever method finalizes/loads flags (the method containing the switch, after flags are applied — or a dedicated validation pass if one exists), add:
```csharp
        if (ContentFeatureFlags.T2ACraftMenus && Core.UOTD)
        {
            var logger = LogFactory.GetLogger(typeof(FeatureFlagManager));
            logger.Warning(
                "T2ACraftMenus is enabled but the expansion is UO:TD or later (Core.UOTD=true). " +
                "T2A packet menus are intended for pre-UO:TD eras; era-specific mechanics " +
                "(half-on-failure, metal-color suppression) will not apply."
            );
        }
```
Ensure `using Server.Logging;` is present (per CLAUDE.md rule 2 — no `Console.WriteLine`).

- [ ] **Step 3: Build**

Run:
```bash
dotnet build ModernUO.slnx -c Debug
```
Expected: succeeds.

- [ ] **Step 4: Commit**

```bash
git add Projects/UOContent/Engines/FeatureFlags/FeatureFlagManager.cs
git commit -m "feat(t2a-craft): warn when T2ACraftMenus is enabled outside the T2A era"
```

---

## Phase 5 — Quality, build, and verification

### Task 6: Convention audit sweep

**Files:** all touched `.cs` under `Projects/`.

- [ ] **Step 1: Enable and run the code-audit skill**

Copy the audit skill and run it over the diff:
```bash
cp dev-docs/claude-skills/modernuo-code-audit.md .claude/skills/
```
Then audit `git diff origin/main...HEAD --name-only -- '*.cs'` against CLAUDE.md rules — focus on: no Tier-3 LINQ on craft hot paths (menu `BuildFilteredEntries`, `T2ACraftSystem.CanCraftItem`), `STArrayPool`/`PooledRefList` where lists are built per craft, braces on all control flow, no `Console.WriteLine`, `ValueStringBuilder` over `StringBuilder`, and confirm the gem/hue context fields are not serialized.

- [ ] **Step 2: Fix only clear violations**

Apply fixes for definite violations (warnings-only items get noted, not force-fixed, per the skill). Keep each fix minimal.

- [ ] **Step 3: Build + commit any fixes**

```bash
dotnet build ModernUO.slnx -c Debug
git add -A
git commit -m "chore(t2a-craft): convention audit fixes"
```
(Skip the commit if the audit found nothing to change.)

### Task 7: Full build + test suite

- [ ] **Step 1: Clean build**

Run:
```bash
dotnet build ModernUO.slnx -c Debug
```
Expected: 0 errors.

- [ ] **Step 2: Full test run**

Run:
```bash
dotnet test Projects/UOContent.Tests/UOContent.Tests.csproj -c Debug
```
Expected: all green, including the new `T2AJewelGemCraftTests` (and any inscription test from Task 4). If pre-existing unrelated tests fail, confirm they also fail on clean `origin/main` before attributing to this work.

### Task 8: Manual in-client verification checklist (cannot be unit-tested)

> Packet-menu UX requires a running shard + T2A client. Enable the flag in the feature-flags config (`t2a_craft_menus: true`) on a pre-UO:TD expansion, then verify:

- [ ] Each of the 8 skills opens its menu (smith, tailor, tinker, carpentry, alchemy, fletching, inscription, cartography); empty-menu guard fires when lacking skill/materials.
- [ ] Double-click tool → "target tool for make-last / any item to begin"; targeting the tool repeats the last craft (incl. jewelry gem re-prompt).
- [ ] Menu filtering hides items the player can't make (skill + materials, including resources in sub-containers).
- [ ] **Jewelry: target a stack of N gems → piece is named "a N <gem> <piece>" and exactly N gems are consumed** (the headline fix).
- [ ] Cartography consumes blank **maps only** (flag on); flip flag off → consumes maps **and** scrolls.
- [ ] Tailoring consumes only the targeted-hue cloth/leather; hue carries to the product.
- [ ] Maker's-mark prompt appears for exceptional items.
- [ ] Failed non-scroll craft consumes **half** resources.
- [ ] Inscription: reagents + scroll consumed on success **and** failure; mana only on success.
- [ ] Flag **off**: gump crafting is unchanged.

---

## Phase 6 — Finish

### Task 9: Push and open PR (when the above is green)

- [ ] **Step 1: Push the branch**

```bash
git push -u origin feat/t2a-crafting-menus
```

- [ ] **Step 2: Open the PR**

Use the design spec as the PR body source. Reference that this supersedes #2181 (Delphi) and #2381 (Jack), credits both authors, and links the design doc. Use the `finishing-a-development-branch` skill to choose merge/PR options.

---

## Self-Review (completed by plan author)

**Spec coverage:** §1 topology → Task 1. §3/§4 mechanics → carried by cherry-pick + verified in Tasks 4/8. §5.4 gating → Task 5. §6 menus → cherry-pick (Task 1) + manual verify (Task 8). §7 engine → cherry-pick. §9 jewelry/D4 → Tasks 2–3. §8 inscription/AI-1 → Task 4. §11 bugs: B3a → Task 3; B3 → Task 3; B1/B2/B4/B5 → these were assessed benign/intended in the spec and are covered by Task 8 manual checks (make-last, gem cancel, hue scope) — no code change required. §12 plan/tests → Tasks 1,7,8. Convention sweep (§11) → Task 6. **No gaps.**

**Placeholder scan:** none — all code/commands are concrete. Task 4 is intentionally a verify-then-conditionally-fix task with an explicit "write the failing test first" instruction (not a placeholder).

**Type consistency:** `OnCraft(int, bool, Mobile, CraftSystem, Type, BaseTool, CraftItem, int)`, `CraftItem(Type, TextDefinition, TextDefinition)`, `AddRes(Type, TextDefinition, int, TextDefinition)`, `Diamond(int)`, `IronIngot(int)`, `GoldRing()`, `GemType.Diamond/None`, `context.PendingGemType/PendingGemCount`, `GemCount` — all verified against Jack's branch source.
