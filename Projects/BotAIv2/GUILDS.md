# The guilds — manifesto, rules, and the model behind them

Written the night of 08→09.09.2026, on Patrick's order: *work the guild manifesto and rules out as far as
they will go, do the research, and build a behaviour model like a social circle.*

This document is the reasoning. The rules themselves live in `BotPopulation/BotGuilds.cs` and the numbers in
`DIALS.md`; where the two disagree, the code is right and this file is out of date.

> **Amended 21.09.2026 (builds 170–171), on Patrick's order: anybody may found a guild.** What changed, and
> everything below should be read with it in mind:
>
> - **Rule 2 is lifted.** A guild no longer needs a crafter to be founded, so the cap of five guilds is gone;
>   the cap is the pool of eight names. The summary still counts guilds with nobody to make anything.
> - **Guilds are founded while the shard runs** (`BotGuilds.Gather`, one bot without a guild every five
>   seconds): it joins the smallest guild under ten, or founds its own with the four nearest bots that have
>   none. Rule 1 is kept by taking the five at once — still a rule about a group applied to a group.
> - **The muster no longer deals the roster out.** Bots survive a restart with their guild, so the muster
>   adopts what the save returned. §4's "dealt by role" describes the old boot.
> - **A guild's fixed point is its head, not its maker**: the head neither walks out nor can be put out, and
>   it is the head that may lead a delve.
> - **Every guild carries a guildstone**, on the internal map. In this era the engine disbands any guild
>   without one the first time it is read back from a save; that is what emptied all five on 20.09.2026.
> - **Leaving means crossing over.** A member of a guild with nothing to show leaves only for a guild that has
>   something to show and room for it, where it used to walk out into nothing and be barred for two days.
>
> The full reasoning and the measurements are in `DECISIONS.md`, entries for builds 170 and 171.

> **Amended 06.10.2026, on Patrick's order: guilds nobody wants may close; fifty more names.**
>
> - **58 names.** The eight are taken first, in order (their halls are inherited by the name on the sign); past
>   them a founding draws one of the free fifty at random. The bound on guilds is now `BotGuilds.MostGuilds`
>   (10), not the pool.
> - **Attractiveness** (`BotGuilds.Appeal`) = `Worth` (hall 2, a square 1 each, a town house 1, a purse of half a
>   hall's price 1) + members / 5. In the `Guilds:` line as `appeal N.N` and in `/api/state` → `guilds[].appeal`.
> - **Closing** (`BotGuilds.Wane`): fewer than five members, attractiveness under 2.0 and nobody joining, for two
>   hours of uptime (kept across restarts) — the guild closes. Not while at war, laying claim, with a member in a
>   company, or while the other guilds lack the places to take its members. The chest is shared among the last
>   members, the members are placed by `Gather`, the engine disbands the guild (guildstone, enemies, allies), the
>   seat and widenings are dropped; squares and a town house are released by the code that already handles a guild
>   that is gone. The name rests a week (`NameRestHours`).
> - §4's "Decline is deliberately not modelled" no longer holds for a guild with nothing: a guild with a hall is
>   never closed by this rule (a hall alone is 2.0).

> **Amended 06.10.2026 (~10:00), on Patrick's word: "five leaving a full guild to found a new one under one of the
> fifty names: we do it."** (`BotGuilds.Hive`, every `ReviewMs`.)
>
> - **Who leaves.** A guild at its ceiling loses five members who each have a reason of their own: *no share* (under
>   the 800gp keep for an hour unbroken while the chest holds 2500gp or more), *far* (living in another town than the
>   guild's house or hall, or neither within 120 tiles of its town), or *a quarrel* (half the other guilds hold the guild
>   at −40 or under, or its town has put it out). Never the head, a member in a company, or one inside the walk-out bar.
> - **When.** Not while the guild is at war, not within 12 h of its last split, not with `MostGuilds` (10) standing, not
>   while another guild with room (beyond the places splits left) reads attractiveness 5.0 or more, not within 3 h of
>   any split.
> - **What they take.** Their own packs and banks. The chest stays; their keys to the old hall and outpost are taken
>   back. The new guild has one of the fifty names with nothing under it, no hall, no ground, no house, a bare seat by
>   the town most of them live in, and claims free squares by the rules every guild does.
> - **No ping-pong.** The walk-out bar is now saved (`BotGuildSplitStore`): the founders may not cross out or go back for
>   48 h of running time, across restarts. The new guild is spared the closing clock for 6 h.
> - Replayed on the night's logs: one split per 3.5-hour log (The Hammer at 02:10, no hall and no house; The Anchor at
>   09:12, no share). Without the gap and the cooldown, two and one.

---

## 1. What went wrong with the first version, and why it is worth stating first

Guilds were introduced on 08.09.2026 by sorting the population by trade: every fighter into the Blade, every
maker into the Needle, the captain and the Baron into the Crown. It worked in the sense that the tags
appeared over the right heads, and it failed in every sense that matters:

| | |
|---|---|
| The Blade | 35 of 49 bots |
| The Hammer | 11 |
| The Crown | 2 |
| The Needle | 1 |

**A guild of one is not a guild, and a guild of thirty-five is not a guild either — it is the population with
a smaller name.** Nothing that a guild is *for* can happen in that shape: a guild of one cannot levy for a
hall, a guild of thirty-five wins every quarrel before it begins, and neither of them is a group anybody
belongs to in a way they would notice.

The deeper fault was the sorting rule. **Trade is a property each bot has on its own**, so sorting by it can
be done one bot at a time — which is exactly why the code did it that way, enrolling each bot as it was born.
A group, though, is not the sum of properties its members have separately. "At least five, one of whom can
make things" cannot be evaluated for a bot standing alone. The old design could not have expressed Patrick's
rules even if it had wanted to, and that is the real lesson: *the shape of the enrolment loop decided the
shape of the society.*

---

## 2. The rules, as ordered

Patrick's rules of 09.09.2026, verbatim in substance:

1. **A guild may be founded only with at least five members.**
2. **At least one of them must be a crafter** — somebody who can make things.
3. **No guild may exceed fifteen members.**
4. **The guild's crafter lives to equip its guildmates as well as it can.**

And their consequences on this shard, which are arithmetic rather than opinion:

- Five bots on this island can make things (`Crafter` ×4, `Architect` ×1). Rule 2 therefore caps the island
  at **five guilds**, whatever the other rules say.
- 49 bots across 5 guilds is **~10 each** — comfortably inside 5–15, with room for a guild to lose two
  members to a bad night and still be a guild.
- The first muster under the new rules produced exactly that: five guilds of ten, every one with a maker,
  nobody left out.

---

## 3. The model: a band, not a category

The behaviour model Patrick asked for is best stated as what a social circle *is not*. It is not a category
("all the archers"), not a hierarchy, and not an alliance of convenience. It is a small group whose members
keep meeting, need each other unequally, and hold something in common.

Four properties, and each one is a rule in the code or a plan already written:

### 3.1 Small enough that everybody is somebody you keep meeting

The ceiling of fifteen is not arbitrary and it is not only about fairness in a fight. A group's cost of
holding together rises much faster than its size: everybody has to keep track of everybody, and the number of
pairs in a group of *n* is *n(n−1)/2*. At five that is ten relationships; at fifteen, a hundred and five; at
thirty-five, five hundred and ninety-five. Human groups that actually function by mutual obligation rather
than by rules and hierarchy — hunting bands, work crews, sections in an army — cluster in the same range,
and the anthropological literature that sets the innermost layers of a personal network at roughly five and
fifteen (Dunbar's layers) is describing the same ceiling from the other side.

**The claim here is deliberately weak and worth stating plainly:** bots have no cognition to be limited by.
The reason to borrow the number is not that the bots will feel it, it is that *a group of ten produces the
behaviour a watcher recognises as a band* — the same faces, repeatedly, with a stake in each other — and a
group of thirty-five produces a crowd. This project is watched by a person from a client; that is the
measure that matters.

### 3.2 Mixed, so it can look after itself

The roster is dealt out **by role**, not in roster order — roster order is birth order, which is class order,
which would put every archer in one guild and rebuild the caste system inside the new rules. Every band
therefore comes out with melee, ranged, a caster, a medic and a maker.

This is what makes a guild a unit that can *do* something: muster a company, hold a hall, survive a fight,
equip itself. A band of ten specialists of one kind is a department. A band of ten mixed trades is a village.

### 3.3 Something held in common

A circle needs a thing that is *theirs*. On this shard that is the hall (`BotEstate`): bought out of a levy
on the members, standing in the world, outliving every bot that paid for it, and fitted out one bench at a
time with the guild's own money. Everything about the hall is deliberately collective — the levy takes from
whoever can spare it, the co-owners are every member, and the benches inside it may be used by the guild and
nobody else.

**The private-tools rule is the sharpest expression of the model.** A forge in the street belongs to
everybody; a forge in a hall belongs to a band. It is the first thing on this shard that one bot has and
another cannot use.

### 3.4 Obligation, unequal and specific

Membership has to cost something or it is a tag again. Three obligations exist now:

| obligation | where | what it does |
|---|---|---|
| Answer your own | `BotGuilds.Worth` → `BotRescue` | a guildmate's cry is worth ×1.35 of a stranger's |
| Pay for the roof | `BotEstate.Levy` | the hall and its benches come out of members' pockets, richest first, each keeping 300 |
| Keep your own equipped | `BotGuilds.Stand` → `BotArmourer` | when a member cannot afford the armour it needs, the guild stands the difference |

The third is rule 4 above, carried out through the market rather than by hand — and it is worth reading how,
because it is the cheapest thing in this whole design. The machinery for equipping a bot already worked end
to end: the armourer asks what piece a bot most needs, raises an order, a crafter fills it, `Rearm` puts it
on. The only thing that ever stopped it was **money** — half this population has never held more than the
four hundred gold it was born with. So the guild pays the shortfall; the member orders its own armour off the
same board as everybody else; and the crafter most likely to fill that order is the guild's own, because a
guildmate's order is worth more to it. *The guild's money ends in the guild's maker's purse, having become a
breastplate on the way.* Not a line of new economy was needed.

---

## 4. Lifecycle

**Formation** happens once, after the population is raised, with the whole roster in view
(`BotGuilds.Muster`). It cannot be done one bot at a time: see §1.

**Names come from a fixed pool** — The Hammer, The Blade, The Crown, The Needle, The Lantern, The Anchor, The
Wolf, The Ash — and that is not decoration either. A hall is found again after a restart by the name written
on its sign, so a population that invented fresh names each start would orphan every building it had ever
raised. The four old names head the list so the halls already standing are inherited rather than abandoned.

**Growth**: a bot born or resurrected after the muster joins the smallest guild with room (`Enrol`). Above the
ceiling it stays unguilded, and that number is reported rather than hidden — a population with a fifth of it
outside every guild is a fact about the ceiling, not about the bots.

**Decline is deliberately not modelled.** A guild that falls below five is *not* disbanded. Disbanding it
would take its hall off the island because of one bad afternoon, and a rule that destroys permanent property
in response to a temporary state is a rule that will eventually destroy all of it. The floor of five is a
condition of *founding* and nothing else.

---

## 5. What is not built yet, and where it is written down

`PLAN-guild-lands.md` carries the rest of Patrick's order of 08.09.2026, with the engine facts already
checked:

- **Lands** — the ground round a hall becomes the guild's; members prefer to work it and move non-members
  along.
- **Standing** — a number per pair of guilds, worsened by working their land, by borders touching, by blood;
  mended by trade.
- **War** — below a threshold, `Guild.AddEnemy`, at which point the engine itself makes members lawful
  targets: no criminal flag, no guard reaction, and looting the dead is not a crime.
- **Looting needs no new code.** Bots already loot corpses (`BotHunt/BotPickings`), and the rule is already
  "only what I killed myself".

The one piece of that already built is the **fifth**, because everything above makes death expensive:
`BotUnload.Risk` — a bot walks to a counter when what it carries is worth more than it should be carrying
about, quite apart from whether the pack is heavy.

---

## 6. How to tell whether the model is working

Read these in the `Guilds:` and `Estate:` lines, in this order. Each has a failure it is there to catch.

| number | healthy | what it means when it is not |
|---|---|---|
| guild sizes | 8–12 each | one guild dominating is the old defect returning |
| guilds with no maker | 0 | rule 2 broke; nobody in that band can be equipped by its own |
| bots left unguilded | 0 | the ceiling is biting: either more names or a bigger `Most` |
| `Ngp stood for members` | rising | rule 4 is working: guild money is becoming armour |
| `N times a guild could not` | low | the guilds are too poor to keep their own equipped |
| halls standing | rising slowly | a guild of ten reaching 5,000gp is the pace of the whole design |
| `N times a workshop was passed over as somebody else's` | small but non-zero | zero means the private-tools rule has never once bitten, which means nobody has built a workshop yet |

The one number that would say the whole thing has gone wrong is the completion band in `Will:` falling while
these rise. A society that costs its members more than it gives them is a society that stops working, and on
this shard that shows up as bots finishing less.
