# Crime: the underworld's trades and the town's law

Patrick's idea of 07.10.2026, approved for the night: "Thieves stealing from packs in the crowd by the bank (Stealing is in the
engine), ambushes on lone carriers with goods on the road, a fence, a wanted list: the town sets a bounty on a head, bounty
hunters from any guild, trial and jail when caught (time in a cell, a ransom by the guild), repentance and a return to honest
life. This gives a living reason for guards, escorts and caravan guards."

**Built on what the underworld already had.** The cells and their clock, the engine's grey for the wanted, the law-abiding who
set on an outlaw in sight and the Baron's patrol are `BotHunt/BotOutlaw`'s and `BotLawful`'s; The Shadow and its fence are
`BotUnderworld`'s and `BotFence`'s; a beaten mark's choice (fight, run, throw half the pack down) is `BotQuarter`'s. This folder
adds the town's books — who was reported for what, where, and what the town pays for the head — and four pieces of work, each a
deed in the auction. Counted at the end of the `Underworld:` line (`crime: …`); every event is also a `crime` event on the
dashboard's stream and a line of speech.

| file | decides |
|---|---|
| `BotCrime.cs` | The town's books: the thieves' roll and the recruit, reports and the wanted list, the bounty, the term (doubling for a repeat), the ransom, release and repentance, and the stolen-goods book. Its clock is the outlaws' (once a second, slow work once a minute). |
| `BotCrimeStore.cs` | Keeps the books across restarts (`Saves/BotCrime`), clocks as durations of uptime. |
| `BotCrimeModule.cs` | `Configuration/bot-crime.json`, the four proposers, the boot line. |
| `BotCutpurse.cs` | A thief's turn in the crowd by a bank: the engine's own Stealing on a random thing in a bot's pack. |
| `BotHighway.cs` | The ambush: 2–3 of The Shadow lie in wait ahead of a lone carrier with goods, outside any guarded town. |
| `BotFencer.cs` | Stolen goods to the fence at half their worth, and the fence's (or a fenceless thief's) sale in another town. |
| `BotBounty.cs` | A free fighter of any guild takes a head off the wanted list, priced at the bounty. |

## The rules, with their numbers

- **Thieves** (`BotCutpurse`). A thief is a member of The Shadow (never its fence) or a poor bot the band recruits: every
  `RecruitEveryMs` (15 min) the poorest fighter under `PoorBelow` (150gp) with `RecruitLeastSkills` (250 points) of skill, not a
  guild's head, while The Shadow stands. At most one thief per `ThiefPerBots` (20) bots, `PerHour` (6) tries across the island and
  `EveryMs` (15 min) per thief. The thief walks to the nearest town's bank, picks the fattest purse within 8 tiles, joins the
  engine's thieves' guild, puts its weapon away and uses the skill. The engine decides everything: what is taken, how much of a
  stack, and whether anybody noticed (a thief at nought is noticed almost always). Noticed is a criminal act, and in a guarded town
  with a banker or shopkeeper near the engine sends its guards: that is the risk. The mark cries "Thief!" and reports; a bystander
  who saw it adds its word; **two reports post the thief**. A theft nobody saw is found later half the time ("My purse!").
- **Ambush** (`BotHighway`). One party at a time, every `EveryMs` (20 min): a carrier on a delivery, a trader's haul or a
  caravan (life18a) with goods worth the bar by the market: `LeastCargo` (100gp) until 40 carriers out of town have been
  measured, then the worth the top `CargoShare` (25 %) of them carry, never under `CargoFloor` (40gp) — on build 416 the
  300gp bar let 73 of 1859 carriers on the roads through (4 %); nobody of ours who could fight within 12 tiles (an escort makes it no
  target), not in a guarded town; the band's 2–3 nearest within 250 tiles (The Shadow and the thieves on its roll), together no
  weaker than the carrier (`ChooseOdds` 1.0). They lie hidden on a point of the carrier's own route (the planner's) that it
  reaches after them — its road there ≥ `Lead` (1.3) × the nearest bandit's walk + `LeadSlack` (20) — for up to 4 minutes,
  strike when it comes within 12, and fight it by the engine's robbery. Every 30 s the spot is checked against the carrier's
  road still ahead (within `OnRoad`, 12): a carrier that turned has the ambush moved after it (`MostRelays`, 3), one that set
  its load down ends it. (On build 416 the spot was 24 tiles ahead on the straight line, and three of four carriers were past
  it before the band arrived.) At a quarter of its health the carrier chooses by `BotQuarter` (help near or still a match: fight on; a ward within 60
  tiles: run; otherwise throw half its pack down and be let go). The band withdraws as a company does, when what stands with the
  carrier passes `BotSquad.WithdrawOdds` (1.5) times its strength. A carrier killed is murder by the outlaws' book (red, the
  inquest, the Baron); its corpse is gone through. **A carrier that lives reports the whole party: posted at once.**
- **The fence** (`BotFencer`). Whoever holds stolen goods worth `LeastLot` (20gp) is offered the walk to The Shadow's fence, who
  pays `FenceShare` (50 %) of `BotMarket.Worth` out of its own purse and takes the goods pack to pack. The fence — or a thief with
  no fence — sells them over an NPC's counter, preferring a town none of them were stolen in. Each kind sold is traced back to the
  seller `TraceChance` (10 %) of the time, `TraceAtHome` (50 %) in the town it was stolen in; `TracesToPost` (3) traces post it.
- **The wanted list and bounty hunters** (`BotCrime`, `BotBountyBoard`). A posted bot is wanted (`BotOutlaw.Want`, the engine's
  grey, refreshed while the posting stands, `PostedMs` 6 h) with a bounty of `BountyBase` (200) plus what it is reported to have
  taken, at most `BountyMost` (2000). Any free fighter of any guild — no trader, no crafter, nobody of The Shadow — `Odds` (1.2)
  times as strong as the head and at 80 % health is offered the hunt, at most `HuntersPer` (2) on a head, priced at the bounty over
  6 minutes (a posted price: corrected by how much of such prices is earned). A wanted bot that is no murderer **yields** at a
  quarter of its health to anybody hunting it (the bounty, the law-abiding, the Baron's posse) and goes to a cell alive; brought
  down in a guarded town, by the guards or anybody, it is taken there. Either way the bounty is paid out of the town's purse
  (`BotCity.Reward`) to whoever took it.
- **The cell, the ransom and repentance.** A term is `TermMs` (30 min of uptime), doubled for every term served before, up to
  `MostDoublings` (3); a murderer serves at least the outlaws' hour. After 5 minutes its guild (not The Shadow) is asked once for a
  ransom of `RansomBase` (300) + `RansomPerMinute` (20) a minute left, paid out of its chest if the chest keeps 500 back, into the
  town's purse, and the prisoner is let out at the clock's next look. Let out unransomed, it is turned out of its guild. No guild
  takes in a thief, a wanted bot, or one out of a cell less than `RepentMs` (2 h) ago; two hours clean and it is repentant — off
  the thieves' roll, and any guild may take it in.

## A turn in a crowd that has a crowd, held once taken (07.10.2026, loops18)

On build 418's first 35 minutes 681 turns were offered and one try was made: every thief went to the nearest town's bank, Skara
Brae's for two of them, where nobody came, and 11 of the 12 drops were outbid by a peddle, an unload or a delivery at the end of the
dwell, or pressed to a delve's party. Now the banks are counted once every `CrowdLookMs` (30 s) for the island (one query a town)
and a thief is sent to the nearest bank within `CrowdReach` (400) and its leash with `LeastCrowd` (2) marks by it; none, and no
turn is offered (`Crowdless`). The wait there is `CrowdWaitMs` (60 s, was 180). The turn is `Steadfast` (`Holds`): held against
ordinary offers, put down for what will not wait and taken up again with its clocks reset; and a thief at its turn is left to it by a
company's call, a harrowing and a delve's party, as a carrier is (`BotSquads.Thieving`, `SpareThieves`). Counted in the cutpurses'
clause.

## The band mustered, not offered (07.10.2026, life19)

Build 419 (2.3 h): 3 ambushes laid, 3 recruits called beside The Shadow, 0 bandits took the call. The ambush was an offer in each
bandit's auction and lost it: Ilyrana took a delivery at 66/min "over ambush: lying in wait for Norna at 37/min", Joran a restock at
159/min over 30/min; and the spot lay 561 and 703 tiles ahead on the carrier's road, so all three carriers were "lost" 30–90 s after
the call. Now (`BotHighway.Press`, on):

- The call presses the ambush on the bandits within `PartyReach` (250) as a company's call presses the enlistment
  (`BotWill.Press`): work that resumes is put down to take up again; a thief's turn in a crowd is put down for it (the band's own
  call). Passed over and counted by why: in a company, tired, hurt (under 70 %), at a fight, answering a company or its class,
  carrying another bot's goods, summoned, on work that would be thrown away. The ambush is laid only when `LeastParty` (2) are free;
  the carrier chosen is the one a free bandit is nearest (failing that, the nearest to any bandit, so the call is made and counted).
- The deed is priced from the expected cargo (cargo × `FenceShare` ÷ party ÷ 6 min, floor `Prior` 1); it no longer decides.
- The leader waits at the spot only while two are committed: fewer and the ambush closes (`FellApart`); a bandit with the carrier in
  reach strikes only with `LeastParty` of the band within 24 tiles of it (`StrikeTogether`, `HeldStrike`).
- The spot lies at most `MostAhead` (300) tiles along the carrier's road; a carrier off its drawn road but bound for the same goal and
  short of the spot keeps the band's place (`KeptOnGoal`) instead of being given up.
- Still one party at a time, 20 minutes apart. Counted in the roads' clause: `the muster: …`.

Build 420 (70 min) laid none: 16 calls found fewer than two free; passed over: work that would be thrown away 14, in a company 7,
at a fight 4, carrying 3, hurt 2, tired 1. life19b: work reckoned under `ThrowBelow` (40gp/min) that does not resume is thrown away
for the call (counted by kind); and `HireHands` adds poor (under `BotCrime.PoorBelow`) greedy or reckless fighters of a thief's
classes within reach, never set on a carrier of their own guild or an ally's (counted as hired hands; robbing outcasts them by
`BotUnderworld.Robbed`, as for any recruit).

## Honest money

A theft moves coin pack to pack by the engine's own skill. The fence pays out of its purse. A bounty comes out of the city's
treasury and a ransom goes into it. The only new coin is a stolen thing sold over an NPC's counter — the one way this shard has
always minted it. Nothing is handed out.

## Traps

- **A thief at nought steals a coin and is caught.** The engine limits a stack to Stealing/10 over the weight (a coin weighs 0.02:
  five coins a point) and notices a thief unless Stealing beats a roll of 150. The thieves start at what their class gave them;
  they learn at the engine's rate. `PerHour` is the bound on the deaths this costs, and the ledger will price the work at what it
  pays: if theft pays nothing, thieves stop choosing it.
- **The posting keeps a bot grey for six hours.** Grey is what lets the law-abiding and the bounty hunters set on it, and what
  makes a guarded town kill it. `PostedMs` is the dial.
- **One ambush at a time, and a party that dies holds the slot** until its 15-minute cap (`CapMs`), because a dead bandit's deed
  may never report its end.
- **The stolen-goods book is in memory only.** A restart cools everything; the goods stay in the packs as ordinary goods.

## How to measure

`Underworld:` ends with `crime: …`: thieves at work against the bound, tries, lifts and what they came to, noticed and witnessed;
the roads' carriers seen, escorted, laid, sprung, the carrier's choices, withdrawals, deaths both sides and the take; the fence's
buys and sales and traces; reports, postings, bounties offered, taken and paid; catches by kind, terms, ransoms, releases, the
turned-out, the repentant and the relapsed; criminal deaths. Per-event lines: `reports … to`, `posts … as wanted`, `lifted …
from …'s pack`, `lays an ambush for`, `springs the ambush`, `The ambush on … is over`, `sold … stolen`, `takes the bounty`,
`yields to`, `is caught by … serves`, `pays … to ransom`, `is let out after`, `is repentant`. Watch deaths per bot-hour (≤ 0.5)
and the work share against the build before.
