# Message Interpolation Audit (Phase 2)

Generated: 2026-05-03
Scope: call sites of `SendMessage`, `SendLocalizedMessage`, `SendAsciiMessage`, `Public/Local/Nonlocal/PrivateOverheadMessage`, `Say`, `Emote`, `Whisper`, `Yell`, `SendLocalizedMessageTo` in `Projects/UOContent` and `Projects/Server`.

This is the input for Phase 3 cleanup PRs (sliced by anti-pattern category). Each PR deletes the rows it fixes.

Phase 1 (PR #2434) added zero-allocation `[InterpolatedStringHandler]` overloads to every player-facing message API. Most `Send…($"…")` call sites are therefore already zero-allocation and are NOT listed here. This audit lists only the call sites where the source pattern still allocates an intermediate `string` despite the new overloads.

**Out of scope (intentionally not flagged):**

- Plain `Send…("literal text")` — string literal is interned, ROS overload zero-alloc.
- `Send…($"literal {intVar}")` with no other anti-pattern — the handler overload handles it cleanly.
- `Send…(cliloc, $"{arg1}\t{arg2}")` — standard tab-delimited cliloc-arg pattern. The `$"…"` here is the cliloc replacement-args parameter, not the message text; that parameter still requires a `string` and is not a Phase 1 target.
- `SendLocalizedMessage(int number)` (no args) — no string involved.

## Summary

| Category | Count |
|---|---|
| 1. Ternary → if/else | 1 |
| 2. Switch expression → switch statement | 1 |
| 3. Inline pre-built local | 3 |
| 4. `.String()` / `.ToString()` extraction | 13 |
| 4b. Concat in hole | 0 |
| 5. `string.Format` → `$""` | 9 |
| 6. LINQ in hole | 0 |
| 7. Pre-built concat var | 1 |
| **Total flagged** | **28** |

## Category 1 — Ternary → if/else

Ternary expressions whose branches are interpolated literals unify to `string`, forcing the ROS overload (or, when nested inside a hole, allocating the inner branch as a `string`).

| File:Line | Snippet | Suggested fix |
|---|---|---|
| `Projects/Server/Items/Item.cs:4213` | `ns.SendMessage(... $"{Name}{(m_Amount > 1 ? $" : {m_Amount}" : "")}")` (nested `$"…"` in ternary inside the outer hole) | Hoist the ternary to an `if/else` that calls `ns.SendMessage(...)` twice with two distinct top-level `$"…"` literals (one with the amount suffix, one without), or build the suffix via a `ValueStringBuilder` before the call. |

## Category 2 — Switch expression → switch statement

Switch expressions whose arms are interpolated literals unify to `string`, forcing the ROS overload.

| File:Line | Snippet | Suggested fix |
|---|---|---|
| `Projects/UOContent/Engines/Factions/Mobiles/Guards/BaseFactionGuard.cs:140-150` | `var warning = Utility.Random(6) switch { 0 => $"I warn you, {m.Name}…", 1 => $"It would be wise…", … }; Say(warning);` | Convert to a `switch (Utility.Random(6))` statement and call `Say($"…")` directly inside each `case`. |

## Category 3 — Inline pre-built local

Locals typed as `string` and used by exactly one `Send…` call: the local forces the ROS overload because the handler overload is only selected when the argument is a literal `$"…"` interpolated string.

| File:Line | Snippet | Suggested fix |
|---|---|---|
| `Projects/UOContent/Engines/ConPVP/DuelContext.cs:1337-1346` | `var text = $"{{0}} are ranked {LadderGump.Rank(entry.Index + 1)} at level {Ladder.GetLevel(entry.Experience)}."; pm.PrivateOverheadMessage(..., string.Format(text, from == pm ? "You" : "They"), ...)` | Inline the `string.Format` call: `pm.PrivateOverheadMessage(..., $"{(from == pm ? "You" : "They")} are ranked {LadderGump.Rank(...)} at level {Ladder.GetLevel(...)}.", ...)`. Removes both the prebuilt format-string local and the `string.Format` call. (Also see Category 5.) |
| `Projects/UOContent/Engines/ConPVP/DuelContext.cs:1463-1472` | Same shape as above, feeding both `pm.LocalOverheadMessage(...)` and `pm.NonlocalOverheadMessage(...)` via two `string.Format(text, …)` calls. | Convert to two if/else branches that each call the message API with a directly-interpolated `$"…"`. (Also see Category 5.) |
| `Projects/Server/Mobiles/Mobile.cs:7911-7918` | `var text = string.Format(title.Length <= 0 ? "[{1}]{2}" : "[{0}, {1}]{2}", title, guild.Abbreviation, type); PrivateOverheadMessage(MessageType.Regular, SpeechHue, true, text, from.NetState);` | Split into `if (title.Length <= 0) { PrivateOverheadMessage(..., $"[{guild.Abbreviation}]{type}", ...); } else { PrivateOverheadMessage(..., $"[{title}, {guild.Abbreviation}]{type}", ...); }`. Removes the prebuilt local, the `string.Format`, and the format-string ternary in one shot. (Also see Category 5.) |

## Category 4 — `.String()` / `.ToString()` extraction

Method calls inside an interpolation hole that return a freshly allocated `string`. The handler would otherwise format the underlying value directly with no allocation; the explicit `.ToString()` defeats that.

| File:Line | Snippet | Suggested fix |
|---|---|---|
| `Projects/UOContent/Commands/StaffAccess.cs:88` | `m.SendMessage($"You cannot set your staff access to {newAccessLevel.ToString()}.");` | Drop the `.ToString()`: `m.SendMessage($"You cannot set your staff access to {newAccessLevel}.");`. The handler appends the enum directly. |
| `Projects/UOContent/Commands/StaffAccess.cs:99` | `m.SendMessage($"Staff access set to {newAccessLevel.ToString()}.");` | Drop the `.ToString()`. |
| `Projects/UOContent/Commands/Handlers.cs:102` | `from.SendMessage($"Your region is {builder.ToString()}.");` (where `builder` is a `ValueStringBuilder`) | Use `builder.AsSpan()` or rewrite the loop to interpolate directly into a `ValueStringBuilder.Create()` and pass the resulting span/string with the handler. The current code allocates a `string` from the builder just to pass to a method that would re-buffer it. |
| `Projects/UOContent/Engines/ConPVP/Tournament.cs:649` | `mob.SendMessage($"You have been awarded a {rank.ToString().ToLower()} trophy and {cash:N0}gp …");` | Cache the lowercased rank into a `ReadOnlySpan<char>` via `string.Create` / `ValueStringBuilder`, or add an `AppendTo(ref RawInterpolatedStringHandler)` extension that lower-cases an enum name into the buffer. Minimum: extract `var rankName = rank.ToString();` once and use `{rankName}` plus `.ToLower()` only at the consumer if absolutely required. (`.ToString()` plus `.ToLower()` allocates twice.) |
| `Projects/UOContent/Engines/ConPVP/Tournament.cs:655` | Same as above, no-cash branch. | Same fix. |
| `Projects/UOContent/Engines/ConPVP/Games/KingOfTheHill.cs:1116` | `mob.SendMessage($"You have been awarded a {rank.ToString().ToLower()} trophy and {cash:N0}gp …");` | Same fix as Tournament. |
| `Projects/UOContent/Engines/ConPVP/Games/KingOfTheHill.cs:1122` | Same shape, no-cash branch. | Same fix. |
| `Projects/UOContent/Engines/ConPVP/Games/DoubleDom.cs:783` | `mob.SendMessage($"You have been awarded a {rank.ToString().ToLower()} trophy and {cash:N0}gp …");` | Same fix. |
| `Projects/UOContent/Engines/ConPVP/Games/DoubleDom.cs:789` | Same shape, no-cash branch. | Same fix. |
| `Projects/UOContent/Engines/ConPVP/Games/CTF.cs:1197` | `mob.SendMessage($"You have been awarded a {rank.ToString().ToLower()} trophy and {cash:N0}gp …");` | Same fix. |
| `Projects/UOContent/Engines/ConPVP/Games/CTF.cs:1203` | Same shape, no-cash branch. | Same fix. |
| `Projects/UOContent/Engines/ConPVP/Games/BombingRun.cs:1816` | `mob.SendMessage($"You have been awarded a {rank.ToString().ToLower()} trophy and {cash:N0}gp …");` | Same fix. The inline comment in the source already notes "There is no formatting flag for Lowercase, we may need a custom interface to get rid of it" — Phase 3 is the time. |
| `Projects/UOContent/Engines/ConPVP/Games/BombingRun.cs:1822` | Same shape, no-cash branch. | Same fix. |

> **Phase 3 helper opportunity:** The `rank.ToString().ToLower()` pattern repeats verbatim in Tournament, KingOfTheHill, DoubleDom, CTF, and BombingRun (12 sites). Add an `AppendLowerEnum<T>(this ref RawInterpolatedStringHandler h, T value) where T : Enum` extension or a tiny `ToLowerSpan` helper before slicing the PR.

## Category 4b — Concat in hole

`+` or `string.Concat` that allocates an intermediate string inside an interpolation hole that is then passed to a message API.

*No flagged sites.* The `+` patterns inside holes that the grep surfaced (e.g. `{i + 1}`, `{Major + 60:00}`, `{salvaged + notSalvaged}`) are all integer arithmetic, not string concatenation; per the spec, integer `+` inside a hole is not a flag.

## Category 5 — `string.Format` → `$""`

Pre-existing `string.Format(...)` calls feeding a message API. Each one allocates a fresh `string` regardless of the new handler overloads, since the handler overload only triggers when the argument is a literal `$"…"`.

| File:Line | Snippet | Suggested fix |
|---|---|---|
| `Projects/Server/Mobiles/Mobile.cs:7911-7918` | `var text = string.Format(title.Length <= 0 ? "[{1}]{2}" : "[{0}, {1}]{2}", title, guild.Abbreviation, type); PrivateOverheadMessage(..., text, ...);` | See Category 3 fix — split into two if/else `$"…"` calls. |
| `Projects/UOContent/Mobiles/Monsters/LBR/Jukas/JukaLord.cs:85` | `Say(true, string.Format(toSay.RandomElement(), from.Name));` | Replace the `toSay` array entries that use `{0}` with template strings keyed by index, switch over the random index, then `Say(true, $"…{from.Name}…")` per case; or change the array to a `Func<string,string>[]` of interpolating lambdas if you must keep the table-driven shape. |
| `Projects/UOContent/Misc/AttackMessage.cs:30-35` | `aggressor.LocalOverheadMessage(..., string.Format(AggressorFormat, aggressed.Name));` (with `AggressorFormat = "You are attacking {0}!"`) | Inline the constant: `aggressor.LocalOverheadMessage(..., $"You are attacking {aggressed.Name}!");`. Remove the `AggressorFormat` constant. |
| `Projects/UOContent/Misc/AttackMessage.cs:36-41` | `aggressed.LocalOverheadMessage(..., string.Format(AggressedFormat, aggressor.Name));` | Inline the constant: `aggressed.LocalOverheadMessage(..., $"{aggressor.Name} is attacking you!");`. Remove the `AggressedFormat` constant. |
| `Projects/UOContent/Engines/ConPVP/Participant.cs:138-149` | `Players[i].Mobile.NonlocalOverheadMessage(..., string.Format(nonLocalOverhead, Players[i].Mobile.Name, Players[i].Mobile.Female ? "her" : "his"));` | The format string `nonLocalOverhead` is a parameter to the enclosing method — the call site that passes `nonLocalOverhead` should pass an interpolating delegate instead, or this method should accept a pre-formatted message and let the caller interpolate. (Larger refactor; mark as such in Phase 3 plan.) |
| `Projects/UOContent/Engines/ConPVP/Gumps/ConfirmSignupGump.cs:518-524` | `_registrar.PrivateOverheadMessage(..., string.Format(fmt, from.Female ? "Lady" : "Lord", timeUntil), from.NetState);` | The `fmt` value comes from a higher-up `switch`-style assignment. Convert each `fmt = "…"` assignment into a direct `$"…"` interpolation and pass it straight in (likely combined with promoting the `from.Female ? ... : ...` to an `if/else` to avoid Cat 1). |
| `Projects/UOContent/Engines/ConPVP/DuelContext.cs:1344` | `pm.PrivateOverheadMessage(..., string.Format(text, from == pm ? "You" : "They"), ...);` | See Category 3 — inline the format string + arg into one `$"…"` per ternary branch. |
| `Projects/UOContent/Engines/ConPVP/DuelContext.cs:1466` | `pm.LocalOverheadMessage(..., string.Format(text, "You", "are"));` | See Category 3 — inline. |
| `Projects/UOContent/Engines/ConPVP/DuelContext.cs:1471` | `pm.NonlocalOverheadMessage(..., string.Format(text, pm.Name, "is"));` | See Category 3 — inline. |

## Category 6 — LINQ in hole

LINQ chains that build strings inside an interpolation hole passed to a message API.

*No flagged sites.* The codebase uses `ValueStringBuilder` / explicit loops everywhere LINQ-string-building would otherwise be tempting; nothing in the message-API call sites uses `Select` / `Aggregate` / `string.Join` over strings.

## Category 7 — Pre-built concat var

String concat (`+`) of multiple interpolated strings, fed to a message API.

| File:Line | Snippet | Suggested fix |
|---|---|---|
| `Projects/UOContent/World Saves/SaveCommands.cs:71-75` | `mobile.SendMessage($"  - {dest.Name} (retention: {dest.GetRetentionCount(ArchivePeriod.Hourly)}h/" + $"{dest.GetRetentionCount(ArchivePeriod.Daily)}d/" + $"{dest.GetRetentionCount(ArchivePeriod.Monthly)}m)");` | Merge the three concatenated `$"…"` segments into one literal `$"…"` so the handler overload absorbs the whole interpolation in a single buffered pass: `mobile.SendMessage($"  - {dest.Name} (retention: {dest.GetRetentionCount(ArchivePeriod.Hourly)}h/{dest.GetRetentionCount(ArchivePeriod.Daily)}d/{dest.GetRetentionCount(ArchivePeriod.Monthly)}m)");`. |

## Methodology notes

- Detection used `Grep` (ripgrep) sweeps with the regex families from the spec, scoped to `Projects/UOContent` and `Projects/Server`, then manually triaged each match against the in-scope method list.
- The bulk of `Send…($"…")` call sites in the codebase (~270 hits) are **already** in the form the new Phase 1 handler overloads accept and produce **zero** allocations — those are intentionally absent from this audit.
- Many `var args = $"{a}\t{b}"; SendLocalizedMessage(cliloc, args)` patterns (Justice, Faction, Spells/Bless, Spells/Curse, AI/TransferItem, AI/PetOrders, etc.) were considered for Category 3 but excluded: per the spec the `$"…"` second argument of `SendLocalizedMessage(int, string)` is the cliloc replacement-args slot, which is out of scope for Phase 1/2 (no handler overload exists for it).
- Loop-hoisted prebuilt locals (`Skills/Stealing.cs:394`, `Skills/Snooping.cs:69`, `Items/Misc/CommunicationCrystals.cs:337`) are intentionally retained: they are reused across multiple iterations / branches and pre-building avoids re-interpolating per send.
- Ternaries and switch expressions whose branches return only `string` literals (e.g. `Items/Talismans/BaseTalisman.cs:492`'s `_summoner.Name.Number > 0 ? $"#{_summoner.Name}" : _summoner.Name.String` and the various `?? $"#{LabelNumber}"`) are NOT flagged when the result is only ever the cliloc-args parameter — that argument slot is out of scope.
- `Items/Body Parts/Head.cs`, `Engines/Craft/Core/CraftItem.cs`, `Engines/Craft/Core/CraftGumpItem.cs`, `Items/Skill Items/Fishing/Misc/SOS.cs`, etc. surfaced switch expressions that build strings, but their results are consumed by gump building / property lists / `Item.Name` returns — not by an in-scope message API — so they are not flagged here.
- `Server/Mobiles/Mobile.cs:1044`'s `SendMessage($"Your access level has been changed. You are now {GetAccessLevelName(value)}.")` calls a method inside the hole, but `GetAccessLevelName` returns a static-`string` table lookup (no per-call allocation); not flagged.
