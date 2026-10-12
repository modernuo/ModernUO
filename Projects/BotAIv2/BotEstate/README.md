# `BotEstate/` — the guilds' halls

Ordered by Patrick on 08.09.2026 as the second stage of `PLAN-guilds-houses-sparring.md`: houses, with
chests and workbenches, and eventually a private place to spar in. This folder is that stage.

## Why a guild hall and not a house per bot

The plan said "a bot buys a house". Three numbers said otherwise, measured on the shard the same evening:

| | |
|---|---|
| cheapest classic house, `HousePlacementEntry.ClassicHouses` | 35,250gp |
| fattest purse in the population | 843gp |
| middle purse | 355gp |
| whole population, pockets and accounts together | under 25,000gp |
| money that survives a restart | none — `BotProgress.Savings` is off by Patrick's order of the same day |

A house per bot at the engine's price is two numbers on one shelf that never meet: the offer would be made,
refused for ever, and the summary would report a zero that means nothing. So the **guild** buys it, out of a
levy on its members, and the price is a dial with the engine's own figure written down beside it.

That choice pays for itself twice. Four halls rather than forty-nine huts is a small, reversible change to a
world that keeps everything; and it gives the guild tag from stage 1 something to *be* — the thing that owns
a roof, a chest and a forge.

## The five engine facts this rests on

Each was checked in the engine's own source before a line was written here, and each would have broken the
design silently if it had gone the other way.

1. **Bots can walk indoors.** `BotStep.Mask` falls through to `MultiMaskCache` for any tile a multi covers —
   "that is what lets bots walk indoors at all". Without it a hall is a box the population cannot enter.
2. **A house door in this era is opened with a key, not with permission.** `BaseHouseDoor.CheckAccess`
   returns true for everybody pre-AOS, but `UseLocks()` is true, so `BaseDoor.Use` demands the key be in the
   pack. Keys die with the population that holds them, so the halls' doors are unlocked once and for all.
3. **The house sign carries a name that survives the save.** `HouseSign.GetName()` is `Name ?? "An Unnamed
   House"` — an ordinary `Item.Name`. That is the whole registry: a hall is found again by reading its sign.
4. **An ownerless house decays.** `BaseHouse.DecayType` with a deleted owner is `ManualRefresh` in this era.
   Every start, `BotEstate.Adopt` hands each hall to whoever leads that guild now and refreshes it.
5. **A forge is a forge wherever it stands.** `BotGround` looks for item ids 4017 (`SmallForgeAddon`) with an
   anvil within three tiles, and `CraftItem.IsHeatSource` accepts the stone oven's 0x92B. So a fitting placed
   inside a hall is found by the sweep with no new code — which is why stage 3 of the plan cost almost
   nothing.

## The files

| file | the one thing it decides |
|---|---|
| `BotEstate.cs` | which guild owns which hall, what the levy takes, and what a restart hands back |
| `BotPlot.cs` | where a hall may stand — asked of `HousePlacement.Check`, never guessed |
| `BotHall.cs` | the undertaking: walk, check the ground again, levy, place, furnish |
| `BotSteward.cs` | whether the guild can afford one, and which member is away raising it |
| `BotReeve.cs` | the guild's own company for the guild's own dire ground: a leader-class member is offered the Baron's harrow on the nearest square its guild holds at or below dire, and the Baron leaves such ground alone while the guild has a leader (build 92) |
| `BotChest.cs` | the guild's chest: a tenth of the coin taken by work that braves a square the guild holds, and the first payer of the guild's next claim or hall before any member's pack (build 93) |
| `BotChestStore.cs` | keeps the chests across restarts, as the claims are kept (build 93) |
| `BotDues.cs` | the members' dues: a fifth of the coin their work brings in, paid into the guild's chest; what the chest keeps back from everyday draws while the guild saves for its hall or a house in Britain; the `Treasuries:` line (29.09.2026) |
| `BotFittings.cs` | what goes inside: the chest, the room the flood-fill finds, and what each guild wants |
| `BotBench.cs` | the undertaking: fetch one workbench the guild has paid for and set it up |
| `BotFitter.cs` | whether the guild can afford its next bench, and which member is away getting it |
| `BotHire.cs` | the undertaking: buy a contract of employment in town and stand a merchant in the hall |
| `BotHirer.cs` | whether the guild can afford a shopkeeper, and which member is away fetching one |
| `BotShelf.cs` | the guild's counter: stocking it, buying off it, paying its wages, emptying its till |
| `BotSupply.cs` | the undertaking: fetch a batch of what the population runs short of and shelve it |
| `BotSupplier.cs` | whether the guild's shelf is short of something, and which member is away buying it |
| `BotWar.cs` | the wars themselves: who is fighting whom, the score, and the rules that begin, end and forbid one (13.09.2026); a lost war starts the loser's clock for declaring one (`LoserCools`, build 64) |
| `BotRally.cs` | falling in with the guild's war company and fighting with it as a company until the company is done; the company is formed on the first beat of the rally that won its auction, not in the proposer (13–14.09.2026); a rally leaves a company already on a living enemy on it (build 49) |
| `BotWarStore.cs` | keeps the war ledger across restarts under `Saves/BotWars`: wars with their run time, kills and plunder, truces and declaration clocks as time left, and the halls a lost war still owes a move; before it every restart was an amnesty (14.09.2026) |
| `BotSeat.cs` | where each guild lives: the point its hall is raised near, its members rise at, and "home" means; hand-set seats kept in `Saves/BotSeats`; no seat, in the file or by hand, nearer another guild's than `BotRegard.Neighbouring` (`TooNear`, build 64) |
| `BotOffice.cs` | one guild, one officer, one errand — and offering is not the same as being on it |
| `BotLand.cs` | which guild the ground belongs to, and what that does to what work is worth; a held square answers before a hall's yard (06.10.2026) |
| `BotBounds.cs` | ground another guild holds and what may not stand on it: a guild building wholly in its own square or on ground its guild holds (walls and steps, not the centre tile alone), a seat or town house never on another guild's ground, a bot's house only on its own or an ally's; every 30 s a hall now standing on another guild's ground owes a move (carried by `BotRemover`, seat follows), a bare seat is moved to the nearest point that is not, a town house is left; counted in the `Estate:` line (06.10.2026) |
| `BotRegard.cs` | what one guild thinks of another, and the two thresholds that make it a war |
| `BotEvict.cs` | the undertaking: walk over to somebody on your land and tell them to move along |
| `BotBailiff.cs` | whether anybody is on this guild's land who should not be, and who says so |
| `BotFeud.cs` | the board of the war: who each guild has called its members onto, and the four engine facts that let a blow land; a member too far from its company's leader for the door is not called to the company and quarrels where it stands (build 49), nor called to a company fighting inside a pocket proved closed from where it stands (`Pocketed`, build 53) |
| `BotQuarrel.cs` | the undertaking: close with a member of a guild yours is at war with, and fight them |
| `BotFeuder.cs` | offers a member of a guild at war somebody of the enemy — the guild's standing call before its own eyes |
| `BotExile.cs` | who won a war, and the debt the loser owes: its hall goes outside the winner's yard |
| `BotRemove.cs` | the undertaking and its offer: carry a beaten guild's hall out of the winner's yard and put it down again |
| `BotClaim.cs` | what a guild has claimed of the island square by square, what a claim costs, and how one is won; a square where the guild's muster failed rests for that guild, thirty minutes doubling (build 61); the price of a claim on open ground is held until three gather, then paid to the city, or handed back whole to the guild's chest if they never do, while a claim on held ground pays at the declaration (build 408) |
| `BotHold.cs` | the undertaking and its offer: declare a claim on a quadrant and stand in it until it is yours; no member is sent to a square the refusal memory holds, and no square is claimed again while its failed muster rests (build 61) |
| `BotClaimStore.cs` | keeps who owns which square across restarts — the one board on this shard that survives, because it was paid for — with each running claim's held price and the failed musters still resting (shape 2, build 408) |
| `BotSeizure.cs` | a claim on a square another guild holds (to take it or to strike its name off) is war at once, without a meeting, and costs the claimant 30×n of every other guild's regard (never past the war line on that beat) and +2%×n in every town, n its seizures in two days, while time mends no opinion of it above −20×n; the staker weighs it first: not at war elsewhere, not an ally, fewer than two remembered, its side at least as mighty as the holder's (06.10.2026) |
| `BotTownGround.cs` | no guild holds a square where a town's guards act (07.10.2026): any of its 900 tiles answering `Region.Find(...).GetRegion<GuardedRegion>()` not disabled; the staker passes it over (open or held, so it is never weighed for a seizure), `BotClaim.Open` refuses it, and the hourly sweep (first at boot) drops held ones, the price back out of the city's purse into the chest; a hall's yard is kept |
| `BotSeizureStore.cs` | keeps the seizures each guild is remembered for across restarts under `Saves/BotSeizures`, as time ago of the shard running |
| `BotEstateConfig.cs` | what `Configuration/bot-estate.json` is allowed to say |
| `BotEstateModule.cs` | module, phase `World`, requires `Classes`, `Will`, `Population` |
| `BotToll.cs` | the toll on hunting a guild's land: posted (10% of a stranger's hunting coin) or patrolled (20%, once told), who pays, and the price it puts on hunting there (26.09.2026) |
| `BotTollman.cs` | sends a fighter of a patrolling guild to a stranger hunting its land |
| `BotWard.cs` | the tollman's walk and the sentence that makes the toll owed |
| `BotHallKind.cs` | the four sizes of hall (small, sandstone patio, large patio, large marble), the members each holds and what each costs (26.09.2026) |
| `BotEnlarge.cs` | a guild that has nearly filled its hall moves into the next size up, benches and counter carried across |
| `BotOutpost.cs` | a guild's second house on its far land: a place to rise nearer the hunting, and a yard for the toll (26.09.2026) |
| `BotAbode.cs` | houses of the population's own, bought by a bot that has done well, found again by the sign at every boot (26.09.2026) |
| `BotRepose.cs` | a bored bot with a house goes home for a while: boredom falls, spare armour goes in the chest |

## Where a hall may not go, and what "inside" means

Two corrections from Patrick on the evening it was built, both from looking at the thing with his own eyes.

**A chest across the doorway.** The room was taken to be every tile the multi covers — and a house's front
steps are part of its multi, with `BaseHouse.IsInside` agreeing they are inside it. The chest, deliberately
placed nearest the door so it would be easy to reach, therefore landed on the porch in the one tile anybody
entering has to cross. The room is now found by flooding outwards from the middle of the house across tiles
that have a floor and no wall, **refusing to step onto a doorway** — a doorway being the only gap in the
walls, a flood that will not use one cannot get out. Everything is then placed furthest-from-the-door first,
chest included. `BotFittings.Tidy` repairs an older hall at the next start rather than needing it razed.

**Twenty tiles clear of a graveyard.** The engine forbids building *in* Britain's graveyard and says nothing
about building against its railings, so the first hall this shard raised went up three tiles from the north
fence. `BotPlot.Clearance` grows every `NoHousingRegion` on the map by twenty tiles and refuses the belt —
seventeen such places on this island. It is not a cheap rule: with the population living three tiles from the
belt's edge, it refused 1,764 candidates in ten minutes and the near ring stopped yielding anything at all.
`BotPlot.Far` was raised from 90 to 180 to compensate, and the Blade rebuilt at (1438, 1410).

## Tools are bought, and they are the guild's

Patrick's order, the same night: *a guild pays for a workbench separately, and only the owner and the
friends of the house may use it.*

**Bought.** A hall now comes with a chest and nothing else. `BotFitter` offers `BotBench` — fetch one thing
off the guild's wishlist and set it up — whenever the guild can pay for the next one out of the same levy
that bought the hall. The Hammer wants a forge (800) then an anvil (500); the Needle an oven (600), a loom
(600) and a wheel (400). The Blade and the Crown want nothing: a barracks is a place to be.

**The prices are ours, and that is unusual here.** Everywhere else on this shard a price is taken from the
engine. There is no engine price for these: a forge, an anvil and a loom are *carpentry crafts* in
`DefCarpentry` — five logs and 73.6 skill — and no vendor on the island sells one. So these numbers are set
against the only number that was already ours: a hall costs five thousand, and fitting one out completely
comes to about a third of that again.

**Only the guild's own.** The engine will not help: a forge, a loom and an oven have no access check at all,
and `CraftItem.NearHeatSource` asks only whether a fire is within reach. So the rule is applied at the one
place a workshop is chosen — `BotGround.Pick` — through `BotEstate.MayUse`, which asks the engine's own
`IsOwner / IsCoOwner / IsFriend`, so it can never drift from what the door would decide. Our halls make
every guild member a co-owner, so a guildmate passes and a stranger does not.

It is a **hard** rule, asked on both of `Pick`'s passes, and that is safe for exactly one reason: it can only
ever refuse a place that stands *inside a house*. Every forge, fire and counter in Britain is in the street
and is refused to nobody. The `Estate:` line counts how often it bites — *N times a workshop was passed over
as somebody else's* — because a smith walking past three forges it may not use is a fact worth seeing.

## Two rules kept deliberately

**The affordability test is in the proposer, not in the work.** `BotDeed.Outlay` is what a bot's own poverty
is measured against, and the money here is the guild's. A hall that declared a 5,000gp outlay would make its
leader read as destitute and refuse everything else on the shard. See `factor-without-a-floor-is-a-veto`.

**Any member may raise the hall, not only the leader.** Written leader-only first, and it was a veto in
a preference's clothes: the Crown could pay from its first minute and never bought anything, because its
leader is the Captain, the Captain joins a company, and a bot on the `Bound` rung has its auction switched
off. Thirty-two busy, seventeen bound, none free — six minutes with the money in hand and nobody who could
be offered the work. One claim per guild, expiring by the clock, does the job the leader was doing.

**Trodden ground is a preference and not a condition.** `BotQuad.Trodden` chooses between two valid plots; it
never decides whether a plot is valid. A fresh shard has walked nowhere, and a rule that required it would be
a veto that switches the whole subsystem off on exactly the run where it is being tested.

## What to watch

The `Estate:` line in the five-minute summary, and the pair of numbers in it:

```
Estate: no halls stand; 0 raised and 0 taken back from the save, ...; The Blade has 3120 of 5000 and is 1880
short, The Hammer has 640 of 5000 and is 4360 short; plots: 96 put to the engine, 12 would take a hall, ...
```

A guild short by the same amount for an hour means `Price` is set above what this population can reach —
that is the dial to move, and the line is there so the answer is not a silent zero. A `plots:` clause where
`would take a hall` stays at nought means the ground is the problem instead, and the refusal counts say which
of the engine's five rules is doing it.

## Undoing it

`do raze` at Argus takes every hall off the island. It is deliberately **not** in `BotHand.Verbs`, so no mind
can ask for it: `BotDebugNote` writes that array into the model's schema as an enumeration, and a verb left
out of it is unreachable. `do halls` reports what stands, on the same terms.


## The merchant, and what a shop in a hall is for

Ordered 09.09.2026: *the merchant sells nothing — bots do not put anything on it. That is also the answer to
"three percent of the population's time in Britain": a shop of your own in the hall spares the trip to town.*

A `PlayerVendor` is hired for 1,252gp — the engine's own price, over a banker's or an innkeeper's counter —
and until now it stood in the Blade's hall with an empty pack. Three things were missing and all three are
here now.

**Stocking it needs no engine patch.** `PlayerVendor.OnSubItemAdded` opens a lot at 999gp the moment goods
land in the merchant's pack, and `VendorItem.Price` is public. So `BotShelf.Put` drops and then prices. The
one trap is stacking: a second packet of twenty ash joins the first and the *merged* lot is what must be
priced, for forty rather than for twenty. The first cut of this gave the guild's second purchase away.

**Buying off it repeats the gump's own arithmetic**, with one deliberate difference in each direction. The
engine tells an owner to help itself; here the owner pays like everybody else, because what stands on the
shelf was bought with the guild's money and the member who happened to carry the contract has no more claim
on it than the other nine. And the payment is taken *before* the goods move, which the gump does the other
way round — a client cannot be halfway through an exchange, but a bank withdrawal can fail on its own.

**What it stocks is measured, not listed.** `BotShopper` has been tallying what bots turn out to be short of
since the day it was given counters. `BotSupplier` reads that tally. A guild of archers fills its hall with
arrows without anybody writing down that archers want arrows.

**It is not a second faucet.** The supply run buys at the same shelf, at the same price, that the member
would have bought at, and sells to members at cost (`BotShelf.Markup`, one). The same gold leaves the world;
it leaves once instead of nine times. `BotPeddle` remains the shard's only faucet.

**The shopkeeper has to be paid or the engine destroys it.** `PlayerVendor.PayTimer` fires every
`Clock.MinutesPerUODay` — two real hours — and charges `20 + (shelf value - 500) / 500`, deleting the vendor
the first time the bill is larger than its purse. It is seeded with 1,000gp, so an empty one lives about four
days. `BotShelf.Wage` tops it up out of the guild's money whenever a member is standing at it anyway.

**And the shelf survives a restart, which the purses do not.** `BotProgress.Savings` is off by Patrick's
order of 08.09.2026 — what a bot has banked is the session's. A merchant is saved with its house, so a guild
that stocked up last night starts today with supplies its members can buy without walking to town. That is
the same rule as skills and buildings rather than an exception to it, but it is worth knowing it is there.

## Land, standing, and the war that is switched off

Stages three and four of `PLAN-guild-lands.md`, built 09.09.2026.

**Land is a multiplier.** Every tile within `BotLand.Reach` (40) of a hall belongs to that guild; the nearest
hall wins where two claims overlap. Work on your own guild's ground is worth x1.25 and on somebody else's
x0.7, as one more factor in `BotAppraisal` beside crowding and caution. It is never a veto, for the reason
this project has paid for most often: five guilds each fencing off ninety tiles is a population that cannot
work. The claim is computed from the halls on every call rather than stored, because a stored map of who owns
what is a second copy of the truth.

**Standing is a number per ordered pair of guilds**, moved only by things that happened: work finished on
somebody else's land (-0.5), a refusal to move along (-6), a killing (-25), a trade (+0.5 both ways) and the
slow souring of neighbours whose yards overlap. That last is scaled by how much they overlap — halls 18 tiles
apart quarrel four times as fast as halls 76 apart — because a flat penalty made an island of four halls into
one scheduled war rather than a rivalry.

**War is on**, by Patrick's order of 09.09.2026 — *"they should feel the consequences."* It was built off,
because he had named stage four the only part of the plan that can make the population smaller; it then ran
for an evening with the opinions moving and nothing dying of them, and the numbers behaved: a quarrel driven
by trespass and by bots refusing to move along, mended by trade, reaching −94 of the −100 it takes.

Nothing else needed writing to make it bite. `Guild.AddEnemy` makes the other side `Notoriety.Enemy`, which
makes striking lawful — no criminal flag, no guard reaction — and corpses lawful to loot, and `BotPickings`
already loots what a bot killed itself.

**Three numbers to read now that it does, and none of them could be read before:** deaths an hour, skill lost
per death, and how much of what was looted was ever used by whoever took it. Off again is one dial:
`dial BotRegard.Warring false`, or `"War": false` in `bot-estate.json`. A war already declared ends by itself
once the two guilds trade their way back above `Amity`.

**Eviction is deliberately almost nothing** — a walk, a sentence and a look at whether they went. No blow is
struck in it at any standing. What it produces is a fact: they were asked and they are still there, which is
what moves the opinion. The measure that says the land rule has gone wrong is the completion band falling
while evictions rise.

## An offer is not an errand

`BotOffice`, and it corrected the same fault in all four officers at once. Each proposer wrote a claim on its
guild for the length of the whole errand — three or four minutes — and then handed the errand to an auction
free to prefer something else. When it did, the guild was locked out of that officer for minutes having done
nothing, and nothing anywhere said so.

Measured on the one that matters most: in the session of 09.09.2026 at 01:59 the steward offered a hall five
times and three were raised. Two guilds spent three minutes each sealed off from the most valuable errand on
the shard because of an offer nobody took. `BotHall.Prior` carries a note about being raised from ninety to
four hundred under exactly this pressure — that treats the symptom.

So there are two lengths. `BotOffice.Offering` is written when the offer is made and lasts twelve seconds;
`BotOffice.Hold` is written by the errand itself every beat it is alive and lasts as long as the officer
says. The `Estate:` line reports how many offers turned into errands and how many lapsed unclaimed.

## Guild work was teaching the shard that guild work loses money

Takings are `coin + made`, and coin is the change in the bot's *own* purse. The levy comes partly out of that
purse, so the ledger read:

```
finished hall: raised The Lantern for 5000gp: -20 in 0.4 min (-50/min)
finished fit:  set an oven up for 600gp:      -33 in 0.9 min (-36/min)
finished hire: hired a merchant for The Blade: -148 in 1.6 min (-93/min)
finished supply: left 20 SulfurousAsh ...:     -60 in 0.9 min (-69/min)
```

`BotCommons.Corrected` then drags the whole trade's estimate towards those numbers. All four now declare
`Made` as what the bot's own purse put in, measured across the payment — which is the rule `BotRestock` has
always stated in as many words: goods are worth what they cost. A hall, a bench, a shopkeeper and a shelf of
reagents are goods. The errand comes out at about nothing a minute: never punished, never preferred over work
that produces something.

## A bandit is set on standing, not chased (07.10.2026, loops18)

`BotFeuder` sets a fighter on a bandit of The Shadow in sight. On build 418 all 19 quarrels taken were against two recruits going
about honest work - one mounted and carrying a scroll, one in a company - and none was finished: 15 "could not get nearer", 2
"no quarrel any more", one death in six seconds. The bandit must now be standing (not walking unless beside the fighter,
`BanditBeside` 2), in no company, not about steadfast work, and reachable on the reach ledger; the fighter out of a company, fit
and not the weaker, in lawful ground. Each refusal is counted by its cause in the feuder's clause ("bandits in sight not set on").
Dial `BotFeuder.BanditStands`.

## Town ground is nobody's to claim (07.10.2026)

Patrick, 07.10 ~10:20: "guilds may not claim town squares with guards. Remove such claims right now and give the guilds
their money back if they paid for the claim." A thirty-tile square is town ground when any one of its 900 tiles is where a
guard would come: `Region.Find(tile, map)` at z 0, then `GetRegion<GuardedRegion>()` not disabled (the question
`BotHaven.Guarded` asks of a point). Towns and the moongates count; Buccaneer's Den, whose guards are off, does not. The
staker (`BotHolder.Worth`) passes such a square over whether it is open or another guild's, so a held one is never weighed
for a seizure; `BotClaim.Open` refuses it before any coin moves. `BotClaim.TownSweep` runs on the first claims look after the
halls are adopted and every hour after: every held town square is dropped (WRN naming guild, square, town and how many of
its tiles are guarded) and a square that was paid for is refunded what it cost: the price on record in
`Configuration/bot-townground.json` (`Paid`: {Guild, X, Y, Gold}, read off the logs, each used once), else the standing
price `BotClaim.Price` (5000gp; the store keeps "bought", not the price), out of the city's purse (`BotCity.Unclaim`) into
the guild's chest — a purse that cannot cover it still makes the chest whole, and the shortfall is the city's debt,
counted and logged once a sweep. A claim still mustering there is called off,
its held price back to the chest. **Not touched:** a hall's own square (its yard) stays its guild's even where it reaches
into a town, a seat is a point and no claim, and a guild house in a town is bought from the town (`BotGuildHouses`), a
different thing. A yard in a town cannot be seized, since no claim is opened on town ground. Counted in the `Treasuries:`
claims clause ("town ground (where guards act): …") and in the `City:` line ("refunded for town ground"); dials
`BotTownGround.Running`, `SweepEveryMs`.
