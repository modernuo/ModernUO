---
name: migrate-gumps
description: >
  Trigger: when converting RunUO Gump classes, OnResponse handlers, or gump UI code to ModernUO DynamicGump/StaticGump.
  Covers: builder pattern, DisplayTo, response handling, empty gump rule.
---

# RunUO -> ModernUO Gump Migration

## When This Activates
- Converting `Gump` subclasses
- Converting `OnResponse(NetState, RelayInfo)` handlers
- Updating gump sending/closing patterns

## Conversion Steps
1. Choose type: `DynamicGump` (variable layout) or `StaticGump<T>` (fixed layout)
2. Change class declaration: `class X : Gump` -> `class X : DynamicGump`
3. Add `public override bool Singleton => true;` if only one per player
4. Make constructor private, add static `DisplayTo()` method
5. Move all `AddXxx()` calls from constructor to `BuildLayout(ref DynamicGumpBuilder builder)`
6. Prefix each call with `builder.`: `AddLabel(...)` -> `builder.AddLabel(...)`
7. Convert properties: `Closable = false` -> `builder.SetNoClose()`
8. Update OnResponse: `OnResponse(NetState, RelayInfo)` -> `OnResponse(NetState, in RelayInfo)`
9. Update text entries: `info.TextEntries[i].Text` -> `info.GetTextEntry(id)`
10. For StaticGump: extract variable text into placeholders + `BuildStrings`

## Quick Mapping
| RunUO | ModernUO |
|---|---|
| `class X : Gump` | `class X : DynamicGump` or `StaticGump<X>` |
| `AddPage(0)` | `builder.AddPage()` |
| `Closable = false` | `builder.SetNoClose()` |
| `Dragable = false` | `builder.SetNoMove()` |
| `OnResponse(NetState, RelayInfo)` | `OnResponse(NetState, in RelayInfo)` |
| `info.TextEntries[i].Text` | `info.GetTextEntry(id)` |
| `from.SendGump(new X(...))` | `X.DisplayTo(from, ...)` |
| `from.CloseGump(typeof(X))` | `from.CloseGump<X>()` |

## Critical: Empty Gump Rule
Never create a gump with no visual elements. Use the `DisplayTo()` pattern -- validate before constructing.

## See Also
- `dev-docs/runuo-migration-docs/04-gumps.md` -- detailed migration reference
- `dev-docs/gump-system.md` -- complete ModernUO gump system
- `dev-docs/claude-skills/modernuo-gump-system.md` -- ModernUO gump skill
