# Work / economy analysis of the two last runs (12.10.2026, read-only)

Logs: **L1** `session-2026-10-08_23-55.log` (08.10 23:55 -> 10.10 14:40, 38.7 h, 1.77 M lines, 464 summaries, 9 749 bot-hours)
and **L2** `session-2026-10-11_03-52.log` (11.10 03:52 -> 14:07, 10.2 h, 0.50 M lines, 122 summaries, 2 776 bot-hours). Build 432 in both.
Method: summary counters are cumulative, so every rate below is a difference of two summaries (windows of 2 h or 30 min); "early / mid /
late" = first 8 h / middle / last 14 h for L1 and first / second 5 h for L2. Deaths = `Kept: ... fell at` lines (4 064 / 748; they agree
with `The island: ... 740 deaths reckoned`). Scratch data: `scratchpad/L1`, `scratchpad/L2` (extracted summary lines, fails.tsv, kept.tsv).

## 0. Headline table (cumulative, with the 08.10 baseline)

| measure | 08.10 baseline | L1 (38.7 h) | L2 (10.2 h) | goal |
|---|---|---|---|---|
| work finished (`Resolve:`) | 92.7-93.1 % | **92.4 %** (399 930 endings) | **91.1 %** (109 012); 92.0 % first 5 h, **89.8 % last 5 h** | 95 % |
| ... failed / dropped / died | - | 4.2 / 2.5 / 0.9 % | 5.2 / 3.1 / 0.6 % | |
| Needs (`Lines:` first-hour share) | 93.9-96.3 % | **95.8 %** (40 887 of 42 663) | **97.3 %** (13 491 of 13 872) | 99 % |
| paths (`Getting about:` reached / searches) | 99.78 % (per-purpose, not comparable) | 95.5 % flat (94.6-96.2) | 94.0 %, **95.7 -> 89.9 % in 10 h** | - |
| deaths per bot-hour | 0.56-0.78 | **0.41** (0.72 in h 0-2 -> 0.16-0.25 on day 2) | **0.27** (0.14-0.30, then 0.35-0.39 in war hours) | down |
| median wealth (`Money: middling`) | 141-252 gp | 271 -> 861 gp (x3.2) | 827 -> 803 gp (flat) | up |
| Orc Caves field share of deaths | 16.5 % | 15.3 % (18.0 % mid, 12.6 % late) | 9.8 % (73 of 748) | down |
| delve finishing | 34 % | 71 % (718 endings) | 88 % (224) | up |
| loop | 20.8 ms/look | 19.5-26.0 (21.0 end) | 22.6 (24.4 at start) | <27 |

## 1. Population over time (`Rest:`)

By design the in-world count follows bands: 00-08 100 % = 310, 08-16 75 % = 233, 16-24 50 % = 155 (+ "past their hours" floor).
L1 average 252 (155-310): 310 at 02-06, 233 at 09-15, **155-167 at 17-23 on 09.10**, back to 295-310 from 00:00 on 10.10.
L2 average 273: 310 until 07:30, 233-254 from 08:00, never reached the 155 band (ends 14:07). All 310 bots stayed on the roll (0 births, 0 wipes).
Throughput per bot-hour is not constant: **42.4 endings/bot-h in the 100 % band vs 35.0 in the 75 % band** (L2: prowl 4.1 -> 2.1,
enlist 4.6 -> 3.1, inscribe 2.0 -> 1.1 per bot-hour) - fewer, longer pieces of work in the day band; this alone moves the finish share
because failures do not scale down (section 2).

## 2. Work finished

Window shares (L1, 2 h steps): 90.7-93.4 % with no trend (mid 92.4 %, late 92.1 %). L2, 30 min steps: 91.3-93.1 % until 08:30, then
**88.2-91.5 %** (88.3 % at 08:57, 88.2 % at 10:27, 88.4 % at 11:57, 88.6 % at 12:57, 88.2 % at 13:27). Per half hour L2 had **215-423 failures (median ~275) and 160-250 drops at all
hours** while endings fell from ~6.5 k to ~4 k: the failures are a fixed-rate background (section 6, D1) plus war windows (D2).

Top 10 failure groups (`failed at`, names/coordinates stripped; segs = thirds of the run):

| # | L2 kind | reason | n | % of 5 920 | segs | L1 n (% of 17 473) |
|---|---|---|---|---|---|---|
| 1 | deliver | could not get nearer to <buyer> at (P) [in town] | 585 | 9.9 | 259/163/163 | 1 313 (7.5), thirds 369/414/530 (rising) |
| 2 | unload | still too heavy to walk at 233 of 222 stones | 242 | 4.1 | 67/78/97 | 155 |
| 3 | prowl | could not take a step towards (P) | 231 | 3.9 | 58/76/97 | - |
| 4 | unload | too heavy to walk, nothing the market/ground would take | 228 | 3.9 | 58/76/94 | 110 |
| 5 | enlist | no way through to (P) / to <creature> in <town> | 312 | 5.3 | 110/128/74 | 482 (2.8) |
| 6 | unload | no way through to (P) | 151 | 2.6 | 49/51/51 | 249 |
| 7 | supper | <table> at the inn of <town> shut before it got there | 132 | 2.2 | 54/39/39 | 539 (3.1) |
| 8 | enlist | could not close on company N: N tiles off after 8 walks | 116 | 2.0 | 40/31/45 | 392 (2.2) |
| 9 | prowl | could not raise enough strength for (P): N of the N it asks | 109 | 1.8 | 37/41/31 | 444 (2.5) |
| 10 | weave | no sheep with its wool grown within N tiles | 100 | 1.7 | 32/35/33 | - |

Next: meet "gave the delivery up before the meeting" 94 (L1 310), restock "no way through to X" 93, forage "nothing left lying about" 86,
chop "no way through" 83, rally "the war company had no room by the time it arrived" 57 (wars only), L1 also mine "turned back: an orcish mage" 193.
Hunt fails (601 / 2 110) are 71-73 % one reason: **"<creature> would not go down: 100 % of it left and not a scratch in 45 s"**, 96 % of them
with Bow (77 %) or Crossbow (19 %) in hand, line of sight True, 88 % from 3+ tiles; 101-116 bots; ~40 per hour in both runs.

Endings by trade (finish %, loss = fail+drop+died, L2): enlist 91.8 % (906 lost, incl. 320 deaths), hunt 84.3 % (899), unload 77.2 % (830),
deliver 95.2 % (765), rescue 76.0 % (587), prowl 94.2 %, skulk 73.4 %, restock 94.1 %, flee 52.1 %, mend 79.3 %, homeward 62.1 %,
guard-duty 53.0 %, tavern 52.5 %, **rally 34.6 %** (309 endings, 85 died), band 70.1 %, stake 68.0 %, delve 87.9 %, harrow 72.1 %.
L1: enlist 92.3 % (3 500 lost, 2 120 deaths), hunt 83.9 %, rescue 73.9 %, mend 73.4 %, flee 55.3 %, delve 71.4 %.

Drops (4 332 / 13 672): **"not taken up again after enlist: it was put down for N minutes" 1 461 (33.7 %) + "pressed to enlist: called to a
party/company" 963 (22.2 %) = 56 % of all drops** (L1 49 %); outbid 5 %, "jumped by meet" 6 %, no reason text 19 %. Biggest droppers: skulk 465,
hunt 265, inscribe 255, homeward 244, supply 232, rescue 223, restock 223, guard-duty 202.

Bots: Baern the Tamer alone is **1 035 of 5 920 failures (17.5 %)** in L2 (384 of 17 473, 2.2 %, in L1 where it started at 02:44 on 10.10).
Top-10 failing bots hold 24.1 % in L2 (8.7 % in L1).

## 3. Needs (`Lines:`) - first-hour share

L1 cumulative 95.8 % (windows 92.5-97.9 %; no trend: 95.5 early, 96.3 late). L2 97.3 % (98.0-98.3 % for 4 h, 96.7 % at 07:57, **92.7 % at
08:57** when the 75 % band began - raw meat 82.2 % in that window - then 95.6-98.7 %). Raw meat 94.0 % / 96.1 %, everything else 96.9 % / 98.0 %.
Failures by want kind (failed + met-after-hour): L1 2 789, L2 600.

| kind | L1 share (fail rate) | L2 share (fail rate) | dominant cause |
|---|---|---|---|
| RawRibs | 51 % (9 %) | **58 %** (6 %; 351) | "offered, nothing brought" 146, "met after hour" 136; supply-bound (1 741 ribs/h brought vs 767 asked) |
| scrolls, all circles | 26 % | 27 % | circle 6-8 = 22 % in both (baseline 59 %): L1 first half 27 % -> second half 16 %; L2 26 % -> 18 % |
| ... ResurrectionScroll (c8) | 166 (83 %) | 49 (**96 %**, 2 met) | "trader found nowhere to have it" 43 |
| ... Mark (c6) / EnergyBolt (c6) / GateTravel (c7) / ChainLightning (c7) | 191 / 145 / 74 / 9 | 4 / 11 / 31 / 26 | one 30 %-skill circle-8 writer |
| LeatherCap | 16 % (58 %; 453) | 11 % (**32 %**; 64) | "buyer past a carrier's reach" 207 / "met after hour" |
| BotCasterStaff | 10 (100 %) | 5 (100 %) | "nobody able looked" |

Scribes able to write (awake): circle 6 grew **1 -> 26** over L1 (32 at L2 start, 21-25 in the day band), circle 7 **1 -> 6-10**, circle 8 **1 (best 30 %)
the whole time**; scrolls written for a want 2 063 (L1) / 588 (L2) vs practice **45 173 / 7 137 (87-95 % of all scrolls are practice)**.
Kept-out lines (not in the share): 3 190 / 919, 90 % "taken back in first 10 min: nobody brought it, walked to a counter" (ribs: 1 574
wants taken back because the buyer held its own).

## 4. Paths, deaths, places

Paths (`Getting about:`, window shares of searches): L1 reached 94.6-96.2 %, partial 3.8-5.3 %, refused 0.03-0.11 %, 0.29 ms/search, 5 834
journeys given up. **L2 drifts: reached 95.7 % (h 0-2) -> 95.5 -> 94.8 -> 93.0 -> 89.9 % (last 2 h)**, partial 4.3 -> 10.1 %. The 30-min
view shows it is spiky, not a slope: partial **10.9 % (10:27), 15.1 % (12:57), 15.5 % (13:27)** against 3.6-5.9 % otherwise, "ended no nearer
than they started" 3-4 % -> 12 %, "pocket proofs lost to the clock" per search x4 - exactly the three war windows (10:07-10:32, 11:27-11:47,
12:42-13:22). Searches fell 335 k -> 230 k per 30 min (population), so the failing share is not load. Errands lost as hopeless, by kind: bind 136,
fight 79, station 29. Journeys given up 2 079, destinations dropped 4 812.

Deaths per bot-hour by window: L1 0.72 / 0.54 / 0.45 / 0.32 / 0.42 / 0.45 / 0.37 / 0.43 / 0.47 / 0.57 / 0.45 / 0.48 (day 1), 0.38 / 0.36 /
0.24 / 0.24 / 0.25 / 0.23 / 0.19 / 0.16 (day 2): a steady fall, not noise. L2: 0.21 0.16 0.30 0.17 0.25 0.19 **0.38 0.28 0.39 0.35** 0.33.
Company size at death: alone 36 % (L1) / 41 % (L2); in a company of 5+: 37 % / 31 %. Work at death (L2, incl. paused pieces): enlist 320,
flee 96, restock 92, rally 85, prowl 81, rescue 56, mine 53, mend 51.

Top 50-tile squares (deaths, % of all):

| L1 (4 064) | | L2 (748) | |
|---|---|---|---|
| **5300,1300 Orc Caves**: 473 (11.6 %); field 5250-5380 x 1250-1370 = **622 (15.3 %)** | | **5300,1300**: 59 (7.9 %); field **73 (9.8 %)** | |
| 950,700 Lich lair (LichLord 62, Lich 31; 17 of 96 alone): 72 | 1.8 % | 600,1450 orc fort W (OrcishMage 17; 18 of 28 alone): 24 | 3.2 % |
| 5450,1850 Gazer/Harpy (86 of 103 alone): 68 | 1.7 % | 950,700 Lich: 22 | 2.9 % |
| 600,1450 orc fort: 65 | 1.6 % | 650,800 Yew (war square): 17 | 2.3 % |
| 5250,1300 / 5350,1300 (field edge): 63 / 57 | 1.6 / 1.4 % | 5450,1850: 14 (+5400,1850: 11) | 1.9 % |

Orc Caves field: **still the top place in both runs, but down** - 0.064 deaths/bot-h in L1, 0.026 in L2 (baseline ~0.107). Killers there: OrcishMage 300,
OrcBomber 170, OrcishLord 68, Orc 53 (L1); OrcBomber 30, OrcishMage 29 (L2). 17 of 73 (L2) were alone; 40 shared their second with other deaths.
Killers overall: L1 OrcishMage 11 %, Reaper 11 %, GiantSerpent 9 %, a bot 6 %, GiantSpider 5 %, OrcBomber 5 %, Gargoyle 5 %, AirElemental 5 %.
L2 **a bot ("one of ours") 17 % - 30 % in the second half (111 of 374)**, GiantSerpent 8 %, OrcishMage 8 %, Reaper 7 %, OrcBomber 5 %, AirElemental 5 %, Lich 4 %.
Bot-on-bot deaths come in bursts of 30-37 an hour at war hours 10, 12, 13 (L1: 33-48 an hour at 09.10 00:00 and 08-09:00, 10.10 09:00 and 11:00) around 650,800 Yew,
800,1150, 2700,850 (Vesper); 237 in L1 (5.8 %), 130 in L2. Wars: L1 5 declared / 6 won by blood / 185 kills; L2 3 / 2 / 92 kills.

## 5. Money

`Money:` (L2 end): 238 earned purses, poorest 0 gp, **median 803 gp**, fattest 181 009 gp (Beatrix the Captain), total 1 100 221 gp, **98 % in bank
accounts** (1 079 137) and 21 084 in pockets. L1: total 215 k -> 921 k (x4.3 in 38.7 h), median 271 -> 861, poorest 0-27 (0 from 10.10 04:00 on), fattest
Aelrindel the Architect 30.6 k -> 131 k. L2 total flat 995 k -> 1 100 k. "Bots under 100 gp" is not a log counter; proxy `The island:` hunts begun by bots
under 100 gp free: 21.6 % of hunts (L1 first 8 h) -> 7.8 % (L1 late window) -> 5.4 % (L2 cumulative); `Society: poor` mood lines 8.

Flows (`Money:` and `City:`): taken out of bots by decisions outside work **2 214 313 gp in 10.2 h (217 k/h) vs handed back 656 479 (64 k/h)** (L1: 5.07 M
vs 2.31 M). Of the outflow **1 156 709 gp (52 %) is the city taking guild payments** (quadrant claims: 1 054 000 gp for 60 claims that gathered, 17.5 k each),
the treasury holds 161 692 gp against its 20 000 cap (1 155 714 "above the cap and kept"), and the city then spent **1 046 872 gp on 3 078 lots / 417 684 things**
(the single biggest buyer: bot-to-bot hand-overs are 620 636 gp in 16 980 meetings, 61 k/h; the city buys ~103 k/h). Minted: only the city, 15 250 gp (L2), 96 325 (L1).
Corpse runs: 1 464 corpses worth 1 127 470 gp gone back for, 105 let go with 85 523 gp in them, 1 189 deaths cost 48 582 gp of supplies.
Frozen: one account holds **16.5 % of all bot gold**; 131 823 -> 181 009 gp (+37 %, +5 k/h) in 10 h, fed by lessons - `The captain:` "127 lessons paid for
at **61 555 gp against 3 236 gp paid back in wages**". Overproduction: `Balance:` "things made into a glut" **946 910 of 1 045 314 (91 %) in L1,
197 410 of 274 215 (72 %) in L2**; IronIngot piled 44 808 (L1, 10.10 01:00) and 48 225 (L2, 08:57) with 0-20 asked, then 3 490 once the city had bought; `Packs:` 68 % of pack weight is
over the allowance (IronOre 2 088 stones, Log 3 050, LeatherCap 1 424 - all surplus; 14-15 of 237 bank boxes full).

## 6. Reflexes, cries (`Reflexes:`; cumulative L1 / L2; window shares identical early and late)

| clause | L1 | L2 |
|---|---|---|
| struck by a creature | 63 903; 18 613 already at it; answered 44 451 (44 116 at once); **never 839** (311 died with nothing decided); 3 454 died though answered | 15 809; 4 776; 10 863 (10 789); **never 170** (54); 545 |
| creature seen coming within 18 tiles | 21 636, answered median <=300 ms, p90 <=3.0 s | 5 221, <=300 ms, p90 <=2.0 s |
| **first blow landed before anything was decided** | 3 480 (**16.1 %** of seen coming; windows 14.1-16.2) | 782 (**15.0 %**; windows 14.5-17.8) |
| ... by why | 1 375 company told, 1 365 on the rung with nothing to take, 584 company not told, 154 in range never put on rung | 322, 306, 120, 33 |
| health crossing the failing line | 7 523; answered 2 382; never 1 561 (**1 269 died with nothing decided**); 1 395 died though answered | 1 631; 567; never 284 (206); 290 |
| **first cries** | 32 974; 13 165 already at it; answered 6 513 (median <=15 s, p90 <=60 s, worst 640 s); **never 13 293** (1 630 died with nothing decided) | 8 175; 3 288; 1 455 (worst 514 s); **never 3 428** (272) |
| cries unanswered / open cries (not already at it) | 67-70 % in every window | 69-73 % |
| never-answered with somebody in earshot | 7 654 (58 %) | 1 914 (56 %) |
| bots under 70 % with a healer within 20 | (not extracted) | 2 485; answered 1 553 (median <=3 s, p90 <=15 s); **never 703**; 866 more had nobody able to mend in reach |

## 7. ERR/WRN, alarms, loop, hangs

ERR: L1 943, L2 282 (`' ERR]'` grepped with the space). WRN 2 172 / 582 (mostly "ground closed ... a killing field" 390 / 65, "could neither walk nor
sell ... put N things on the ground" 251 / 51, claim lines).

| ERR group | L1 | L2 | cause |
|---|---|---|---|
| `Attempted to start timer ... while world is Saving` | 291 (31 %) | 75 (27 %) | engine: `BaseHouse.Serialize` -> `Timer.StartTimer(child.Delete)` on the save thread; every autosave with a decayed item in a house container |
| BotStall "has not moved or changed what it is doing for N minutes" | 462 | 112 | 149 / 15 are sleepers (>=15 min stuck, out of work <2 min = a bot logged out at an inn); real ones cluster at pocket (5741,433,60): 31 / 21 lines |
| "could get nowhere at all ... carried home (TooBig/Enclosed ground)" | 126 | 35 | 17 / 15 from the same pocket (5741,433,60) |
| "fighting bare-handed and has nothing in its pack to put on" (BotArms) | 41 | 28 | bound weapon "gone from the world" or never bound; mostly Mages/Warriors |
| BotStall "has not left (x,y) ... taking and dropping N errands" | 7 | 21 | Baern (12), Galathil Moonshadow (6) |
| other (no counter/fire/bottle in reach, "seam list is full at 2048") | 16 | 11 | |

Alarms (`alerts.ndjson`): L1 373 lines - 8 `dying` raised (40 deaths in 5 min at the 00:00 boot, then 10-17), 7 `errors` raised (stuck/timer), 38 heartbeats,
11 war notes, 151 "still overstated" lines (delve 43, reclaim 40, plunder 21, relic 23). L2 116 lines - 2 `dying` (15 at 03:57, 10 at 06:32), 2 `errors`
("seam list full", "no counter within reach"), 39 "still overstated" (reclaim 26, liberate 7, plunder 4, steal 2), 35 findings (10 of them Argus/Lynceus conjectures that Lorandil is in a "loop of failed fletch
attempts"; L1 findings are mostly "stuck in an unreachable fight").
Overstated: reclaim read 166-169/min pays 7-114; liberate 55 -> 9-12; plunder 40 -> 0-25; steal 10 -> -18...-10; delve 65 -> 9-42; relic 15 -> 7-9.

Loop (`The clock:`): 19.5-26.0 ms a look (L1), 22.6 (L2) at 233-310 bots, deciding 46-68 us/turn, 0 faults. Slow ticks (>100 ms, `BotBeat.SlowMs`):
13 656 in L1 (353/h, ~1 % of ticks), 2 816 in L2 (276/h); of the lines logged (one per 5 min: 445 / 118) the median is 201 / 274 ms and **90 % of the ones with a
dominant turn are one Miner-proposer decision of 150-280 ms** (198 of 221 / 64 of 71; a mine turn averages ~0.06 ms). Two boot ticks of 2.3 s and 3.1 s (Beatrix, hunt/liberate,
Scoutmaster). **One real stall in L1: tick of 51 761 ms at 10.10 01:19:00, 51 760 ms of it in the bots' turns, costliest single turn 6 982 ms (Yllarion Silverleaf on
skulk, auction 6 775 ms, proposer Shopper 2 346 ms) and six log gaps of 9, 27, 14, 9, 22, 30 s between 01:17:12 and 01:22:00** with no ERR; L2 has no gap >= 8 s.
The proposer cost is normally microseconds, so this looks like the whole process being starved (host / GC) rather than the auction - not reproduced in 49 h.

## 8. Defects, by importance

| # | defect | evidence | likely file (BotAIv2/) | size |
|---|---|---|---|---|
| D1 | **Overweight bot never sheds weight** - Baern the Tamer carries 225-258 of 222-229 stones, 0 stamina, since 10.10 02:44 (>35 h, survives restarts) | 1 035 of 5 920 failures (17.5 %), 680 of 770 "made no progress towards ... has given up" walks, 454 unload + 228 prowl + 108 acquire fails; `Baern failed at unload ... still too heavy to walk at 233 of 222 stones, no lighter than when the last pass put things away`; ~110 fails/h; fixing it = +0.9 pt finish share and its search load | `BotPopulation/BotUnload.cs` (`Dump` skips bound and kept items, `Ditch`), `BotWill/BotLadder.cs` (Ceiling), `BotOutfit/` (cap kit weight), `BotPopulation/BotStall.cs` | S |
| D2 | **War windows hurt everything** - rally 34.6 % finish (85 of 309 died), band/stake/ward, partial paths 11-15 %, pocket proofs lost to the clock x4, bot-on-bot deaths 17 % of L2 (30 % in 2nd half) | finish 88.2-88.6 % in 30-min windows containing 10:27, 11:57, 12:57, 13:27 vs 92 %; partial 10.9 / 15.1 / 15.5 %; "the war company had no room by the time it arrived" 57 | `BotEstate/BotRally.cs`, `BotEstate/BotWar.cs`, `BotMovement/BotReach.cs` (pocket proof clock) | M |
| D3 | **Enlist presses leave dropped work behind** - 56 % of all drops are "not taken up again after enlist: put down for N minutes" (1 461) or "pressed to enlist" (963); L1 49 %. About 2.2 pt of the 8.9 pt L2 loss. Also skulk 73 %, homeward 62 %, guard-duty 53 % finish, mostly drops | see section 2 | `BotWill/BotWill.cs` (`Unpause`, `AsideCapMs` ~l.3118), `BotSquad/BotEnlist.cs` | M |
| D4 | **Archers shoot without hitting** - 71-73 % of hunt fails: "would not go down: 100 % left, not a scratch in 45 s", 96 % Bow/Crossbow, LOS True, 88 % at 3+ tiles; hunt finish 84 % on 5 700-18 750 endings; `Bows:` 276 836 of 814 538 asks "had the shot too near" | 424 / 1 539 events, 101-116 bots, ~40/h in both runs | `BotHunt/BotSlay.cs` (verdict), `BotCombat/BotBolt.cs` (kite/shot) | M |
| D5 | **Deliveries cannot close on the buyer** - 585 fails (10 %) "could not get nearer to X at (P) in Britain", rising over time in L1 (369/414/530 per third); hot squares 1380-1400,1720-1740 (Britain south) and 1480,1520; meet "gave the delivery up" 94 / 310 | section 2 table row 1 | `BotMarket/BotRendezvous.cs`, `BotMarket/BotWayless.cs`, `BotMarket/BotCourier.cs` | M |
| D6 | **"No way through" is 15-18 % of failures** and one place is a quarter of it: Khaldun / Orc Caves interior (5340,1290; (5362,1295,-23)) - enlist to a company standing there: 292 (L2) / 328 (L1) fails, 179 / 195 on creatures in "Khaldun"; the same field is the top death place; unload "no way" 151 / 249; chop 297 in L1 | 1 080 of 5 920 / 2 606 of 17 473; enlist 372 / 587 | `BotSquad/BotEnlist.cs`, `BotMovement/BotReach.cs`, `BotWill/BotWill.cs` (phrase), dungeon entry in `BotHunt/` delve | M |
| D7 | **Overproduction with no exit** - 72-91 % of everything made is made into a glut; IronIngot 48 225 piled, 0 asked; IronOre 2 088 stones surplus in packs; 68 % of pack weight above allowance; 87-95 % of written scrolls are practice; mine (6 609 taken, 94 % finished) is the producer, so it inflates the work share while making what nobody asked for | `Balance:`, `Packs:`, `Resolve:` by trade | `BotMarket/BotGlut.cs`, `BotPopulation/BotSurplus.cs`, `BotHarvest/BotMiner.cs`, `BotSpells/BotScribes.cs` | M |
| D8 | **Three wants the market cannot meet**: LeatherCap (8 gp; 32-58 % fail, 1 424 stones of caps idle in packs + 476 in full boxes; "buyer past a carrier's reach" 3 000-6 800 passes per line (3 651-7 937 passes in all)), ResurrectionScroll (96 % fail, one circle-8 scribe at 30 %), RawRibs (58 % of Needs failures, supply-bound) | section 3 | `BotMarket/BotCourier.cs` (`Reach`, `LeastGold`, far-walk rule), `BotSpells/BotScribes.cs`, `BotCraft/BotCook.cs` / hunting | S-M |
| D9 | **First cries unanswered and blows before decisions**: 69-73 % of open first cries never answered (58 % of those had somebody in earshot), 15-16 % of seen-coming creatures land the first blow undecided (306-1 365 on "the rung with nothing it could take"), 1 269 / 206 health-failing bots died with nothing decided | section 6; unchanged between early and late | `BotCombat/BotCry.cs`, `BotCombat/BotReflex.cs`, `BotCombat/BotRescue.cs` | M |
| D10 | **Gold sinks into one hand and the auction reads stale numbers**: Captain account 16.5 % of all gold (+5 k/h; lessons 61 555 gp in, 3 236 out); overstated deeds stand for hours (reclaim 169 vs 7-114, liberate 55 vs 12, plunder 40 vs 0-25, steal 10 vs -18) | section 5 / 7 | `BotDrill/BotAttend.cs`, `BotDrill/BotSchool.cs`; `BotPopulation/BotReclaim.cs`, `BotHunt/BotFreedom.cs`, `BotHunt/BotPlunder.cs`, `BotCrime/BotCutpurse.cs` | S each |
| D11 | **ERR noise hides real alarms** - 27-31 % of ERR is the engine's save-thread timer (and the decayed house items it fails to delete stay forever); 32 % (L1) / 13 % (L2) of stall ERR are sleepers; pocket (5741,433,60) (228 tiles) swallows 21-31 stuck episodes + 12-17 carry-homes a run (4-14 min each); 41 / 28 "fighting bare-handed" | section 7 | engine `Projects/UOContent/Multis/Houses/BaseHouse.cs:3001` (`Timer.StartTimer(child.Delete)` inside `Serialize`), `BotPopulation/BotStall.cs`, `BotMovement/BotReach.cs`, `BotCombat/BotArms.cs` | S |
| D12 | **One Miner-proposer decision costs 150-280 ms** and causes ~90 % of the slow ticks that have a dominant turn (13 656 / 2 816 slow ticks); plus one unexplained 51.8 s stall (L1, 10.10 01:19) | section 7 | `BotHarvest/BotMiner.cs` (proposer), ore/seam list in `BotGround` (full at 2048), `BotPopulation/BotBeat.cs` | S (perf), stall: measure first |

## 9. What is better than 08.10 (for the KPI guard)

Needs 95.8-97.3 % (was 93.9-96.3); scroll share of Needs failures 22 % at circles 6-8 (was 59 %), scribes able at circle 6: 23-32 (was 1);
deaths/bot-h 0.27-0.41 (was 0.56-0.78) and 0.16-0.25 late on day 2; Orc Caves field 9.8-15.3 % of deaths at a third of the rate; delve finish 71-88 % (was 34 %);
median wealth 800+ gp, poorest 0-27 gp, poor hunters 5-8 %; loop 21-23 ms/look; one 51.8 s stall in 49 h, none in L2. Not better: work finished 91.1-92.4 % (goal 95 %), paths
drift in war windows, cries/reflex shares unchanged.

Open question for the next wave: why endings per bot-hour fall 17 % in the 75 % band (prowl/enlist/inscribe halve) while fail counts stay constant.
