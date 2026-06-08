# T2A Packet-Based Crafting Menus

This document covers ModernUO's **T2A-era crafting menus** — the pre-UO:Third-Dawn, packet-based item-list crafting UI that replaces the modern gump crafting interface when enabled. It is the developer/AI reference for how the system is wired, how to extend it, and how it deviates from authentic T2A behavior.

> Design rationale, historical research, and decision log live in
> `docs/superpowers/specs/2026-06-07-t2a-crafting-menus-design.md`.
> Player-facing usage and quality-of-life notes live in `docs/t2a-crafting/` (not committed).

## Overview

In the T2A era (≈1998–2001, before Publish 14 on 2001-11-30), UO crafting did not use gumps. The server sent the generic `0x7C` "Open Dialog" menu packet and the client replied with the 13-byte `0x7D` response. Double-clicking a crafting tool opened a **skill-and-material-filtered item-list menu**; the player picked a category/item and targeted a resource, and the item was made.

ModernUO reproduces this behind a single startup-read toggle. When `T2ACraftSystem.Enabled` is `false`, crafting uses the normal `CraftGump`. When `true`, the same `CraftSystem`/`CraftItem` definitions are presented through packet menus instead. The value is read once at startup from the `t2aCraftMenus` server setting (default `!Core.UOTD`), so a pre-UO:TD shard gets the T2A menus automatically and a UO:TD-or-later shard gets gumps — with no runtime/admin toggle.

The wire-level menu packets (`0x7C`/`0x7D`) already exist in the engine (`Projects/Server/Network/Packets/OutgoingMenuPackets.cs`, `Projects/UOContent/Network/Packets/IncomingPlayerPackets.cs`) and in `Server.Menus.ItemLists.ItemListMenu` / `Server.Menus.Questions.QuestionMenu`. The T2A feature is a *consumer* of that existing infrastructure, not a new protocol.

## Activation

Toggle: `T2ACraftSystem.Enabled` (static). It is set once in `ExpansionConfiguration.Configure()` from `ServerConfiguration.GetSetting("t2aCraftMenus", !Core.UOTD)` — a read-only setting (the default is **not** written back to the config file). It is intentionally **not** a runtime feature flag and cannot be flipped in-game by admins; change it via the `t2aCraftMenus` server setting and restart.

**Intended deployment:** because the default is `!Core.UOTD`, a pre-UO:TD shard gets T2A menus and the matching era mechanics automatically. The toggle controls the *UI system*; the expansion/era (`Core.UOTD`) controls *mechanics* (see [Gating model](#gating-model-toggle-vs-era)). Since the default tracks the era and there is no runtime override, the two cannot drift into an incoherent combination.

## Architecture

All T2A-specific code lives under `Projects/UOContent/Engines/Craft/T2A/`.

| Type | File | Responsibility |
|---|---|---|
| `T2ACraftSystem` (static) | `T2ACraftSystem.cs` | Central router. `ShowMenu(from, craftSystem, tool, preTarget)` dispatches per craft system to the right resource-selection / menu flow. Hosts shared filtering helpers. |
| `T2ACraftToolTarget` (Target) | `T2ACraftToolTarget.cs` | The first target after double-clicking a tool: target the **tool** → make-last; target **anything else** → begin crafting with that item as `preTarget`. |
| `*Menu : ItemListMenu` | `AlchemyMenu.cs`, `BlacksmithMenu.cs`, `BowFletchingMenu.cs`, `CarpentryMenu.cs`, `CartographyMenu.cs`, `InscriptionMenu.cs`, `TailoringMenu.cs`, `TinkeringMenu.cs` | One menu per skill. Builds filtered entries, drives category submenus, and on response either descends a category or crafts. |

**Separation of concerns:**
- Each `*Menu` owns only its category tree and entry formatting.
- `T2ACraftSystem.CanCraftItem` / `FilterEntries` / `AnyCraftableInCategory` own *craft-eligibility* (skill gate + material count, sub-resource aware, with resource-equivalence: `Log↔Board`, `Cloth↔UncutCloth`, `Leather↔Hides`).
- `CraftItem` / `CraftSystem` own *consumption and item creation*.

### Control flow

```
BaseTool.OnDoubleClick
  └─ if T2ACraftSystem.Enabled:
        from.Target = new T2ACraftToolTarget(tool, system)
        "Target this tool to make last item, or any other target to begin crafting."
        ├─ target == tool      → make-last (repeat context.LastMade; jewelry re-prompts the gem)
        └─ target == item/null → T2ACraftSystem.ShowMenu(from, system, tool, preTarget)
                                    ├─ resource selected/targeted (per skill)
                                    ├─ build filtered menu; empty → "You lack the skill and materials…"
                                    └─ ItemListMenu sent (0x7C) → player picks → 0x7D → OnResponse
                                          ├─ category → open submenu
                                          └─ leaf     → CraftItem.Craft(...)
```

Tool-less skills (Inscription, Cartography) enter `ShowMenu` from their **skill handler** (`Skills/Inscribe.cs`, `Skills/Cartography.cs`) instead of a tool double-click — see [Tool-less skills](#tool-less-skills).

### How a selection maps back to a craftable

`ItemListEntry` carries a `CraftIndex` (a 4th constructor arg added for this feature) — an index into the menu's parallel `Type[]`. When the `0x7D` response arrives, `OnResponse(state, index)` uses the entry's `CraftIndex` to resolve the chosen category or `CraftItem` type. Menus build their entries through `T2ACraftSystem.FilterEntries(from, staticEntries, types, system, selectedResourceType)` so only craftable rows appear.

## Key mechanics

### Resource pre-selection
Tool skills select the working resource **before** the menu opens. `T2ACraftToolTarget` passes whatever the player targeted as `preTarget`; `T2ACraftSystem.ShowMenu` validates it per skill (e.g. ingots for smithing, cloth/leather for tailoring, wood for carpentry/fletching, blank map for cartography, blank scroll/reagent/rune for inscription) and otherwise prompts for a valid resource. The selected sub-resource index is stored via `T2ACraftSystem.SetLastResourceIndex` (`context.LastResourceIndex` / `LastResourceIndex2`) for make-last.

### Make-last (QoL — see deviations)
`T2ACraftToolTarget`: targeting the tool repeats `context.LastMade` with the remembered resource (and hue). Jewelry re-prompts for a gem target (you cannot silently re-consume gems). **Not historically part of T2A packet menus** ("Make Last" was a Publish 14 gump feature) — kept as a quality-of-life convenience.

### Hue-aware tailoring
Targeting hued cloth/leather carries that hue into the product, and only matching-hue material is consumed for the **primary** resource. Implemented by the `CraftItem.Craft(..., resHue)` overload and `CheckHuedRes`/`ConsumeHuedRes`/`GetHuedAmount`/`ConsumeHuedAmount`; the hue rides the `InternalTimer` (`m_ResHue`) into the hue-aware `CompleteCraft` overload, which sets `context.LastHue`. Secondary resources (e.g. ingots in mixed items) are consumed normally.

### Stacked-gem jewelry
Tinkered jewelry consumes ingots + a targeted gem **stack**. The player targets a stack of N gems; `TinkeringMenu.GemSelectTarget` captures `gemItem.Amount` into `context.PendingGemCount` and the gem type into `context.PendingGemType`. `BaseJewel.OnCraft` consumes the **entire** stack (`ConsumeTotal(gemItemType, PendingGemCount)`) and names the piece by count ("a 1000 diamond ring"). The count persists via `_gemCount` (`[SerializableField(7)]`, jewel serialization **v5**) and is shown in `OnSingleClickPreUOTD`. If the gems are unavailable at craft time the piece is left plain and the player is messaged.

### Half-resources on failure (era mechanic)
`CraftItem.ConsumeRes` reduces each resource by half on a failed craft when `!Core.UOTD` (`amounts[i] -= amounts[i] / 2`). Note integer division: amount-1 resources (e.g. each inscription reagent + the single blank scroll) are fully consumed, matching the confirmed scroll-scribing rule; multi-unit resources (e.g. runebook's 8 blank scrolls) lose half.

### Tool-less skills
`DefInscription` and `DefCartography` override `RequiresTool => !T2ACraftSystem.Enabled`, and `CanCraft` wraps tool validation in `if (RequiresTool)`. Inscription is invoked from the skill list (`Inscribe.cs` → `T2AInscribeTarget`: blank scroll opens the menu, recall rune crafts a runebook, a book enters the copy flow); cartography from `Cartography.cs`. `CraftItem` tool-null guards prevent `UsesRemaining` decrement when there is no tool.

### Maker's mark
Under T2A the system **always prompts** for the maker's mark (no auto/never toggle): `CompleteCraft` gates on `makersMark && (T2ACraftMenus || context.MarkOption == PromptForMark)`, using the shared `QueryMakersMarkGump` (the old `QueryMakersMarkMenu` was removed). Exceptional + mark are tied to GM/near-GM skill, as in the era.

## Gating model (toggle vs era)

| Switch | Meaning | Governs |
|---|---|---|
| `T2ACraftSystem.Enabled` (from `t2aCraftMenus` setting, default `!Core.UOTD`) | "Use packet menus instead of gumps." | Menu routing, `ShowCraftMenu` (message vs gump), tool-less inscription/cartography, jewelry gem-targeting flow, always-prompt maker's mark, `BlankMap`/`BlankScroll` equivalence suppression. |
| `Core.UOTD` (expansion/era) | T2A↔UO:TD era boundary (`false` = T2A or earlier). | Era mechanics: half-on-failure, tinkering metal-color suppression, pre-AOS recipe availability. |

Because the toggle's default **is** `!Core.UOTD` and there is no runtime override, the two move together by construction — a pre-UO:TD shard gets both the menus and the era mechanics, and there is no incoherent "menus on / UO:TD era" combination to guard against. An operator can still force the setting explicitly (e.g. menus on a later era) via `t2aCraftMenus`, but that is a deliberate, restart-time choice.

## Extending: add a craftable to a T2A menu

1. Ensure the item has a `CraftItem` in the relevant `Def*.cs` (`AddCraft(...)`), as for gump crafting — the T2A menus read the same `CraftSystem.CraftItems`.
2. Add the item's `Type` to the appropriate category `Type[]` in the skill's `*Menu.cs` and a matching static `ItemListEntry` (name + `ItemID` + `CraftIndex`). Entries are filtered at build time by `T2ACraftSystem`, so you don't repeat skill/material checks.
3. For a new **category**, add a `Category` enum value, a `GetQuestion` arm, a static entries array, and the navigation case in `OnResponse`. `BlacksmithMenu.cs` is the canonical template.
4. Jewelry: gem-bearing pieces flow through `TinkeringMenu.GemSelectTarget` and `BaseJewel.OnCraft`; ensure `BaseJewel.GetGemType`/`GetGemItemType` cover any new gem.

## Gotchas

- **Resource equivalence is era-gated.** `CraftItem.InitTypesTable()` only treats `BlankMap`/`BlankScroll` as interchangeable when `!T2ACraftMenus` (the gump clilocs reference both). Under T2A they are distinct, so cartography consumes blank *maps*, not scroll s.
- **Transient context fields are not serialized.** `CraftContext.PendingGemType`, `PendingGemCount`, and `LastHue` are plain properties (no `[SerializableField]`) — they exist only during a craft.
- **`BaseJewel` is at serialization v5.** Bumping it again requires `MigrateFrom(V5Content)` per the serialization rules.
- **Menu entry creation uses reflection in one spot.** `T2ACraftSystem.ShowMenuDirect<T>` uses `Activator.CreateInstance` (once per tool double-click). Fine for now; convert to a compiled factory if it ever shows up hot.

## Files

- T2A UI: `Projects/UOContent/Engines/Craft/T2A/*.cs`
- Engine glue: `Projects/UOContent/Engines/Craft/Core/{CraftItem,CraftContext,CraftSystem,Enhance,Repair,Resmelt,CraftGumpItem,QueryMakersMarkGump}.cs`
- Defs: `Projects/UOContent/Engines/Craft/Def{Alchemy,Cartography,Inscription,Tailoring,Tinkering}.cs`
- Skills: `Projects/UOContent/Skills/{Inscribe,Cartography}.cs`
- Items: `Projects/UOContent/Items/Jewels/{BaseJewel,Ring}.cs` (+ `Migrations/Server.Items.BaseJewel.v5.json`)
- Tool entry: `Projects/UOContent/Items/Skill Items/Tools/BaseTool.cs`
- Toggle: `T2ACraftSystem.Enabled` (in `Engines/Craft/T2A/T2ACraftSystem.cs`), set from `Projects/UOContent/Configuration/ExpansionConfiguration.cs` via `ServerConfiguration.GetSetting("t2aCraftMenus", !Core.UOTD)`
- Engine menus (additive): `Projects/Server/Menus/{BaseMenu,ItemListMenu,QuestionMenu}.cs`; response: `Projects/UOContent/Network/Packets/IncomingPlayerPackets.cs`
- Tests: `Projects/UOContent.Tests/Tests/Items/Jewels/T2AJewelGemCraftTests.cs`

## Testing

`BaseJewel.OnCraft`'s gem block is unit-testable directly (it keys off `CraftContext.PendingGem*`, not the flag): see `T2AJewelGemCraftTests.cs`. The packet-menu UX (double-click → window → target → craft) requires a running shard + T2A client and is covered by the manual checklist in the design spec (§12.1).

## Deviations from authentic T2A (summary)

Decided in the design spec §4; faithful to Jack's research except where shard authority overrode:
- **Make-last** — kept as QoL though it post-dates the T2A packet menus.
- **Half-on-failure** for non-scroll crafts — best-known reconstruction, not OSI-confirmed.
- **Hue-aware tailoring** — reconstruction, unverified by primary sources.
- **Stacked-gem jewelry** — the full targeted stack is consumed and named by count (shard-authoritative; overrides both the "single gem" reconstruction and Jack's deliberate "consume 1, name by stack").
- **Cooking** — out of scope (no T2A crafting menu existed for it).

## Related docs

| Topic | File |
|---|---|
| Design rationale, research, decisions | `docs/superpowers/specs/2026-06-07-t2a-crafting-menus-design.md` |
| Player guide & QoL (uncommitted) | `docs/t2a-crafting/` |
| Serialization | `dev-docs/serialization.md` |
| Networking & packets | `dev-docs/networking-packets.md` |
| Era & expansion handling | `dev-docs/era-expansion.md` |
| Gumps (the non-T2A path) | `dev-docs/gump-system.md` |
