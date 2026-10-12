# World / life features over two runs (analysis of 12.10.2026, read-only)

Runs: R1 = session-2026-10-08_23-55.log (08.10 23:55 -> 10.10 14:40 = 38.75 h; "d1" = 09.10, "d2" = 10.10), build 432.
R2 = session-2026-10-11_03-52.log (11.10 03:52 -> 14:07 = 10.25 h). Neither run reached a second 20:00 championship
(R1 reached the 09.10 one only; R2 stopped before 20:00).
Method: figures marked (S) are the shard's own cumulative counters from the last 5-minute summary line of the run
(Bot shops / Events / Underworld / Guilds / Estate / Wars / Sea / Relics / Stewards / Zones / Tourney lines); the rest are
counts of per-event log lines. Rates are per hour of the run. Samples are shortened.

## 0. Verdict table

| Feature | Verdict | One-line reason |
|---|---|---|
| Taverns | WORKING (small) | 40 shifts/h, ~2.5 meals a shift |
| Moneylender | WORKING (tiny) | 2 loans/h, 3 % default, 2.8k gp in 39 h |
| Invasions + militia | WORKING | 14 raids, 9 repelled, 13.8k gp heads |
| Fair / lottery | WORKING (price effect); fairgoers WEAK | 0-8 bots walk to a fair |
| Caravans | WORKING; their guards WEAK | 85 % of guard jobs fail on one reach defect |
| Rumours | WORKING | 31 chests found of 39 |
| Elections / plots / councils | WORKING | 84 elections, 3 deposed heads |
| Town charters | WORKING | 15 held, 4.8k levies |
| Claims / wars / guild changes | WORKING (slow) | wars only over seized squares |
| Law: catches, jail, ransom | WORKING | 27 caught, 13.9k gp ransoms |
| The Shadow | WORKING after a 24 h delay | founded 10.10 00:11 |
| Relics + banners | WORKING (self-limiting) | all 5 relics home after 3 lairs |
| Championship (arenas) | WORKING, 1 of 1 | held 09.10 20:00, 50 entrants |
| Bot shops | WEAK -> better | mean 6.1 of 24 open (was 3), 8004 sales |
| Stewards | WEAK | 3-5 of 14 towns, hard cap 5 |
| Ragman | WEAK | 24 % of posts buy anything |
| Cutpurses at banks | WEAK | 7 lifts in 39 tries, ~73 gp total |
| Bounty hunters | WEAK | 11 hunts taken of 106 offered |
| Duels (grudge) | WEAK | ~29 in 39 h (22 of the 51 are the championship's sparrings) |
| Quartermasters | WEAK | 46 draws taken of 944 offered |
| Herald bouts / scouts | WEAK | 2 bouts decided of 50 challenges |
| Ambushes on carriers | WEAK | 19 laid, 3 sprung, 0 gp taken |
| Infirmaries | DEAD | 21 opened, 0 healed, 0 raised |
| Libraries | DEAD | 10 opened, 0 scrolls sold |
| Herald recruiting | DEAD | 0 invited (10 020 looks) |
| Sea voyages / colonies / pirates | DEAD | 0 voyages, 0 expeditions |
| Ultimatums / tribute | DEAD | 0 sent |
| War capitulation | DEAD | 0 |
| Fence, relic theft | DEAD | 0 walks, 0 tries |
| Fair-called championship | DEAD by design | 9 refused by its hour |

## 1. Working

### Taverns (BotTaverns, BotTrades/BotTavern.cs)
- R1: 1556 opened (40/h), 3825 meals for 15 101 gp (99 meals/h) at 57 % sell-through, 3696 guests, 84 tired bots sent to an inn, 129 ate before bed. R2: 439 opened (43/h), 1286 meals / 6118 gp.
- Weak spots inside a working feature: 793 of 1552 shifts shut "when the cook was taken away"; of 1.45 M supper asks only 4332 went (699 k had no tavern within 100 tiles).
- `[23:56:17] Nimue Silverleaf opened a tavern at the inn of Britain (1489, 1688) with 13 meals, Ribs at 6gp...`

### Moneylender (BotLoans, BotTrades/BotLoans.cs)
- R1: 80 loans / 2827 gp (2.1/h), 79 repaid, 1 default (3 %), interest 574 gp. R2: 11 loans / 386 gp, 9 repaid, 1 default.
- Only poor bots short of a kit qualify: of 1.59 M looks, 1.36 M were "not poor (over 100gp)"; 1122 sent, 89 went. Working as designed, volume negligible.
- `[13:08:43] Galadrion lent Aelith Moonshadow the Archer 30gp at 20 % ... a default after 4h of its play`

### Invasions + militia (BotEvents/BotInvasion.cs)
- R1 (S): 14 called (0.36/h), 11 answered by a militia, 3 not; 31 waves, 216 creatures, 201 killed by ours; ended 9 repelled, 1 militia left; 11 of ours fell; 13 840 gp paid by the head. R2: 4 called, 3 answered, 8 waves, 57 killed, 2 of ours fell.
- Militia raised 24 / let go 23 (R1). 
- `[00:40:18] An invasion out of Terathan Keep is called on Britain's road at (1407, 1471)... 29 free fighters within 120 tiles bring 39919`

### Fair + lottery (BotEvents/BotFair.cs)
- R1: 9 fairs held (6 Great Fairs at 80 %), 1.7 M counter prices discounted, 3195 rebates / 32 444 gp; lottery 853 tickets, 8 draws, 7950 gp paid. R2: 3 fairs, 2 Great, 687 rebates, 3 draws.
- Fairgoers are weak: closing lines say "0-8 came" (8 fairs: 2,0,2,3,1,8,1,0); 1189 walks offered to traders/crafters, 17 taken (S).
- `[03:39:49] The fair in Vesper closes (its hours are over): 2 came for it, 59 tickets sold this fair`

### Caravans (BotEvents/BotCaravans.cs)
- R1: 66 posted (1.7/h), 55 led / set out, 52 arrived, 2 lost; 5500 gp premiums, 5346 gp wages. R2: 22 posted, 18 set out, 16 arrived, 2 lost.
- `[00:23:04] Naevys Silverleaf's caravan reached Trinsic after 6 minutes: 10 of 10 MandrakeRoot onto Ayila's shelf`
- Guards = WEAK: R1 167 hired, 16 finished, 150 failed, of which 128 "could not get nearer to <trader> at (x, y, 29-30) in Britain" (a floor 30 high, e.g. (1484,1532,30)); 15 "fell behind". R2: 50 hired, 8 finished, 41 of 42 failures are the same reach failure. The trader stands where a guard cannot walk to.

### Rumours (BotEvents/BotRumour.cs)
- R1: 39 spread (1.0/h), 45 bots went, 31 chests found (9300 gp), 9 lapsed (2605 gp back). R2: 10 / 6 / 1800 gp.
- `[14:16:52] Ulaine found the rumoured chest 153 tiles east of Trinsic first and took 300gp`

### Elections, plots, councils (BotPolitics/BotElection.cs, BotCouncil.cs)
- R1 (S): 84 elections (30 for a term, 51 for a head fallen, 3 after no confidence), 26 new heads, 58 re-elected, 2658 votes; plots: 32 looks found discontent, 3 votes carried (e.g. `The Mill has deposed Hilde: 34 of 34 voted no confidence`). 12 councils, 88 tax-collector payments. R2: 20 elections, 1 plot carried.

### Town charters (BotCharters, BotPopulation/BotCharter.cs)
- R1 (S): 15 held; 237 auction weighings, 66 sold, 216 bids / 93 718 gp, 46 262 gp into the towns' purses; 4821 levies = 103 226 gp; 305 "is charged" task lines, 256 board requests. R2: 13 held, 15 sold, 1847 levies / 71 217 gp.

### Law: catches, jail, ransom (BotCrime / BotOutlaw)
- Crime active only from 10.10 00:11 (14.5 h) in R1. R1 (S): 50 reports, 27 posted, 27 caught (20 brought down in town, 7 yielded), 27 terms = 1470 min (10 doubled for a repeat), 14 ransoms paid = 13 920 gp into the purse, 6 unpaid, 25 let out (5 turned out of their guilds), 17 repented, 7 relapsed, 87 criminals died. R2: 35 reports, 20 caught, 13 ransoms = 11 340 gp.
- `[09:33:27] Yorick was caught by Uldreth and is sentenced to cell 0 at (5276, 1164) for 60 minutes`
- Net: the law collects ~13.9 k gp of ransoms against ~73 gp ever stolen.

### The Shadow (BotHunt/BotUnderworld.cs)
- Founding needs 2 bots that "killed from hiding" (Founders = 2). Only 1 existed after 24 h of 09.10 (Yorick, ~10:00), the second came 10.10 00:11 -> `[00:11:39] The Shadow is founded by Elowen (Hiding 48.2...), Yorick (Hiding 41.8...)`. So 09.10 had 0 crime events of any kind (pickpocket, report, ambush, jail all start d2).
- After that: members Elowen, Yorick + fence Dalla the Sage (R2 also Annora); 4-6 camps pitched/burned; hideouts: 5 given away / 4 burned (R1), 6 / 6 (R2); raids on the hideout 4 (10 fighters pressed, 3 members found) / 7 (11, 6); 11 thieves of 11 allowed, 27 recruited. It survives a restart (R2: "1 taken in, 3 put back at a boot", founded 0 times) - so R2 had it from minute one.

### Relics + banners (BotRelics/BotRelicLair.cs)
- R1 (S): 5 town relics home (0 hidden), 10 banners; lairs 3 laid -> 3 cleared (5 captains took a call, 4 companies), 3 wardens killed, 2 relics carried home; war: 5 banners + 3 relics taken, 5 grudges; banners bought back 4 for 6000 gp; 8127 rebates at relics' towns / 44 009 gp; 6 Great Fairs called. R2: 0 lairs ("3 times no hidden relic"), 2 banners + 1 relic taken, 1 bought back (1500 gp).
- Self-limiting: once the 5 relics are home no lair can be laid; further lairs need a relic lost in war or to a thief.
- `[00:35:23] Wilma raises a company for the Lantern of Vesper in its lair 124 tiles west of Vesper`

### Championship at the arenas (mindedBots/debugger/BotTourney.cs)
- 09.10 20:00: held (1 of 1 possible). 50 entrants (22 crafters), 25 sparrings in round 1 then 8, 4, 2, 1 (5 rounds), 22 bouts fought (17 yields, 5 on the clock), 18 walkovers, 23 ported, 27 "with no reserve" (bots at work that cannot be put down). Winner Cerys Ashdown (Warrior), took the title 20:29 because the champion Aerith (title since 08.10 20:32) "did not come to defend (resting)" - 0 title bouts, 0 defences; prize 500 gp.
- `[20:29:07] Tourney: Cerys Ashdown the Warrior is the champion of the shard: took the title from Aerith, who did not come to defend it (resting)`
- Open: the 08.10 finding "mages without reagents between bouts" is NOT fixed: `Heal()` (BotTourney.cs:1873-1888) restores health, stamina, mana and poison only; reagents are given only in a prize pack (`Supplies`, lines 1345-1374).
- Fair-called championship: dead (see section 3).

### Claims, wars, guild changes (BotEstate/BotClaim.cs, BotWar.cs, BotGuilds)
- Claims R1 (S): 185 made (4.8/h), 151 won, 3 only struck a name off, 19 never gathered 3, 14 driven off; 29 squares fell for no longer reaching their hall; 1.12 M gp paid for ground (cumulative with saves). R2: 83 made (8.1/h), 62 won, 17 never gathered, 0 fell.
- Wars R1 (S): 5 declared (0.13/h), 6 finished (one carried over from the save), all "won by blood" at 25 dead after 40, 8, 12, 35, 18, 14 min; 9 became big wars (11-21 a side); all 5 declared were claims on a held square ("over seizing"). 0 by plunder, 0 clock, 0 peace. R2: 3 declared, 2 by blood (27, 39 min), 1 ended in peace after 15 min "nobody ahead, 4 to 0". Wolf/Mill/Blade/Hammer in R1, Ash/Crown and Mill/Blade in R2.
- Guilds: R1 started with 9 guilds / 289 bots, ended 10 / 285; 0 founded (founding was blocked: 286 looks "fewer than 4 others without a guild"), 1 split (The Moon from The Blade 10.10 13:49), 0 closed, 16 members left a guild with "nothing to show", 3 joined. R2: 10 guilds; The Needle closed 06:25 (1 member, 151 min, chest 2079 gp), The Chalice split from The Hammer the same minute, 4 joined, 0 founded ("3568 looks found 10 guilds standing" = at the cap).

### Danger-zone accuracy (BotZones, Zones line, window 6 h)
- Attacker deaths landing in painted zones (clean-ground misses = the rest): R1 97.7-98.9 % (278/282 at the end, i.e. 4 misses), R2 99.0-99.4 %. Visits by the watchers to empty "danger": R1 97.5-99.5 % (393/395 at the end, 2 misses), R2 96.6 % at the first look then 99.3-99.8 %. Combined R1 97.8-99.1 %, R2 97.5-99.4 %.
- Last-24 h deaths by ground (R1): 1610 deadly, 85 hostile, 147 fringe, 157 on clear ground (8 %); but 580 of 4064 deaths were not by an attacking creature (war, duel, ambush), which is where most "clear ground" deaths come from. Fringe deaths of empty zones: 2 in R1, 0 in R2. Painted 2.56 M patrol tiles.

## 2. Weak

### Bot shops (BotShopkeep) - improved, still under half
- Open shops (sampled every 5 min): R1 mean 6.1 of 24 (max 16, 17 samples at 0-1); R2 mean 7.5 (max 16). The 08.10 figure was 3. R1 (S): 1874 shifts opened, 8004 sales / 351 564 things / 1.0 M gp, 2424 sales to bots short of a kit; the city bought 332 k things for 933 k gp at them. 1.04 M of 1.65 M looks were fighters, who keep no shop.

### Stewards + decrees (BotPolitics/BotStewardship.cs)
- Towns with a steward: R1 3 (44 samples), 4 (155), 5 (229) of 14; R2 4 (84), 5 (29). Never above 5 - by construction: the steward is the head of the guild with best standing in the town, and guilds exist in only 5 towns (Travel line: Britain 3, Minoc 1, Trinsic 1, Vesper 1, Yew 2, all others 0).
- R1 (S): 15 chosen, 7 deposed (3 empty coffer, 4 falling regard; 08.10 had 8 empty-chest removals); 672 rounds, 519 with nothing lawful to decree; 132 decrees: 49 caravans, 43 tax cuts, 36 tax raises, 3 holidays, 1 militia, 0 captains, 0 bounties, 0 bars (model 98, rule 34, Argus 0). R2: 34 decrees (17 caravans, 9 raised, 7 cut, 1 captain), 3 deposed. Coffers 2201 gp held (R1), 3167 (R2).
- `WRN deposes its steward X of G: its coffer stood under Ngp for N minutes` (7 in R1, 3 in R2).

### Ragman (BotRagmen, BotTrades/BotRagman.cs)
- R1: 243 posts opened (6.3/h), 58 bought anything (24 %); 2770 things from 90 hunters for 6649 gp (worth 10 678 gp at counters); 0 killed. R2: 70 posts, 13 productive, 701 things / 2436 gp. 173 of 242 shifts ended "at the end of the shift".
- Reason: `BotRagSeller.Propose` only serves a hunter who is out of a company/combat/delve (most fight in companies), within 120 tiles, carrying >= 40 gp of loot (3158 carried less), and the post must stand 60 tiles nearer than a counter (391 failed that); 837 sent, 103 went. Post also pays only 60 % (`Share`).
- `[14:40:05] Ilphrin shut Ilphrin's buying post... the shift is over: bought 0 things from 0 hunters for 0gp`

### Cutpurses at banks (BotCrime/BotCutpurse.cs; log tag BotPickpocket)
- R1 (14.5 h active): 39 tries (cap 6/h), 7 lifted 24 gp + 6 things worth 49 gp, 17 empty, 23 noticed by the mark + 21 by a bystander, 16 refused by the engine before a roll. R2: 39 tries, 11 lifted 0 gp + 15 things (53 gp), 2 thefts found later.
- Reason: 9070 looks found "no bank within 400 tiles with 2 in its crowd"; thieves have Stealing 3.6-8.5 (`lifted 12gp ... at Stealing 3.6, and was seen`).
- `[00:13:20] Yorick the Mage lifted 12gp from Greta Ashdown's pack by the bank in Britain at Stealing 3.6, and was seen`

### Ambushes on carriers (BotCrime / BotUnderworld)
- R1: 19 laid, 41 bandits answered, 3 sprung, 7 carriers lost (left the road), 0 gp / 0 things taken, 3 withdrawals at the odds. R2: 7 laid, 2 sprung, 2 bandits died, 0 taken. 431 calls with fewer than 2 free bandits in 250 tiles.
- Bandit stock of loot is zero, so the fence (0 walks, 0 lots) and the traced-seller path (0) never run.
- `The ambush on Aelrindel near Britain is over: Aelrindel left the road the band lay on, and the band lost it`

### Bounty hunters (BotCrime/BotBounty.cs)
- R1: 27 posted wanted, 6787 looks by free fighters, 106 hunts offered, 11 taken (560 heads too strong, 208 already hunted), 7 bounties paid = 1400 gp. R2: 141 offered, 10 taken, 6 paid = 1200 gp. A patrol was raised only twice.
- `[01:39:18] Orabel the Mage takes the bounty of 200gp on Mirthal, wanted in Britain`

### Duels (BotCombat/BotDuel.cs)
- R1: 51 duels, of which 22 are the championship's sparrings (20:xx) -> ~29 grudge duels (0.75/h); 38 ended by a yield at 35 %, 13 by the clock, 0 deaths. R2: 9. Character line (S): 403 challenges -> 27 duels; refusals: challenger in a company 172, challenger duelling already 109, challenger hurt 26.

### Quartermasters (BotOfficers/BotQuartermaster.cs)
- R1 (S): 33 officer posts held, 67 appointed; quartermasters 101 shifts (6 min each), 45 wants posted for 672 units, 10 166 gp of takings carried into chests; BUT hand-outs: 944 draws offered, 46 taken (4.9 %), 44 kits of 176 units / 1429 gp; R2: 61 offered, 0 taken, 0 kits.
- Reason: `BotKitDraw` (BotQuartermaster.cs:883) - Reach 100 tiles ("too far from the open store" = 32 463 passes), and the store is open ShiftMinutes = 6 per RestMs 40 min at most (606 min total open across 9 guilds in 2325 min, ~3 % of guild-time); "able to buy its own" 5027.

### Guild scouts and herald bouts (BotOfficers/BotGuildScout.cs, BotGuildBout.cs)
- Scouts R1: patrols 2871 offered, 140 zones watched, 142 attackers sighted, 10 death-map squares knocked down; shadows 1456 offered, 17 reported; rumours 3 offered, 0 chests reached first. Most passes: "no hostile zone within reach of the seat" 12 969, "road to the lookout too dangerous" 2122.
- Herald bouts R1: 50 challenges, 10 accepted, 2 begun, 2 decided (220 gp), 8 called off; refusals "no champion free near the stand" 33, "chest short of the purse" 7. R2: 17 / 3 / 1 / 1.

## 3. Dead

### Infirmaries (BotInfirmaries, BotTrades/BotInfirmary.cs)
- R1: 21 opened on demand (all for a wound, 0.54/h), 0 patients healed, 0 raised, 0 gp in fees, each shut after 3-4 min "nobody hurt past mending". R2: 1 opened. Resurrections by infirmary: 0 in both.
- 3912 deaths judged: 3326 "too far from the seat to call" (>150 tiles), 333 near the seat with no able healer, 78 left to their company, 134 delving. Raisers (S): "0 of 9 guilds have a Healer able to raise now (0 of 14: 0 by scroll, 0 by book, 0 by 80/80 bandage; 12 have Magery 50+, 0 have 80 Healing and 80 Anatomy)". Company raising is the same wall: 38 raised of 2703 company deaths (1.4 %; 1879 "nobody able"). R2: 2 of 462.
- Reason: able = resurrection scroll at Magery 50 / book at 70 with herbs / 80+80 bandage; no healer holds a scroll or the skills, and the fallen are far from any seat (BotInfirmary.cs 150-tile reach).
- `[00:08:41] Elenwe Moonshadow shut The Lantern's infirmary after 3.0 minutes - ... 0 patients healed, 0 raised where they fell, 0gp in fees`

### Libraries (BotLibraries, BotTrades/BotLibrary.cs)
- 10 opened / 10 shut in each run, 0 scrolls sold, 0 gp. R1: all within 05:25-06:59 on 10.10 by three scribes (Quelindor x5 and Rhiannel x5 at The Wolf, Sylwen x1 at The Anchor), each shift 0.7-2.3 min, re-opened in bursts of 5 within 15 minutes. R2: Faelar x8 at The Anchor 04:49-05:03 and Ysolde x2 at The Mill 13:32-13:33.
- Reason: the scribe's library work is priced 2-6/min (took on 5 times in two runs) and "pressed to enlist" by a company call pauses it within a minute (`Paused` -> "the scribe was called away", BotLibrary.cs:872); gates: 3261 scribes held under 3 spare scrolls the casters lack (LeastScrolls = 3), 37 991 were over 150 tiles from the seat, 9342 in no guild; buyers: 3 casters sent, 0 went.
- `[05:26:40] Quelindor shut Quelindor's library at The Wolf's hall after 0.7 minutes - the scribe was called away: 0 scrolls sold to 0 casters for 0gp`

### Herald recruiting (BotOfficers/BotHeraldStand.cs)
- R1 (S): 77 stands (10 min each), 10 020 looks, 0 invited, 0 joined, 0 crossed; 60 pieces of news read. R2: 19 stands, 2687 looks, 0 invited.
- Reason: `Invitable` (lines 410-470): nobody is unguilded (285 of 287 in a guild; 9-10 guilds at the cap), poaching needs a member sour on its head ("content in its guild" 5138), and the look reaches only `Reach` = 14 tiles round the bank stand ("far from the stand" 12 605; "no bank near the seat" 2736).

### Sea, colonies, pirates (BotSea, BotColonies/BotColonies.cs)
- Both runs: 0 voyages, 0 ships bought, 0 expeditions offered, 0 colonies, pirates "no loaded ship has sailed"; 194 247 travellers asked (R1), all "holding no ship"; expeditions: 1137 guilds weighed, 1137 "with no island free" (R2: 411/411).
- Reasons: (a) BotSea.cs:75-90 - a journey pays nothing, "no voyage here pays a ship back", so nobody buys a ship; (b) `BotColonies.SiteOn` (line ~543) finds no square within 5 rings of the dock that is off town ground, unclaimed, walkable from the shore and has >= 4 sampled rock tiles: `Colonies: Nujel'm's best site is nowhere` (150 lines R1, 40 R2).

### Ultimatums / tribute (BotPolitics/BotTribute.cs) and capitulation (BotEstate/BotWar.cs)
- Ultimatums: 0 sent, 0 paid, 0 refused in both runs. `Demand` fires only when the would-be attacker has >= 2.0 x (Odds) the defender's might; guild mights ran 1.1-1.9 x (e.g. 138 156 vs 72 223 = 1.91).
- Capitulation: 0 in both runs. Needs the enemy at >= 1.5 x (YieldMight) AND (8 kills behind and 3 x its score, or < 1/5 the might after 15 min). Wars were between ~equals (1.1-1.3 x) and ended at 25 dead in 8-40 min; a 25-0 rout in 8 min did not capitulate because the might half failed.

### Fence and relic theft (BotHunt/BotFence.cs, BotRelics)
- Fence: Dalla seen with the band 3-4 times, 0 walks to the fence, 0 lots bought (nothing to sell). Relic thieves: 0 offered, 0 tries (needs Stealing 30+; thieves have < 9).

### Fair-called championship
- `championships at the fair: 0 called, 9 refused by its hour` (R2 3); the fair only logs "the championship is held once a day at the arenas east of Papua, by the calendar from 20:00 ... the fair at Vesper does not call it". Harmless; the calendar path works.

## 4. Debuggers (bot-debugger.log, R1 lines 98847-139944, R2 139945-154104)
- (S) R1: Argus 29 findings + 163 echoes turned away; Lynceus 38 + 155; Hermes 231 asked, 0 findings (witnesses meetings: 499 witnessed, 498 model words at 5.4 s); Heimdall 0 asked (zones keeper). R2: Argus 19 + 32 echoes, Lynceus 16 + 36; 123 meetings witnessed.
- FINDING lines in the log: R1 67 filed (38 Lynceus stuck, 27 Argus stuck, 1 waste, 1 loop), 318 "repeated ... word for word, not filed" -> 83 % duplicate; R2 35 filed, 68 echoes (66 %). The remembered store is unchanged: "40 findings remembered, 12 standing conclusions, 11 unanswered questions" at the end of both runs.
- Useful content: ~zero new. The filed "stuck" findings cover 13 bots in R1 (Lorandil 73 of 174 stuck-lines, Liftrasa 23, Lorelei 19, Nymarel 17) and 6 in R2; the recurring remembered ones are the September loops (peddle 63, restock 63, homeward 49, drill-in 36, prowl 30, "two-tile loop" 44/42). None of the 67+35 findings mentions taverns, infirmaries, libraries, ragmen, loans, caravans, The Shadow, stewards, heralds, colonies or relics. The model is deepseek-r1:14b.
- Zone keeper Heimdall works: see accuracy above (tour round 18, 6533 stops, 1848 held).

## 5. ERR / FTL
- FTL: 0 in both runs. ERR: R1 943, R2 282. WRN: 2172 / 582 (mostly notices that use WRN: BotParley 764 "calls a meeting", BotClaim 369, BotBarred 404 "ground closed - a killing field", BotBurgh 268, BotUnload 251, BotWar 26, BotSeizure 25, BotStewardship 7, BotCharters 4).
- ERR groups: BotStall 469 / 133 ("has not moved or changed what it is doing", 386 / 72 of them with work "nothing"; 39 are Cerys the Baron); "Attempted to start timer ... while world is Saving" 291 / 75, always from `BaseHouse.Serialize` of SmallOldHouse / SandStonePatio (the bots' / guild houses, engine side, once per ~3 saves); BotPopulation 131 / 35 ("could get nowhere at all ... carried home", the Baron on TooBig ground); BotArms 41 / 28 ("fighting bare-handed and has nothing in its pack to put on").
- Tied to the features studied: only single BotStall lines whose work was "called to the bout of The Hammer against The Wolf" (x2, herald bout), "off to the hideout, wanted" (x1, crime), "on the way to stand by Jasper in its pay" (x1). No ERR from taverns, infirmaries, libraries, ragmen, loans, caravans, rumours, politics, relics, sea.

## 6. What the numbers say to do next (ranked by size of the gap)
1. Caravan guards: 128 of 150 fails are one reach defect - the trader is at z=29-30 in Britain; fix the destination/standing tile, not the pay.
2. Infirmary + company raising: no healer can resurrect (0 able of 14-17); either give healers a scroll/book path or drop the role; reach 150 tiles to the seat leaves 85 % of deaths uncovered.
3. Libraries: price (2-6/min) can never beat enlist; either make the shift non-pausable by a company call or retire it.
4. Herald recruiting: no unguilded bots exist; change purpose (poach by war/regard) or Reach 14 -> bank crowd.
5. Colonies / voyages: SiteOn finds no rock site on any island and ships do not pay; both gates must change together.
6. The Shadow took 24 h to found; lowering Founders or crediting hidden kills sooner would give crime on day one (it persists across restarts, so only a wiped world is affected).
7. Arena: add a reagent top-up next to `Heal()`; the fair's championship request is dead code by design.
8. Stewards: structural cap of 5 towns while guilds sit in only 5; decide whether the other 9 towns get a non-guild steward.
