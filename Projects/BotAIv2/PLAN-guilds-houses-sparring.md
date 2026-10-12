# Guilds, houses and sparring — a plan

Patrick's order of 08.09.2026: guilds to bind the bots together; houses bought with chests and workbenches in them;
sparring for Anatomy and Healing — **and no bot may turn grey**, because a grey bot is easy prey and a target for the
town guards.

The three orders are tied by one knot in the engine, and it has to be named first.

---

## The knot: a blow against an innocent is what turns a bot grey, and only a guild war gets round it

`Projects/UOContent/Misc/Notoriety.cs` decides who may be struck with impunity. Three roads were checked:

| road | verdict |
|---|---|
| `CheckHouseFlag` — "a fight in one's own house" | **does not fit.** It allows striking only somebody who is **not** a friend of the house (line 534: if `m` is a friend of the house — false). Two of ours sharing a house stay protected from each other. |
| one guild | **does not fit.** `sourceGuild == targetGuild` → `Notoriety.Ally`, and the blow is criminal. |
| **a guild war** | **works.** `sourceGuild.IsEnemy(targetGuild)` → `Notoriety.Enemy`, a lawful target, no flag, the guards stay out. |

**But a guild war holds across the whole island, not in a house.** Declaring war between two large guilds means a
massacre in the fields and in the market.

**The answer: sparring pairs.** For a training bout two bots leave their guilds for two tiny one-day guilds at war with
each other. Only the two of them fight; nothing changes for anybody else. When it ends, both guilds are disbanded and
the fighters go home.

---

## Stage 1. Guilds

**What it gives.** A tag over the head (visible to the eye), `Notoriety.Ally` between members, the ground for stage 3.

**How.** `new Guild(leader, name, abbreviation)` from `Projects/UOContent/Misc/Guild.cs`; `AddMember`, `Members`,
`Enemies`, `IsEnemy`.

**How many, and by what.** By trade, not at random — a guild has to mean something:

- **The Hammer** (`HAM`) — smith, miner, architect
- **The Needle** (`NDL`) — tailor, cook, alchemist, scribe
- **The Blade** (`BLD`) — warrior, archer, mage, healer, bravo
- **The Crown** (`CRN`) — captain, baron, watcher

**What changes in behaviour** (one seam each, every one counted):

1. `BotRescuer` — a guildmate's cry is worth more: help inside the guild comes first.
2. `BotMuster` — a company is raised from guildmates when there are enough; others are taken on when there are not.
3. `BotSpoils` — the spoils are shared more generously inside the guild.

**The risk.** A guild must not split the population: if a company could be raised only from guildmates, the defect
"none of ours nearby" would come back. So the guild is **a preference, not a condition** (a factor, not a veto; see
`factor-without-a-floor-is-a-veto`).

---

## Stage 2. Houses

**What it gives.** Storage of its own instead of the bank, room for workbenches, a floor for sparring.

**How.** `Projects/UOContent/Multis/HouseDeed.cs` → `BaseHouse`. A bot buys a deed from an architect (the `Architect`
NPC sells them), places the house on free ground and puts a chest inside.

**The order of work:**

1. `BotEstate` — the population's register of houses: who owns it, where it stands, what is inside.
2. `BotSteward` (a proposer) — offers "buy a house" when a bot has the money and no house.
3. `BotHomestead` (a deed) — buy the deed, walk to the place, place the house, bring in the chest.
4. Storage: `BotUnload` prefers its own chest to the bank when there is a house and it is nearer.

**Where to place it.** Not at random: `BaseHouse.FindHouseAt` and the engine's placement rules refuse occupied ground.
A search for a free patch near the population's home is needed — the same kind of problem as `BotStep.Settle` — and it
has to go through the existing `BotQuad.Trodden`, or the house will stand where nobody walks.

**The risk.** A house is a permanent object in the world and will outlive the experiments. Place one only with an
explicit owner's mark, and be able to raze every house of the population with one word to Argus (`do raze`), or the
island will be overgrown.

---

## Stage 3. Workbenches in the house

`ForgeAddon`, `AnvilAddon`, `Loom`, `SpinningWheel`, `Oven` are ordinary addons, sold as deeds. A workbench placed in a
house makes the house a workshop: `BotGround` already finds fires and anvils with a spatial query, so **no new code is
needed for "the house became a smithy"** — the sweep will find them by itself. This is the cheapest part of the order
and the most pleasing one: the economy will take it up without a change.

The one thing needed: the sweep has to reach the house. A house near the population is reached.

---

## Stage 4. Sparring

**Conditions to begin:** both bots healthy and free, standing in the house of one of them, both below their aim in
Anatomy or Healing, and neither of them in a company.

**The bout:**

1. Move both into paired one-day guilds and declare war (see the knot above).
2. Fight down to `SparSpare` (say, 40% health) — not to the death.
3. Part, declare peace, disband the guilds, send both back to their own.
4. **Heal each other with bandages** — this is where Anatomy and Healing grow, not in the blows themselves.

**Why the healing is the point.** Anatomy grows with use, Healing with bandaging somebody wounded. The fight here is
only a way to get a wound that can be healed. So the success of a bout is measured **not** by the number of blows but
by the skill gained — and that is what has to go into the counters.

**The risks, each with its own safeguard:**

| risk | safeguard |
|---|---|
| they kill each other | a health threshold to stop at + `BotSlay.FleeAt`, which already exists |
| the war is not disbanded, a massacre across the island | disband on a timer, not on an event; on a world restart, clear every sparring guild |
| sparring instead of work | sparring is priced low, like `stroll`; it wins only when there is nothing to do |
| the guards | a private house has no guards inside; but check that the house is **not** inside a town's zone |

---

## Order and cost

1. **Guilds** — small, with an immediate effect, needed for stage 4.
2. **Houses** — the largest piece, but it is also what gives storage and workshops.
3. **Workbenches** — nearly free; the economy will take them up by itself.
4. **Sparring** — needs 1 and 2, and is best done last, because it is the only one that touches guild wars.

**What to check after every stage:** the share of grey bots (`Criminal`) and of kills between our own — both must stay
at nought. That is the main sign the knot was solved correctly.
