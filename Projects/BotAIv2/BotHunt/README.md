# Hunt: where the money in this world comes from

The first work in the project that **brings new gold into the world**. Everything else only moves it about.

| File | What is in it |
|---|---|
| `BotQuarry.cs` | what is worth fighting, and where what is left of it is lying; a company is not handed a creature swimming where nobody can stand (`Afloat`); a creature found unreachable again where it stood is shunned twice as long each time (`Reshunned`); a lone hunter asks the reach ledger and the refusal memory about its quarry, as a company always did (build 59), counted apart as `Penned` and `Unwelcome`, and both hunter and company ask whether the creature's own cell is in a proved pocket rather than the ground around it (`ShutOff`, build 60) |
| `BotSlay.cs` | an obligation: close → fight → go through the corpse |
| `BotHunter.cs` | the proposer: who is whole enough to want a fight; a prowl's company is counted at the edge of the town, where it will be raised; ground where bots have lately died is prowled only by a company (`Deadly`); ground whose road from home runs more than `Detour` past the straight line from the asker is counted (`Roundabout`) and passed over only with `ReadsRoads` on (build 63, see `BotRoads`); the prowl it offers is a sweep: the chosen ground and the best squares beside it the hunter takes alone (`Sweep`), or the squares a sweep left for a fight (`Again`) |
| `BotHuntConfig.cs` | `Configuration/bot-hunt.json` |
| `BotHuntModule.cs` | module, phase `World`, requires `Classes`, `Will`, `Auction` |
| `BotBand.cs` | calling a company together for something one bot cannot take, and seeing it through |
| `BotGlean.cs` | picking spent ammunition up off the ground |
| `BotMuster.cs` | offers a bot the chance to call a company against something it must otherwise walk past |
| `BotPickings.cs` | going through something this bot killed without meaning to |
| `BotPlunder.cs` | going through a chest, crate or barrel standing out in the world — a camp's, a ruin's |
| `BotFreedom.cs` | getting a prisoner out of a camp and walking it home for the engine's bounty; the prisoner is the chooser's from the moment it chooses, so a boot does not send the whole population to one cage (build 57); a prisoner the engine has paid for and let go is delivered, not lost (build 58) |
| `BotProwl.cs` | going to look for a fight, when there is nothing to fight where the bot is standing; its failures say the strength gathered and the tile the walk stopped on; a walk that stops closing looks once for ground on its own side a search proves walkable (`Redart`) before it gives up, and so does a walk whose road is refused on the way (`Bend`, build 62); from 06.10.2026 a sweep of 2–4 squares (`BotHunter.Sweep`), each waypoint reached before the next, "found nothing" finished only with every square walked, a fight found leaves the rest for after it (`BotSweepRecord.Leave`), priced by what sweeps of its squares paid (`BotSweepRecord.Worth`, `Unpaid`) |
| `BotBarrier.cs` | where walks have stopped getting nearer and which way they were heading, so the hunt stops throwing darts past those places |
| `BotOutlaw.cs` | murderers: red for an hour by the engine's own count of kills, ten cells at the bottom of the map with one bot to a cell, the clock that places, releases and redeems, `BotSentence` (sitting the sentence out) and `BotManhunt` (the Baron's patrol against a red, a posse pressed into it) (build 70) |
| `BotOutlawStore.cs` | keeps the murderers and the cells across restarts, each clock as what is left of it; a restart used to be an amnesty the engine did not grant (build 83) |
| `BotRob.cs` | `BotRob`, setting on one of ours for its purse and going through the corpse; and `BotRobber`, the rarest thought on the shard — rolled from `BotOutlaw`'s clock and pressed on a free or busy fighter, never offered at auction, because an offer at 400 over three minutes lost to every hunt and was gone (build 70, 72) |
| `BotLawful.cs` | the law-abiding set on a murderer they can take between them: every five seconds, for each red outside a guarded town, the strongest fit fighters in sight are pressed against it with the patrol's fight until together they come to one and a half times its strength, and nobody is pressed if they would not (build 111c, Patrick's order of 17.09.2026) |
| `BotShadow.cs` | Hiding, Stealth and Detect Hidden as a bot uses them: the engine's own skills through `Skills.UseSkill`, at the engine's gain rate, under the Renaissance rules (Stealth wants Hiding 80 and armour under 26; a hidden step without Stealth's steps reveals); `RobHiding` 30, `StalkStealth` 30 (build 112) |
| `BotSkulk.cs` | five minutes of practising hiding, and quiet steps once allowed: pressed on a tempted fighter that cannot hide well enough to rob, and every half hour on anybody with any Hiding (build 112) |
| `BotWaylay.cs` | a thief that has thought of robbery and found nobody to rob, going to where the work is and waiting there |
| `BotLieLow.cs` | a murderer getting a dozen tiles away from the body, hiding and lying still for three minutes (build 112) |
| `BotInquest.cs` | every murder's body: the nearest law-abiding bot to come within six tiles searches with Detect Hidden, calls out, and names the killer to the Baron if it found or saw it; also presses a robber into lying low (build 112) |
| `BotUnderworld.cs` | the island's criminal record by name (murders, those from hiding, robberies, blackmail, catches) and The Shadow [SHD], the sixth guild: founded by two killers from hiding, five at most, kept outside `BotGuilds` and re-made at every boot; its hideout, given away by a caught member (builds 113, 114) |
| `BotUnderworldStore.cs` | keeps the record and The Shadow across restarts (build 113) |
| `BotRaid.cs` | the Baron's raid on a hideout given away: a posse marches there, searches with Detect Hidden, sets on every member found, and the hideout is burned (build 114) |
| `BotAmbush.cs` | the blow struck out of hiding: the weapon's own swing three times over and the victim held where it stands, three seconds by hand and one by arrow (build 117) |
| `BotQuarter.cs` | what a mark does when a robbery has beaten it down to a quarter of its health: fights on, runs for the town, or throws its gold |
| `BotLair.cs` | The Shadow's camp: a fire at the hideout, a chest beside it that only the band opens, and the band going to ground when a stranger comes near (build 118) |
| `BotStash.cs` | a thief carrying its takings out to the chest, because a red cannot reach a counter and a catch now strips its pack (build 118) |
| `BotFence.cs` | the band's keeper: a Sage or an Architect taken in at two thieves, who commits no crime, sells the band's goods, and is a criminal worth 5000gp the moment a witness tells (build 119) |
| `BotSilence.cs` | the fence's chase after a witness it could not buy: cut it down inside thirty seconds or it tells (build 119) |
| `BotHoleUp.cs` | the keeper with a price on its head going to the hideout and sitting it out, because nothing it could be doing instead survives a catch (build 139) |
| `BotFetch.cs` | the fence's run out to the chest for the goods, after which the island's ordinary peddling turns them into coin (build 120) |
| `BotBurgle.cs` | a bandit breaks into a guild's hall — hidden, the lock picked by skill — and carries off a share of the treasury (26.09.2026) |

---

## Why this became urgent

Before this folder **there was no gold in the world at all.** Not "little" — not one line that creates a coin: a
bot is born with none, trade between bots moves it about, and a shopkeeper's counter is where it leaves. The
consequence is arithmetic: `BotShops.Buy` computes what is affordable as `purse / price`, which at zero returns
zero, so **everything with an `Outlay` failed on its first beat** — sewing, writing, filling a book, restocking.
The only thing that could happen was digging, because digging is free.

A monster's purse is the only faucet. Everything else is that same gold one step further from the field: a
fighter pays a smith for a blade, the smith pays a gatherer for ore, a caster pays a scribe for a scroll.

---

## The fighting is entirely the engine's, and that is the key fact

`Mobile.Combatant = target` **starts a server-side timer by itself** (`CheckCombatTime`, every 0.01 s) which
checks range and line of sight, swings the weapon, rolls to hit, applies damage and wears the blade down. No
client is involved and there is nothing to simulate.

So this file never decides a blow. It decides three things: who to go for, when to run, and what to take off the
corpse.

## Who: the biggest thing we can take

Everything the judgement needs is already written in `BotCombat`. `Power` is toughness × damage, `Hostile`
answers what counts as a target at all, `OurPower` sums our side **including the neighbours**, `Tolerance` is 1.5.

There is exactly one new question: of the things I could beat, which is the biggest. Not the nearest — distance
is already priced by the appraisal's nearness factor, so a hunter that preferred the closest rat would be paid
twice for being lazy.

The threshold is **the same tolerance the flight decision uses**. Not elegance: a bot must not walk towards
something it would immediately run away from.

**And cooperation here is free.** It is judged against `OurPower` rather than against the bot alone, so two bots
in the same field take on what one of them would have walked away from — and neither had to be told the other was
there. No party, no leader, no message: the same arithmetic-from-a-shared-fact that decides everything else here.

## When to run: 40 %, and that the threshold exists matters more than its value

Before this session the "health is going" rung was served by nobody — so the brain's answer to failing health was
"hold on to what you are doing", and what this bot is doing is dying. `BotMend/` serves it now, but flight still
lives **here**: mending under blows is pointless, and the decision to break off a fight can only be taken by the
work that is running it.

In v1 this cost 443 deaths in a night, 104 of them one bot rising in the same tile every twenty to forty seconds.
Not once in all 443 did "too dangerous to get up" fire. So the flight decision lives inside the obligation, and
an obligation has to be willing to give itself up.

A failure marks the place with caution — exactly the right record: **this patch of ground kills me.**

The threshold for setting out (`FitAt` 80 %) is above the threshold for fleeing (`FleeAt` 40 %) on purpose:
without the gap a bot that has just escaped is immediately offered the same fight by the same arithmetic.

## What to take: everything, and to the market rather than to a counter

There is no "treasure or rubbish" test here and none is needed. **Everything picked up goes to the bots' own
market**, and a stall that nobody bought from in half an hour is carried to a shopkeeper by `BotPeddler` — the
same road produced goods travel.

One bot's junk is another's material, and the only thing that can tell the difference is the market. So the
market is asked, every time, before a counter is.

The limit is **weight, not value**. Loot is not bound, so it weighs, and a hunter that empties a corpse into a
full pack is a hunter that cannot carry its own takings home. What is left stays on the corpse for whoever comes
past.

Gold goes into the purse and is counted as gold; goods are counted as `Made`. Neither is counted twice.

---

## The boundaries, and why they are deliberately narrow

Thirty tiles of sight inside two hundred tiles of `Roam`. v1's death loop cannot be built at those boundaries: a
bot is never far from the place it will get up.

The price of that is stated plainly: **the monsters near a town are poor, so the faucet will be thin.** Named
hunting grounds — a graveyard, a dungeon mouth — are the next step, and they will need a separate defence of
"do not get up where you are being killed", because that is exactly where v1 died.

## The one honestly expensive line

The proposer makes a **real spatial query every time** a free bot asks. This is the only exception to "a question
of the world must not be an expensive one", and it is unavoidable: a vein stands still and can be remembered, a
shopkeeper stands still, but a monster walks and respawns — a remembered one is a lie within a minute. What is
remembered instead is **which patch pays**, and the thing that remembers it is the ledger, whose business facts
about ground are.

---

## What appeared outside this folder along with it

**`BotShops` learned to sell.** `Buys` asks the shopkeeper itself (`IsSellable`/`GetSellPriceFor` — about a
specific object, because that is the only question the engine answers), `Buyer` finds the nearest one, and `Sell`
goes through `OnSellItems`. The takings are **measured** rather than summed from price tags: the engine decides
how much of an order it honours. And it requires `IsStandardLoot()`, while the bind marks things `Newbied` — so
**the weapon and the book cannot be sold at the engine level**, a free guarantee on the "not merchandise"
promise.

**`BotPeddle`/`BotPeddler`** — selling as work, with one condition for going: a stall that has never sold one and
has stood for `StaleMs`. Half an hour in front of the whole population and nobody took it. No "is this junk"
test, no table of worthless things. And it can only ever offer what the bot **itself decided to sell**, so tools,
herbs and paper cannot be sold — they are never on a stall.

**A fighter came to want what a crafter makes.** A weapon wears on every landed hit and the engine destroys it at
zero; `BotShopper` now notices a missing blade (at the highest priority, above tools) and spent arrows, and
`BotRestock` gained a "buy it off another bot" route. Whichever is cheaper, and **a shopkeeper is the ceiling
rather than the preference** — which is precisely why a fighter's gold goes to a smith instead of out of the
world.

`IBotWilful` gained `Bond`, because otherwise "buy another of what I had" is unanswerable: the class offered six
blades and the roll handed over one, together with the skill that swings it.

---

## What to check with a client

1. `The hunt is on: ...` at startup, and `Hunt` among the eleven `World`-phase modules.
2. `took on hunt`, then `fighting a skeleton`, then `finished hunt: 2 things and 43gp off a skeleton`. That is the
   first gold in the project's history.
3. `[bots`, the `purse` column — it must stop being zero. If it is zero for everybody after ten minutes, look for
   the `Nothing within 30 tiles ... is worth fighting` line.
4. The shops line on reload: `N things bought for Xgp, M sold for Ygp`. **The difference between those two
   numbers is the health of the economy**, in one subtraction.
5. Kill a bot by hand near a monster and check that it does not come back to die in the same tile: a failed hunt
   marks the place with caution.
6. `bot-hunt.json` → `"FleeAt": 0.0` — the bot stops running away. That is the A/B for v1's death loop; switch it
   on only to see it.

## What is not here

**Squads.** `BotSquads.Form` is still called by nobody — the decision is solo hunting, with the cooperation
`OurPower` gives for free. Joining a company as an obligation with a price comes after solo hunting has been
measured.

**Magic in a fight.** A caster walks into melee. The book fills up, spells are not cast, and reagents are spent
only on writing.

**Armour.** There is none in the kit at all. That is the next step, together with smithing — before it, armour
would be a need a shopkeeper satisfies, i.e. a new gold sink rather than a crafter's income.


## A chase has a length as well as a clock

Patrick's order of 09.09.2026: *if the quarry runs and the bot cannot reach it, drop it after fifty tiles.*

`BotSlay` had `CapMs` — a chase gives up after so long — and a clock does not bound a bot's position. A quarry
that walks away as fast as the bot walks after it is a bot travelling in a straight line for as long as the
cap allows, and the shard was paying for it: fourteen bots carried home in an hour on 09.09.2026, all in
bursts of two to six inside one minute, and the six at 17:34 were standing in a single ten-tile patch at
(1208, 2202) — **737 tiles from home, where `BotPopulation.Roam` is 200**. A company had chased something
clean off the ground the population is allowed to choose work on, and got stuck there together.

`BotSlay.Leash`, fifty tiles, measured from **where the chase began** rather than from home. That distinction
is the point: the walk out to a quarry is a journey the decision layer priced and agreed to, and what was
never agreed to is that walk becoming unbounded the moment the quarry starts running. The roam limit governs
what a bot may *choose*; this governs what a chase may *drag* it into.

Counted as `BotSlay.Slipped`, and the ending says so — *"a dire wolf drew it 63 tiles and was let go"*.


## Archers backed away from things that were not chasing them

Patrick, 09.09.2026: *"why the hell do archers always run away from mobs? They should kite, yes — but if they
can shoot they MUST shoot."*

One condition. `BotSlay.Kiting` asked whether the quarry was within `reach - 1`, and a bow reaches ten — so a
creature **nine tiles away**, which is the distance an archer exists to fight at, read as "something has
closed on me". The tally over one session: **1,514 kites against 860 shots**. Nearly two beats in three spent
walking backwards from things already exactly where they were wanted.

`BotSlay.KiteWithin`, five tiles — half the reach a bow is worth, which is a statement about the archer's
advantage rather than a tidy number. Above it the bot stands and shoots; below it the idle window is spent
opening the distance. Widening cannot cost a shot by construction, because the kite only runs while the bow
is reloading; what the old nine-tile gate wasted was position and time.

**Four was tried first and was measured wrong within the hour** — one past `TooClose`, tidy, and one tile
wide, because anything inside three is already handled by the branch below. Result: 1,537 archer asks, every
one of them *far enough off to simply shoot*, and not a single kite. Half of what was asked for had gone
inert.

### And a lesson about the instrument rather than the shard

At four tiles the tally read *longest clock seen 227ms against 1700ms needed*, and that was read here as
proof that the bow never fires — the engine returns a quarter of a second and retries when the shooter has
moved inside the last second, which fits 227ms exactly. It was wrong. `Longest` was only sampled **after** the
distance gate, so tightening the gate stopped it being sampled at all, and a number that had almost stopped
being written read as a number that had stopped moving.

Sampling stillness on every ask settled it:

```
1974 of them had been standing still long enough for the engine to loose an arrow,
longest stillness seen 37647ms against the 1000ms it wants — longest clock seen 5382ms
```

A weapon clock reaching 5,382ms is `GetDelay` having been returned, which only happens when a shot was
actually loosed. The bows work. **A counter behind a gate measures the gate, not the world** — read where a
number is written before believing what it says.

## A murderer, the towns and the law-abiding (build 111, 17.09.2026)

**A red walks round a guarded town, and is refused the step into one.** `BotOutlaw.Keeps` refused a red work inside a
guarded town and work whose straight road crossed one, and the walker's road is not the straight one: Elspeth, red,
prowled from about (1474, 1152) towards (966, 1590), the line between passing north of Britain's western ward, and the
road went three tiles into the ward at (1160, 1541), where she died. Now a red's every search keeps out of guarded
ground (`BotOutlaw.Road`, `BotAvoid.Towns`) unless it stands in a town or walks into one, every step a red takes from
open ground into a guarded town is refused (`BotOutlaw.Steps`, from `BotMobile.Move`), and a red's unreached work is not
written into `BotRefused` for everybody. `dial BotOutlaw.WalksRound false` undoes it.

**The law-abiding set on a murderer they can take.** Until then a red was hunted only by the Baron's patrol and by
whoever it set on. `BotLawful` looks at every red outside a guarded town each five seconds and presses the strongest fit,
free law-abiding fighters in sight against it — never a healer, the Baron, a company's bot or one in a fight already —
until their strength together, with anybody already on it, is one and a half times the red's; if the strongest four
would not come to that, nobody goes, and the log says so. The fight is the patrol's, so a red that falls to it goes to
a cell.

## Criminals act unseen (build 112, 17.09.2026)

Patrick's order: criminals hide, train it and stalk their victims hidden; a bot that finds a body searches for the killer
with Detect Hidden and tells the Baron. He chose the engine's own gain rate, and that a bot which cannot hide yet practises
instead of robbing.

**The rules are the era's, not ours.** In the Renaissance a hidden mobile's first step reveals it unless Stealth has
granted it quiet steps — one per ten points of Stealth, for ten seconds — and running or riding reveals it anyway;
Stealth is refused below Hiding 80 or in armour of 26 and over; Detect Hidden searches a tenth of its skill in tiles, and a
player-mobile with any of it notices a stealther within four tiles by itself. Every use costs the ten-second skill clock.
From nothing, Hiding 80 is six or seven hours of practice at the engine's rate.

**So a robber has two ways in.** One that can move hidden (`BotShadow.CanStalk`: Stealth 30, and allowed) hides fourteen
tiles off and comes in on quiet steps; one that cannot walks up beside its victim, hides and lies in wait twelve seconds.
Either way the blow reveals it. A fighter tempted with Hiding under 30 goes to practise instead (`BotSkulk`), and anybody
with any Hiding is sent back to practise every half hour — Hiding above nought is kept across restarts by `BotProgress`,
and is the only mark a crook carries.

**Six of them practise in earnest** (`BotSkulk.Rank`, build 116). The gain is the engine's, and the engine's gain falls off
a cliff at ten: the nine stints that ended together at 22:31:40 on 17.09.2026 gained 0.2 to 1.2 apiece where the stints
below ten had gained 2.5 to 8.5, which is thirteen hours from eleven to the thirty a robbery wants. So the sweep names the
`Apprentices` best hiders still short of the threshold — Patrick's "most skilled and cunning" read literally — and presses
them back every five minutes instead of every thirty. The list is re-reckoned each sweep, so reaching the threshold, being
taken into The Shadow, dying or being jailed drops a bot out of it and the next best takes the place: the island loses six
pairs of hands to the underworld, not sixty.

**After the kill the robber gets away from the body and lies low** (`BotLieLow`), and the body is left to be found
(`BotInquest`). The finder searches with Detect Hidden — with none of the skill that searches nothing, and it is how the
skill is learned — and calls out "Murder!". A killer brought out of hiding, or standing in plain sight within eighteen
tiles, is named to the Baron (`BotOutlaw.Tell`). **The Baron's patrol goes only after a murderer he has been told of and
can see** (`BotOutlaw.AtLarge`); the law-abiding pass a hidden red by, and tell the Baron of one they see. A red read back
after a restart counts as told. `dial BotShadow.Running false` sends robbers in openly again.

## The Shadow (builds 113 and 114, 17.09.2026)

Patrick's order: the most skilled and cunning murderers may found a sixth guild of thieves and brigands, five at most,
whose work is robbery, hiding, blackmail and killing — at the price of prison and pursuit. His choices: two killers from
hiding found it and the better hider leads; it keeps a secret hideout far outside the towns, raided once a caught thief
gives it away; blackmail is "pay or die", and whoever pays tells the Baron; it is The Shadow [SHD].

**Kept outside `BotGuilds` on purpose.** The trade guilds are dealt round the crafters at every boot and a guild with
nothing to show loses members to the review; The Shadow is an engine guild made at founding and re-made after every
muster from the names in `BotUnderworldStore`. The engine's guild is what makes its members allies and keeps a robber off
its brother.

**Its work.** A member thinks of a crime at 0.25 every ten minutes, and half of those are blackmail: a robbery that stalks
its mark, steps out of hiding beside it and asks for a third of its purse. The mark pays by degrees as the thief — with
any of The Shadow in twelve tiles — outweighs it; a refusal is answered with a blow by a stronger thief and with a retreat
by a weaker one; either way the mark tells the Baron. Members rise at the hideout and practise hiding there every quarter
hour.

**Its price.** A told thief is wanted for half an hour (`BotOutlaw.Want`): known to the Baron and grey in the engine's
reckoning, so the law-abiding and the Baron's patrols may lawfully set on it, the guards kill it in a town, and no healer
helps it; a wanted thief that falls to them goes to a cell like a murderer. A member caught gives the hideout away, and the
Baron raids it (`BotRaid`): he and a posse search it with Detect Hidden, set on every member found, and burn it.

## The band's household (builds 117 to 122, night of 17.09.2026)

Patrick's eight orders for the night, and where each of them lives.

**The blow out of hiding** (`BotAmbush`, 7). The weapon's own swing three times over and the engine's own hold: three
seconds from a hand weapon, one from a bow. The first one ever struck, at 23:38:40, was an arrow for 96 against seventy
hit points — "an archer all but one-shots another bot" was the order and the arrow read it back word for word.

**The price of blood** (`BotUnderworld.Price`, `BotCity.Blood`, 5). 1000gp for the first murder proved and 500 for each
after, paid at the kill: nothing at all when the Baron's own hand did it, the whole to one bot, split between a company
within twenty-four tiles of the body. The first payment, at 23:41:51, took the treasury from 1450gp to 450 — a head is
dear, and that is the point of it.

**The camp** (`BotLair`, 3). A fire at the hideout and a chest beside it, both plain engine items so they survive a
restart; found again after a boot by looking for them rather than by a serial in the store. Anybody not of the band
within twelve tiles sends every member standing there into hiding.

**What the band keeps** (`BotStash`, `BotFetch`, 2). A thief carries its takings out to the chest, because the towns are
shut to a red and what it carries when it falls is gone. The fence carries the goods from the chest to a shopkeeper no
red could reach, and the coin comes back to the chest by the same sweep.

**The fence** (`BotFence`, `BotSilence`, 2b). A Sage or an Architect, taken in once the band holds two thieves, known by
its trade rather than by a line in the store. It commits no crime and is exempt from the temptation and from practice.
Seen within twelve tiles of a thief by anybody not of the band, it offers 500gp or half the witness's purse; if it
cannot pay it hunts the witness for thirty seconds, and a witness that gets away makes it wanted with 5000gp on its head.

**What a bandit may join** (4). Championships, which never asked whether a bot was red. Not the city's errand board and
not a company: both are refused to a thief, and both would walk it to a guarded town.

**How a thief learns** (8). Like any other bot. The gain is the engine's, and `BotSkulk.Apprentices` is nought.

**The wanted board** (6). The dashboard's Band tab: every bandit at large, dearest head first, and nobody who is in a
cell.

## What a catch costs, and what the keeper keeps (builds 135 to 139, 18.09.2026)

The night's work built the band; this is the day's, and all of it came out of one question Patrick answered on the
morning of 18.09: **what does being caught actually take off a bandit?** "The bank is docked a thousand and the whole
corpse is confiscated; being caught is the very worst thing that can happen to a bandit, because it literally loses
everything and its progress stalls."

**The catch takes everything** (`BotOutlaw.Seize`, 135). The confiscation written the night before had never taken a
single thing, and the reason is worth keeping: it ran at the cell door, over the backpack of a bot that was already
dead. Every catch on this shard is a kill, so the purse, the tools and the armour are in a corpse by then and the
hunter opens it a minute later. Now the corpse is emptied from inside the death hook, the bank is docked `BankFine`,
and the bond is struck out — that last being the part without which none of it means anything, because bound gear
survives death here only by `BotBinding.Restore` handing the bot another one when it rises. What the bond keeps is the
*roll*, the weapon and ammunition this body was made with: clearing those too would leave a stripped bandit unable to
buy a weapon for the rest of the session, which is destruction rather than punishment.

**The keeper's other half** (`BotFence.Wants`, `BotShopper`, `BotStash`, 136 and 139). Patrick's keeper "looks after the
band's goods and orders" and only the goods half existed. The order is read off the band rather than written down: the
most any one member keeps of a kind, times `Spares`, less what the chest already holds — so a band that takes in an
archer starts asking for arrows with no list to maintain, and a chest that would otherwise become a warehouse of eight
skillets stays a larder. `BotShopper` asks it last, after everything the fence needs for itself, which puts the whole
of the island's buying machinery behind it; the walk out to the chest puts down what was bought and keeps the keeper's
own kit back by splitting the stack.

**Buying a witness off had never once been possible** (137 and 138). The ledger read `0 witnesses bought off` over every
session since the rule was written, and the reason was arithmetic: a witness asks half of what it carries with a floor
of 500gp, the fence paid out of its backpack, and every seller on this shard is paid by deposit — so the keeper was
asked for five hundred while holding a hundred, every time. The ask is capped at `Most` and the bill is settled out of
the pocket first and the account for the rest, which is how the guild's dues, a horse and a hall's levy are already
paid here.

**And a keeper that is never at large keeps nothing** (`BotHoleUp`, 139). A fence is worth 5000gp, five times the best
price any murderer on this shard has earned, so the whole island hunts it the moment the Baron hears: three catches in
one four-hour session, all of them the fence, 78% of that session in a cell. It now walks to the hideout and sits the
price out. Not hidden — a Sage has no Hiding worth the name — but away, because the hideout is two hundred and fifty to
five hundred road steps from anywhere anybody lives and patrols are raised within `BotOutlaw.Reach` of a murder. The
price still stands and a raid still finds it there; it simply stops walking past the people who would collect.

## Sweeps are not sent where a sweep could not walk; hunters at their fight are not called away (07.10.2026, work18)

- `BotHunter.NoWay`: a square whose waypoint a sweep's walk was refused or gave up on within `BotSweepRecord.WaylessMs` (an hour;
  filed by `BotProwl.Bend`), or whose point the reach map calls an island or a pocket, is not a sweep's square: asked of the darts,
  the named pickers, the ring of a sweep and the first square in `Offer`. Counted at the end of `The island:`'s sweep clause
  ("looks at a square refused …, prowls not offered for their first square") and "squares filed as having no way".
- `BotSlay.Engaged` (the quarry within reach and standing) makes `BotSquads.Pressable` false, and the delve's party and the
  harrowing skip such a hunter too (`BotSquads.SpareFighters`, counter "hunters at their fight left to it" in the squads line).
  On build 416, 103 of 133 hunt drops were "not taken up again after enlist", 87 of 163 presses caught the hunter fighting.
