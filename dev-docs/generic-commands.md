# Generic Commands (finding and manipulating entities)

How staff find a set of objects and run a command against all of them: scopes, `where` conditions,
`order by` / `distinct` / `limit`, dot notation, value syntax, `[interface` and `[batch`.

This covers the **generic command system** only. For the full per-command list (`[add`, `[props`,
`[tele`, …) see the in-game commands page shipped with the distro, published at
<https://muo.gg/commands>.

## The shape of a generic command

```
[<scope> <command> [command args] [where <Type> <conditions>] [distinct <props>] [order by <props>] [limit <n>]
```

- **scope** — which objects to consider (`Global`, `Area`, `Region`, …).
- **command** — what to do to each (`Delete`, `Props`, `Set`, `Count`, `Interface`, …).
- **modifiers** — optional filters applied to the found set before the command runs.

```
[global count where Item Movable = false
[area delete where Item ItemID = 0x1F13
[region interface where Mobile Hits < 10 order by Hits limit 20
```

Commands opt into which scopes they support and whether they act on Items, Mobiles or both, so not
every command works under every scope. A command that does not support the scope reports
*"That is either an invalid command name or one that does not support this modifier."*

## Scopes

| Scope | Usage | Conditions? | Selects |
|---|---|---|---|
| `Global` | `[global <command> [condition]` | yes | every object in the world |
| `Area` (`Group`) | `[area <command> [condition]` | yes | a bounding box you drag |
| `Screen` | `[screen <command> [condition]` | yes | everything on your screen |
| `Range` | `[range <range> <command> [condition]` | yes | within `<range>` tiles of you |
| `Region` | `[region <command> [condition]` | yes | your current region |
| `Facet` | `[facet <command> [condition]` | yes | your whole map |
| `Contained` | `[contained <command> [condition]` | yes¹ | inside a targeted container |
| `Online` | `[online <command> [condition]` | yes | connected players |
| `IPAddress` | `[ipaddress <command> [condition]` | yes | accounts sharing a targeted player's IP |
| `Multi` (`m`) | `[m <command>` | no | several objects you target in turn |
| `Single` | `[single <command>` | no | one targeted object |
| `Self` | `[self <command>` | no | you |
| `Serial` | `[serial <serial> <command>` | no | one object by serial |

`Multi`, `Single`, `Self` and `Serial` do not parse modifiers at all — a `where` clause there is
not rejected, it is passed through to the command as ordinary arguments.

¹ `Contained` honours conditions on the normal command path, but it never sets the
`SupportsConditionals` flag, and `[batch` is the one place that checks it. So a condition works
under `[contained` typed directly and is refused under `[batch` with that scope.

## `where`

The **first token after `where` is a type name**, and it is required. It filters the set to objects
of that type (subclasses included) and fixes the type whose properties the rest of the clause reads.

```
[global count where Item                  -- every Item
[global count where BaseCreature Hits < 10
```

Only properties marked `[CommandProperty]` are visible, and your access level must meet the
attribute's read level.

### Operators

| Operator | Meaning |
|---|---|
| `=`, `==`, `is` | equal |
| `!=` | not equal |
| `>`, `<`, `>=`, `<=` | relational (needs a comparable type) |
| `=~`, `~=`, `==~`, `~==`, `is~`, `~is` | equal, case-insensitive |
| `!=~`, `~!=` | not equal, case-insensitive |
| `starts`, `ends`, `contains` | substring tests |
| `starts~`, `ends~`, `contains~` | substring tests, case-insensitive |

The `~` may lead or trail — `~contains` and `contains~` are the same operator.

Relational operators on a type with no ordering (no `IComparable`) are rejected rather than
silently misbehaving. Equality on such a type compares **by value**, not by reference.

### Combining conditions

Conditions separated by whitespace are ANDed. `or` (or `||`) starts a new alternative group, and
`not` (or `!`) negates the single condition that follows it.

```
[global count where Item Movable = true Hue = 0
[global count where Item Hue = 0 or Hue = 1
[global count where Item not Movable = true
```

## Dot notation

A binding may walk a chain of properties:

```
[global count where SkillTeleporter Message.Number = 1060847
[global interface where BaseCreature ControlMaster.Name =~ bob
```

If a link partway along the chain is null, the object simply does not match — it is not an error,
and it does not stop the sweep. The same applies under `not`: an unreadable binding never matches.

For `order by` and `distinct`, which have no "no match" to give, a null link reads as the property
type's default (`0`, `null`, …).

Chains are **read-only**. `[set Message.Number 5` fails when the intermediate's members are
get-only, as `TextDefinition`'s are.

## Values

A comparison constant is resolved by the same parser behind `[set`, `[add`, spawner props and the
props gump, so a value that works in one place works in all of them.

| Form | Means |
|---|---|
| `123`, `-4` | a number |
| `0x1F13` | a number, hex |
| `true` / `false` | a boolean |
| `Magery` | an enum member, case-insensitive |
| `Felucca` | a `Map` |
| `Static`, `BaseCreature` | a `Type`, by name |
| `0x40001234` | an entity, resolved by serial |
| `hello world` | a string — quote it in the command line if it contains spaces |
| `null` | null (in `where` only — see below) |
| `(-null-)` | null (in `[set` / `[add` / spawner props) |
| `@"text"` | the literal text inside, for values that would otherwise be read as something else |
| `#1234` | a `TextDefinition` cliloc, explicitly |

### Quoting, and why `@"..."` exists

The command tokenizer strips real quotes before any parser sees them, so `"0"` and `0` arrive
identical. `@"..."` is the in-band escape that survives:

```
[set Name @"null"          -- the four-letter string, not a null
[set Message @"1060847"    -- the string "1060847", not cliloc 1060847
[set Message #1060847      -- cliloc 1060847, explicitly
[set Message 1060847       -- cliloc 1060847 (a bare integer is always a cliloc)
```

`[get` writes the same form back for any value that would otherwise be misread, so its output can
be pasted straight into `[set`.

### The one inconsistency: `null`

`where` spells a null constant as a bare `null`. `[set` and friends use `(-null-)`, and read a bare
`null` as the four-letter string. This predates the shared parser and is preserved deliberately —
every existing `where … = null` clause depends on it.

```
[global count where Item Name = null        -- Name is null
[set Name (-null-)                          -- set Name to null
[set Name null                              -- set Name to the string "null"
```

## `distinct`, `order by`, `limit`

```
[global interface where Item order by Hue desc limit 50
[global interface where Mobile distinct Name order by Name
```

- `distinct <prop> [<prop> …]` — keeps one object per distinct combination of those properties. It
  sorts internally to do so, so it also reorders the set; add `order by` if the order matters.
- `order by <prop> [direction] [<prop> …]` — `by` is optional. Direction is `+`/`up`/`asc`/`ascending`
  or `-`/`down`/`desc`/`descending`, defaulting to ascending. Multiple keys break ties left to right.
- `limit <n>` — keeps the first `n` after the others have run.

Keywords are case-insensitive, and **the order you type them does not matter**: they always apply
as `where` → `distinct` → `order by` → `limit`.

## `[interface`

```
[<scope> interface [view <properties …>] [condition]
```

Opens a gump listing every match instead of acting on them. Each row can be inspected, and `view`
adds columns for the properties you name. This is the safest way to see what a condition selects
before running something destructive with the same clause.

```
[global interface where Item Movable = false ItemID = 0x1F13
[global interface view Hue Name where Mobile Hits < 10
```

## `[batch`

`[batch` opens a gump that runs **several commands against one found set**. Type `[batch` with no
arguments; the gump has three parts:

- **Scope** — pick one of the scopes above.
- **Condition** — the whole clause, and it must start with `where`.
- **Commands** — one or more entries, each with a command and an optional **Object**.

Every command runs against the same matched set, in the order listed. It is the tool for "find
these once, then do three things to them" without re-running an expensive sweep, and for
multi-step edits that would otherwise race against their own filter — a `where Hue = 0` clause
re-evaluated after the first command has changed `Hue` would no longer match.

The **Object** field is a property chain that redirects that one command onto a sub-object of each
match. Leave it blank to act on the match itself; set it to `Backpack` to act on each mobile's
backpack instead. Objects whose chain is null or unreadable are skipped for that command.

Logging is suppressed automatically when the set is larger than 20 objects.

## Gotchas

- The type after `where` is mandatory; `where Movable = true` is a parse error, not a wildcard.
- Only `[CommandProperty]` members are reachable, and write access is checked separately from read.
- Relational operators need a comparable type; equality is available on everything.
- A bare integer targeting a `TextDefinition` is always a cliloc. Use `@"123"` for the string.
- `where … = null` and `[set … (-null-)` are different spellings of the same idea. See above.
- Spawner **Params** are split on plain spaces, not the quoting tokenizer, so a constructor
  argument cannot contain a space. Spawner **Props** use the normal tokenizer and value syntax.
- Test a destructive clause with `[interface` or `count` first.

## Key files

| Concern | File |
|---|---|
| Scopes | `Projects/UOContent/Commands/Generic/Implementors/` |
| `where` parsing, operators | `Projects/UOContent/Commands/Generic/Implementors/ObjectConditional.cs` |
| Condition/sort/distinct compilation | `Projects/UOContent/Commands/Generic/Extensions/Compilers/` |
| Modifier parsing and apply order | `Projects/UOContent/Commands/Generic/Extensions/BaseExtension.cs` |
| Value parsing | `Projects/UOContent/Utilities/Types.cs` |
| Command tokenizer | `Projects/Server/Commands.cs` (`Commands.Split`) |
| `[batch` | `Projects/UOContent/Commands/Batch.cs` |
| `[interface` | `Projects/UOContent/Commands/Generic/Commands/Interface.cs` |

## See also

- `dev-docs/commands-targeting.md` — registering commands and the targeting system.
- <https://muo.gg/commands> — the full command list, regenerated with distro updates.
