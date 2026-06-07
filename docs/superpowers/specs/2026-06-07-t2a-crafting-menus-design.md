# T2A Crafting Menus — Definitive Design

- **Date:** 2026-06-07
- **Status:** Draft for review
- **Owner:** Kamron Batman
- **Scope:** Finalize ModernUO's pre-UO:Third-Dawn ("T2A" / The Second Age) packet-based crafting menus, reconciling two community branches against current `main`.
- **Feature flag:** `ContentFeatureFlags.T2ACraftMenus` (default off)

---

## 1. Goal & Context

ModernUO's crafting UI is the modern, universal **gump** interface (the one OSI shipped in Publish 14, 2001-11-30). For T2A-era shards we want the **authentic packet-based item-list menus** that predated it: double-click a tool → a skill-and-material-filtered list window → target your resource → the item is made.

Two community members built this:

- **Delphi** — `delphi/T2A_CraftingMenus` (PR #2181, open). Original effort, *not* rooted in historical authority.
- **Jack (UOLL)** — `jackuoll/t2a_crafting_menus` (PR #2381, closed — "still in internal testing"). Did the deep-dive research and corrections.

### Git topology (decisive for the plan)

```
484381109  "Add: T2A Crafting Gumps and Menus for all core skills"   <-- shared base
   |
   ├── delphi/T2A_CraftingMenus  = 484381109 + 2 merge commits (Kamron's WIP main-integration)
   └── jackuoll/t2a_crafting_menus = 484381109 + 12 fix/add commits  <-- superset
```

Jack's branch **is** Delphi's exact base commit plus twelve follow-up commits that fix and extend it. Delphi's branch contributes **no unique feature code** beyond that shared base — only two merge commits from Kamron's unfinished attempt to update it against newer `main`.

**Therefore** "cherry-pick and reconcile both branches against newest main" reduces in practice to: **replay Jack's 13 commits onto current `main`, resolve conflicts, fix the known issues.** Delphi's branch is retained only as a reference for conflict resolution against newer `main`.

> **Authority rule (from the user):** Jack's PR is the correct *mechanic*, barring bugs and logic errors. This document treats Jack's implementation as the implementation baseline, validated against independent historical research (§3), with divergences explicitly decided in §4.

---

## 2. Research method & sources

An independent deep-research pass (5 search angles → 18 sources fetched → 62 claims extracted → 25 adversarially verified, 3 votes each → 22 confirmed, 3 killed) grounded this design. Confidence labels below reflect that verification.

**Source quality, honestly stated.** Packet-structure facts are strong (multiple period packet guides + POL + Wolfpack + ModernUO's own code cross-confirm). Maker's-mark/exceptional/bowcraft rest on a **primary OSI patch note (1998-11-10)**. Most *mechanical* era details (tinkering gems, inscription, resource consumption) rest on the **UOSecondAge (UOSA) freeshard wiki** and its forums — secondary era-reconstruction sources, mutually consistent but reflecting one reference shard's interpretation of authenticity. For a T2A-targeted server this is an acceptable and appropriate authority; it is not OSI primary documentation.

Full source list in §13.

---

## 3. Confirmed reality baseline

| Mechanic | Confidence | Basis |
|---|---|---|
| Crafting used the **generic `0x7C` Open-Dialog menu** (server→client) + **`0x7D` 13-byte response** (client→server), *not* a craft-specific protocol. These already exist in ModernUO (`OutgoingMenuPackets`, `IncomingPlayerPackets`). | High | Jerrith/torfo, POLserver, Wolfpack, MUO code |
| `0x7D` is fixed 13 bytes: cmd(1) + dialogID(4) + menuid(2) + **1-based index(2)** + model(2) + hue(2). MUO converts index to 0-based on receipt. | High | torfo, POL, Wolfpack, MUO code |
| Tool skills: **double-click tool → skill-filtered item-list menu → target the resource** (e.g. ingots). Available items scale with skill. | High | UOSA (period OSI text), UOSA wiki |
| Tinkered jewelry = ingots + a targeted gem; gem **type** names the product ("a diamond necklace"). *(Gem **count**: UOSA reconstruction said single/unstacked, but ModernUO implements the T2A **stacked-gem** behavior — full targeted stack consumed and named by count. See D4 / §9.)* | High (type) · Authority (count) | UOSA, UOGuide + shard authority |
| **Inscription is tool-less** — spellbook (must already contain the spell) + blank scroll + reagents, invoked from the **skill list**, not a tool double-click. | High | UOSA, UOGuide |
| Inscription consumption: **reagents + blank scroll always consumed on success *and* failure; mana only on success.** Magery gates which circles are available; **Inscription** governs success. | High | UOSA (+4 corroborating) |
| **Maker's mark + exceptional are tied to GM / near-GM skill**; a GM crafting an exceptional item appends "crafted by (name)". | High | **Primary OSI patch note 1998-11-10** |
| The packet menus were replaced by the **universal gump in Publish 14 (2001-11-30)**. The claim that this coincided with **UOR was explicitly refuted**. | High | uo.com, UOGuide |

### What research could NOT establish (open questions)

These are the seams where we are reconstructing rather than reproducing:

1. **Non-scroll resource-consumption-on-failure** (smith/tailor/carpentry: all/half/none) — unestablished. (The always-consume rule was confirmed *only* for scroll scribing.)
2. **Hue propagation** — whether a targeted hued resource carries its hue into the product, and whether only matching-hue material is consumed — no claim survived.
3. **Per-skill menu category/navigation structure** for tailoring, carpentry, alchemy, fletching, cartography — only blacksmithy's flow was concretely confirmed.
4. **Cartography tool requirement, runebook crafting, and "Make Last"** as pre-Publish-14 features — unconfirmed. Notably, **"Make Last" is documented as a Publish 14 *gump* feature**, suggesting it may be anachronistic for the T2A packet-menu era.

---

## 4. Divergence decisions

Where Jack's code outruns or contradicts the record, these are the resolved calls for this branch:

| # | Item | Research status | **Decision** |
|---|---|---|---|
| D1 | **Half-resources on failure** for non-scroll crafts (`if (isFailure && !Core.UOTD) amounts[i] -= amounts[i]/2`) | Unconfirmed for non-scroll | **Keep** Jack's half-on-failure (pre-UOTD), documented as best-known T2A reconstruction, not OSI-confirmed. |
| D2 | **"Make Last"** via targeting the tool (`T2ACraftToolTarget`) | Likely Publish-14 (gump) feature; no T2A evidence | **Keep as QoL**, explicitly flagged in-doc as a non-authentic convenience layered on the T2A flow. |
| D3 | **Hue-aware tailoring** (targeted hue → product; only matching-hue consumed) | Unverified | **Keep**, flagged as reconstruction. Already plumbed through `CraftItem`. |
| D4 | **Jewelry gem count & naming** — how many gems a piece consumes and how it's named | UOSA reconstruction said *single unstacked* gem; Jack's code **deliberately** consumes 1 but names by stack size | **Override (shard authority):** implement the **stacked-gem** mechanic — the player targets a gem stack (up to a full stack, ~60000); the **entire targeted stack is consumed** and the piece is named by count ("a 1000 diamond ring"). Metal cost stays **2 ingots**. This corrects Jack's intentional "consume 1, name by stack" choice. See §9. |
| D5 | **Skill scope** | T2A had no crafting menu for cooking | **8 skills** (smith, tailor, tinker, carpentry, alchemy, fletching, inscription, cartography). Cooking is out of scope — it had no T2A packet menu. |

These decisions are intentionally conservative-to-Jack: we preserve his working mechanics and label the unverified ones, rather than re-litigate behavior that plays correctly on T2A shards.

---

## 5. Architecture

### 5.1 Wire protocol (reuse — no new packets)

ModernUO already implements the `0x7C`/`0x7D` menu packets. The T2A craft UI is built entirely on the existing `Server.Menus.ItemLists.ItemListMenu` / `Server.Menus.Questions.QuestionMenu` abstractions. No new packet types are introduced; Jack's server-side changes are **additive** to these classes.

Server-menu changes carried from Jack (`Projects/Server/Menus/`):

- **`ItemListEntry`** gains a 4th constructor arg **`CraftIndex`** — an index into the menu's parallel `Type[]` so the response handler can map a 1-based menu selection back to a craftable type. (`main` currently has only `name, itemID, hue`.)
- **`ItemListMenu.Entries`** exposed with a setter (menus need to rebuild/inspect entry count for empty-menu validation).
- **`BaseMenu`** gains `HasSent` tracking to prevent double-send.
- **`IncomingPlayerPackets.MenuResponse`** validates the response serial matches the player's current pending menu, and iterates `state.Menus` by index with a `(uint)` serial compare (avoids sign extension on the high-bit menu serial).

> **Audit note:** these `Projects/Server/Menus/` edits are the *only* engine-layer changes. CLAUDE.md says do not modify `Projects/Server/` without explicit request — **explicitly authorized by the shard owner (Kamron) on 2026-06-07** to accept Jack's minimal additive changes (`CraftIndex` on `ItemListEntry`, `Entries` setter, `HasSent` guard). Keep them minimal and additive.

### 5.2 Component map (`Projects/UOContent/Engines/Craft/T2A/`)

| Component | Responsibility |
|---|---|
| `T2ACraftSystem` (static) | Central router. `ShowMenu(from, system, tool, preTarget)` dispatches per `CraftSystem` to the right resource-selection / menu flow. Hosts shared helpers: `CanCraftItem`, `FilterEntries`, `AnyCraftableInCategory`, `SetLastResourceIndex`, resource-equivalence (`Log↔Board`, `Cloth↔UncutCloth`, `Leather↔Hides`). |
| `T2ACraftToolTarget` (Target) | The initial prompt after double-clicking a tool. Target **the tool itself** → Make-Last (D2); target **any other item** → pass it as `preTarget` into `ShowMenu`. |
| `*Menu` (one per skill) | `ItemListMenu` subclasses: `AlchemyMenu`, `BlacksmithMenu`, `BowFletchingMenu`, `CarpentryMenu`, `CartographyMenu`, `InscriptionMenu`, `TailoringMenu`, `TinkeringMenu`. Each builds skill/material-filtered entries, drives category submenus, and on response either descends a category or crafts. |

**Single-purpose boundaries.** Each menu owns only its category tree and entry formatting; all *craft-eligibility* logic lives in `T2ACraftSystem.CanCraftItem` (skill gate + material count, sub-resource aware), and all *consumption* lives in `CraftItem`. A menu can be understood and changed without touching the craft engine.

### 5.3 Interaction flow

```
Double-click tool (BaseTool.OnDoubleClick)
  └─ if ContentFeatureFlags.T2ACraftMenus:
        from.Target = new T2ACraftToolTarget(tool, system)
        "Target this tool to make last item, or any other target to begin crafting."
        ├─ target == tool      → Make-Last (D2)
        └─ target == item/null → T2ACraftSystem.ShowMenu(from, system, tool, preTarget)
                                    ├─ resource pre-selected/targeted (per skill)
                                    ├─ build filtered menu; if empty → "You lack the skill and materials…"
                                    └─ SendMenu → 0x7C → player picks → 0x7D → OnResponse
                                          ├─ category   → open submenu
                                          └─ leaf item  → CraftItem.Craft(...)
```

Skill-list-invoked skills (Inscription, Cartography) enter the same `ShowMenu` flow from their skill handler instead of a tool double-click (they are tool-less in T2A — §8).

### 5.4 Gating model — flag vs era (the coherence audit)

Two independent switches exist. This is the reconciled, intended model:

| Switch | Meaning | Governs |
|---|---|---|
| **`ContentFeatureFlags.T2ACraftMenus`** | "Use packet menus instead of gumps." A **UI-system** switch. | All menu routing, `ShowCraftMenu` choosing message-only vs gump reopen, tool-less inscription/cartography, jewelry gem-targeting flow, always-prompt maker's mark, `BlankMap/BlankScroll` equivalence suppression. |
| **`Core.UOTD`** (era/expansion) | The T2A↔UO:TD **era boundary**. `Core.UOTD == false` ⇒ T2A-or-earlier. | Era *mechanics*: half-on-failure (`!Core.UOTD`), tinkering metal-color retention disabled (`!Core.UOTD`), pre-AOS recipe availability. |

**Intended deployment:** a T2A shard sets expansion ≤ T2A (so `Core.UOTD == false`) **and** enables `T2ACraftMenus`. The flag drives UI; the era drives mechanics. They are expected to move together.

**Known incoherence risk (to resolve in implementation):** the two can be set inconsistently — e.g. `T2ACraftMenus` on while `Core.UOTD == true`. That would give packet-menu UI with UO:TD-era mechanics (no half-on-failure, metal color retained). This is not a crash, but it is a nonsensical config.

**Resolution:** Document the invariant ("T2A menus are intended for `Core.UOTD == false`") and add a startup warning in `FeatureFlagManager` if `T2ACraftMenus` is enabled while `Core.UOTD` is true. We do **not** hard-couple the flag to the era (keeps the flag testable and lets a shard opt into the UI independently), per the approved "keep dual gate, audit coherence" decision.

---

## 6. Per-skill menu design

Each menu is an `ItemListMenu` with a private `Category` enum; entries carry a `CraftIndex` into a parallel `Type[]`. Entries are built **filtered** (only items the player can currently make, via `T2ACraftSystem.CanCraftItem`), and category rows are shown only if `AnyCraftableInCategory` is true. Static entry templates are cached (`??=`).

### 6.1 Blacksmithy (`BlacksmithMenu`) — *the template*
- **Resource-first (Lost Lands flow):** ingot type is selected/targeted *before* the menu opens (`ResourceSelection`). If a repairable item is targeted, auto-select Iron; if only one ingot type is available, auto-select it; else prompt `BlacksmithResourceTarget`.
- **Two-level tree:** Main → {Repair, Smelt actions} + {Weapons, Armor, Shields} → leaf. Weapons → {Blades, Axes, Maces, Polearms}; Armor → {Ringmail, Chainmail, Platemail, Helmets}.
- **Repair/Smelt** integrate directly with `Repair.Do` / `Resmelt.Do`.
- Entry label format: `"<name> (<amt> ingots)"`.

### 6.2 Tailoring (`TailoringMenu`)
- **Hue-aware resource selection (D3):** target cloth/leather/hides; the targeted **hue is carried** through the menu and into the craft. Categories: Main, LeatherMain, Hats, Shirts, Pants, Footwear, Misc, Leather, Studded, Female.
- Recognizes the cloth branch (`Cloth`, `UncutCloth`) vs the armor branch (`Leather`, `Hides`).
- Adds `BoltOfCloth` recipe when `!Core.AOS`.

### 6.3 Tinkering (`TinkeringMenu`)
- Resource targeting: `Log/Board` → Wood, `BaseIngot` → ingots, `Keg` → keg.
- Categories: Main, Wood, Tools, Parts, Utensils, Traps, Misc, Jewelry → {Necklaces, Earrings, Rings}, Keg.
- **Jewelry gem targeting (D4):** selecting a jewelry leaf clears `context.PendingGemType/Count`, then prompts `GemSelectTarget`; on target, `BaseJewel.GetGemType` resolves the gem type and `GemSelectTarget` captures the **full targeted stack size** (`gemItem.Amount`) into `context.PendingGemCount`. `BaseJewel.OnCraft` then **consumes that entire count** and stores it as `GemCount`, so the piece is named by quantity ("a 1000 diamond ring"). Metal-color retention disabled when `!Core.UOTD`. AOS gem-resource jewelry recipes are skipped under the flag; T2A ingot-based jewelry added instead (Gold/Silver Necklace/Earrings/Ring, WeddingRing).

### 6.4 Carpentry (`CarpentryMenu`)
- Wood resource targeting. Categories: Main, Furniture, Containers, Weapons, Instruments, Misc, Addons.
- Label format `"<name> (<X> wood)"` or `"… (<X> wood, <Y> ingots)"` for mixed-resource items.

### 6.5 Alchemy (`AlchemyMenu`)
- Reagent/Bottle targeting (or direct if `preTarget` is a reagent/bottle/null). Categories by potion family (Refresh, Agility, NightSight, Heal, Strength, Poison, Cure, Explosion). Requires a Bottle to craft. Fixed hue 0.

### 6.6 Bowcraft/Fletching (`BowFletchingMenu`)
- Wood/feather/shaft targeting. Flat list: Kindling, Shaft, Arrow, Bolt, Bow, Crossbow, HeavyCrossbow.

### 6.7 Inscription (`InscriptionMenu`) — tool-less (§8)
- Categories: Main → Circle1…Circle8, plus Runebook. 8 spells per circle (offset = circle×8 into the flat `CraftItems`).
- Per-spell gate: must have a `BlankScroll`, and **the spell must already be in the player's spellbook** (`HasSpellInBook`). Spell IDs cached in a static `Dictionary<Type,int>`.
- **Runebook** special-cased: target a `RecallRune` → craft Runebook directly.

### 6.8 Cartography (`CartographyMenu`) — tool-less (§8)
- Blank-map targeting. Flat list: local/city/sea-chart/world map. Resource hardcoded to `typeof(BlankMap)`.

---

## 7. Core craft-engine changes (`Engines/Craft/Core/`)

### `CraftItem.cs` (largest change, ~+695)
- **`m_TypesTable` → `InitTypesTable()`**: the `BlankMap ↔ BlankScroll` equivalence pair is added **only when `!T2ACraftMenus`** (gump clilocs reference both). Under T2A this equivalence is removed, fixing cartography consuming a backpack blank *scroll* instead of a sub-container blank *map* (breadth-first search order bug). ✅ research-aligned (distinct resources).
- **Half-on-failure (D1):** `if (isFailure && !Core.UOTD) amounts[i] -= amounts[i] / 2;`
- **Hue-aware path (D3):** new `Craft(from, system, typeRes, tool, resHue)` overload; `CheckHuedRes`/`ConsumeHuedRes`/`GetHuedAmount`/`ConsumeHuedAmount` filter the **primary** resource by hue (secondaries consumed normally); `InternalTimer` carries `m_ResHue`; new hue-aware `CompleteCraft` overload sets `context.LastHue`.
- **`ShowCraftMenu(from, system, tool, ...)` static**: replaces direct `CraftGump` construction everywhere. T2A path = send message, no reopen; UOTD path = send gump.
- **Maker's mark gating:** `if (makersMark && (T2ACraftMenus || context.MarkOption == PromptForMark))` — T2A **always prompts** (no auto/never toggle). Uses the shared `QueryMakersMarkGump` (the separate `QueryMakersMarkMenu` is removed). Tool-null guards added around `UsesRemaining--` for tool-less skills.

### Supporting
- `CraftContext`: `+ int LastHue = -1`, `+ GemType PendingGemType`, `+ int PendingGemCount`.
- `CraftSystem`: `+ virtual bool RequiresTool => true`; `+ CreateItem(..., hue)` overload.
- `Repair.cs`, `Resmelt.cs`: route through `ShowCraftMenu` instead of `CraftGump`.
- `QueryMakersMarkGump`: `+ resHue` param; now serves both T2A and gump crafting.

### Def files
- `DefCartography` / `DefInscription`: `+ override bool RequiresTool => !T2ACraftMenus`; `CanCraft` wraps tool validation in `if (RequiresTool)`.
- `DefTinkering`: `RetainsColorFrom` gated `!Core.UOTD`; `InitCraftList` swaps AOS jewelry for T2A ingot jewelry under the flag.
- `DefTailoring`: `+ BoltOfCloth` when `!Core.AOS`.
- `DefAlchemy`: idempotent `Initialize` guard.

---

## 8. Tool-less inscription & cartography

Per confirmed research, **inscription is tool-less** in T2A (and we extend the same to cartography's skill-use). Implementation:

- `Skills/Inscribe.cs`: `OnUse` → `T2AInscribeTarget` accepts a `BlankScroll` (open menu), a `RecallRune` (craft Runebook), or a `BaseBook` (copy flow). `IsEmpty` refactored to indexed loops (no LINQ). UOTD path keeps the legacy book-copy flow.
- `Skills/Cartography.cs`: `OnUse` → `ShowMenu(m, DefCartography.CraftSystem, tool: null)`.
- `CraftSystem.RequiresTool == false` for both under the flag; `CraftItem` tool-null guards prevent `UsesRemaining` decrement.

**Inscription consumption invariant (✅ confirmed) — AI-1 VERIFIED 2026-06-07:** reagents + blank scroll consumed on success *and* failure; mana only on success; Magery gates circle availability, Inscription governs success. Trace result:
- **Mana only on success** ✅ — `CraftItem.ConsumeAttributes(...)` is called with `consume:true` only on the success path (`CraftItem.cs` ~1387); it is never called on the skill-check failure path (~1582–1585).
- **Reagents + blank scroll on failure** ✅ — on failure, `ConsumeRes(..., isFailure:true)` runs with `ConsumeType.All` (scroll recipes don't set `UseAllRes`). The half-on-failure line `amounts[i] -= amounts[i] / 2` (`CraftItem.cs` ~684) fires, but **every spell-scroll reagent and the blank scroll are quantity 1**, and integer `1/2 == 0`, so the subtraction is a no-op → they are **fully consumed**. Matches the confirmed rule.
- **Runebook** uses 8 blank scrolls and therefore loses 4 on failure (`8 - 8/2`). A Runebook is a *non-scroll* craft, so this is **consistent with D1** (half-on-failure for non-scroll crafts), not a violation.

> **Latent fragility (not a current bug; noted for review):** spell-scroll correctness relies on the amount-1 coincidence above. If any future spell-scroll recipe used a reagent amount ≥ 2, the half-on-failure line would wrongly halve it. Optional hardening if desired: add an opt-in `CraftItem.AlwaysConsumeFullOnFailure` flag set on scroll recipes and skip the half-deduction when set (blast radius: `CraftItem.ConsumeRes` + `DefInscription` only). **Deliberately not implemented** during this reconciliation to keep the shared failure logic untouched.

---

## 9. Jewelry, gems & serialization

- `BaseJewel` serialization **bumped v4 → v5**: `+ [SerializableField(7)] int _gemCount`. Migration `MigrateFrom(V4Content)` defaults `_gemCount = 0`; migration JSON `Server.Items.BaseJewel.v5.json` adds the `GemCount` property. `WeddingRing` is new (`SerializationGenerator(0)`).
- `GemSelectTarget` captures the **full targeted stack** (`gemItem.Amount`, up to ~60000) into `context.PendingGemCount`.
- `OnCraft`: reads `context.PendingGemType/Count`, **consumes the full count** (`ConsumeTotal(gemItemType, PendingGemCount)`), sets `GemType`/`GemCount`, clears the pending context.
  - **⚠ Jack's branch deliberately consumes only `1`** (`ConsumeTotal(gemItemType, 1)`) while naming the piece by the full stack count — i.e. it names a "1000 diamond ring" but pockets 999 diamonds. **This must change (B3a):** consume `PendingGemCount`, and verify the gems are reachable before crafting (fail with a message if not, so the piece isn't named for gems that weren't consumed).
- `OnSingleClick` (T2A): "a ring with a sapphire" / "a ring with 1000 sapphires" via `GetGemName` (pluralized) and `_gemCount` (serialized field 7 — count persists across save/load).
- Helpers: `GetGemType(Item)`, `GetGemItemType(GemType)`, `GetGemName(GemType, plural)`.

> **Bug to fix (B3):** `GetGemItemType` returns null for unknown gems → `ConsumeTotal(null, …)` fails silently with no player feedback. Add a guard + message.

---

## 10. Maker's mark & exceptional (✅ confirmed)

Exceptional quality and the maker's mark are tied to GM/near-GM skill; a GM exceptional craft appends "crafted by (name)". Under T2A menus the system **always prompts** for the mark (no auto/never option), via the shared `QueryMakersMarkGump`. This matches the era behavior and Jack's implementation.

---

## 11. Known issues to address during reconciliation

From the implementation audit; fix as part of building the definitive branch:

- **B1 — `InscriptionMenu._spellIds`** static cache grows unbounded. Bounded (one entry per spell type, finite) → acceptable; document, no action required.
- **B2 — Hue filtering scope:** tailoring hue-filters only the *primary* resource; secondaries (e.g. ingots in mixed items) consume normally. Confirm this is intended (D3) and document.
- **B3 — `GetGemItemType` null-safety** (see §9).
- **B3a — Gem consumption count (must fix, per D4):** `OnCraft` deliberately consumes only 1 gem but names the piece by the full targeted stack (`GemCount`). It must consume `PendingGemCount` and verify availability so the name matches what was consumed. (See §9.)
- **B4 — Make-Last jewelry path:** `T2ACraftToolTarget` re-prompts `GemSelectTarget`; if the player cancels, flow returns to menu rather than crafting. Confirm acceptable; document the cancel behavior.
- **B5 — `GemSelectTarget` timeout/cancel:** `OnTargetCancel` reopens the menu (no loop). Confirm.
- **AI-1 — Inscription consumption VERIFIED (§8):** spell scrolls fully consume reagents+scroll on success/failure; mana only on success; runebook half-on-failure is intended (D1). No code change; latent amount-1 fragility documented for optional future hardening.
- **Convention sweep:** run `modernuo-code-audit` over all touched `.cs` (LINQ tiers, `PooledRefList`, `STArrayPool`, braces, no `Console`, `ValueStringBuilder`). The branch already favors indexed loops and `stackalloc`; verify no Tier-3 LINQ on craft hot paths.

---

## 12. Reconciliation / branch plan

**Conflict surface (measured 2026-06-07):** across 78 commits of `main` drift since Jack's merge-base (`e1ae9706`, 2026-03-15), **only 2 of Jack's 39 files also changed on `main`** — `ContentFeatureFlags.cs` and `FeatureFlagManager.cs`, both purely additive (new flag + switch arm). No logic conflicts. `BaseJewel` is at v4 on `main` (Jack → v5), so the serialization bump is clean. The rebase is therefore near-trivial; the real work is the bug fixes below, not conflict resolution.

1. **Create** the definitive branch from current `origin/main` (`feat/t2a-crafting-menus` — already created; design doc is its first commit).
2. **Replay Jack's 13 commits** (`merge-base(origin/main, jackuoll/t2a_crafting_menus)..jackuoll/t2a_crafting_menus`) onto it via rebase/cherry-pick, preserving logical history.
3. **Resolve conflicts** against newer `main`, using `delphi/T2A_CraftingMenus`'s two merge commits as a reference for how Kamron began the integration.
4. **Apply fixes** B3, AI-1, and the §5.4 coherence warning; confirm B1/B2/B4/B5.
5. **Run** `modernuo-code-audit` convention sweep + `dotnet build` + relevant tests.
6. **Verify** behavior with the flag on/off (test plan §12.1).

### 12.1 Test plan (carried + extended from PR #2381)

- Flag on: each of the 8 skills opens the correct menu; empty-menu guard fires when lacking skill/materials.
- Make-Last repeats last craft with the correct resource type (incl. jewelry gem re-prompt).
- Menu filtering hides un-craftable items (skill + materials, sub-container resources).
- Cartography consumes **blank maps only** (flag on) but **maps and scrolls** (flag off).
- Tailoring consumes only the targeted-hue cloth/leather.
- Jewelry consumes the **full targeted gem stack** and is named by count ("a 1000 diamond ring"); count persists across save/load (B3a).
- Maker's-mark prompt appears for exceptional items.
- Failed non-scroll craft consumes **half** resources (D1).
- Inscription: reagents + scroll consumed on success and failure; mana only on success (AI-1).
- Flag off: gump crafting is byte-for-byte unchanged.

---

## 13. Open questions & future verification

- **OQ1:** Exact T2A non-scroll failure cost (we chose half — D1). Revisit if OSI-primary evidence surfaces.
- **OQ2:** Exact hue propagation rules (we kept Jack's — D3). Optionally validate against a live UOSA reference shard.
- **OQ3:** Per-skill menu category ordering for non-blacksmith skills is reconstructed; cosmetic, low risk.
- **OQ4:** Whether Make-Last is authentic (we keep it as flagged QoL — D2).

---

## 14. Sources

**Primary / strong:**
- OSI T2A Patch Notes 1998-11-10 (maker's mark, exceptional, breastplates) — UOSA wiki archive
- uo.com Publish 14 (2001-11-30) — universal gump craft interface
- Packet guides: Jerrith/torfo (`uo.torfo.org/packetguide`), JUOPackets mirror, POLserver `0x7C`, Wolfpack `uo_protocol`
- ModernUO source: `IncomingPlayerPackets.cs`, `OutgoingMenuPackets.cs`

**Secondary / era-reconstruction (UOSA reference shard et al.):**
- UOSecondAge wiki: Blacksmithy, Tinkering, Inscription; UOSA forums
- UOGuide: Crafted Jewelry, Maker's Mark, Publish 14, Publishes, Third Dawn

**Refuted (recorded so they don't resurface):**
- 0x7D omitting the trailing color field (1-2)
- Jewelry "1 ingot rings / 2 ingot bracelets" specific split (0-3)
- Craft-menu transition coinciding with UOR rather than Publish 14 (0-3)

---

## 15. Decision log

| Decision | Choice |
|---|---|
| Grounding | Jack's PR as baseline + independent research validation |
| Branch assembly | Rebase Jack's 13 commits onto current `main`; Delphi as conflict reference |
| Gating | Keep dual gate (`T2ACraftMenus` flag + `Core.UOTD` era), audited & documented (§5.4) |
| D1 Failure cost | Keep half-on-failure (flagged reconstruction) |
| D2 Make-Last | Keep as QoL (flagged anachronism) |
| D3 Tailoring hue | Keep (flagged reconstruction) |
| D4 Jewelry | Stacked gems: consume full targeted stack (~60000 max), name by count; 2 ingots metal (corrects Jack's deliberate consume-1) |
| D5 Scope | 8 skills (cooking had no T2A menu) |
