# The bots' economy: how money and goods move, what broke, and how it was measured

The shard runs a population of autonomous bots on a ModernUO server (Ultima Online, Renaissance era). The bots
gather, craft, hunt, sell to the engine's shopkeepers, and trade with each other on a market of their own. This
folder is about the money: where it comes from, where it goes, how the project measures it, and the defects the
measuring found between August and 26 September 2026.

Two kinds of evidence are used and kept apart:

- **The decision log and the code** (`Projects/BotAIv2/DECISIONS.md`, the module READMEs, and comments in the code)
  for everything before 18 September. Numbers from there are quoted as the project recorded them, with the
  section they come from.
- **The session logs of 18–26 September 2026**, read by `extract.py` into `data/`. Every number in section 3 comes
  from those files and can be regenerated (section 6). The logs cover **1,007 five-minute summaries from 117 sessions,
  84.1 hours of shard time**, 18.09 00:17 to 26.09 15:28.

`index.html` draws the same data: the money flows, the three numbers by day, every five-minute reading on one
timeline, and the armour orders of 26 September.

| Over the 84.1 hours of 18–26.09 | gp |
|---|---:|
| Bots paid bots on their market (stall sales and filled wants) | 1,853,102 |
| The city bought off the bots' stalls (booked by the market as sales) | 466,672 |
| Shopkeepers paid bots | 2,329,629 |
| Bots paid shopkeepers | 1,835,294 |
| The most coin ever down on the want board at once (20.09 13:32) | 104,041 |
| Coin on the want board at the last summary before restarts the bots survived, 85 restarts (estimate) | 638,810 |
| What guilds paid the city for ground that arrived at a full treasury and vanished (estimate) | 722,254 |
| The largest single purse (Hale the Architect, 24.09 21:16) | 66,408 |

---

## 1. How money and goods move

### 1.1 Where coin enters and where it leaves

The bots are born with a small purse. After that, coin enters their hands three ways (`GUIDE.md`, "The economy"):

- **Kills.** A creature's purse is new money. No summary line counts it, so it is the one flow this folder cannot
  measure (section 2.5).
- **Selling to a shopkeeper.** The engine's NPC counters pay real coin for loot and for goods nobody on the market
  wanted (the `peddle` errand). `BotShops/README.md`: "Coin handed to a shopkeeper leaves the world; coin taken
  from one is the only coin that enters it."
- **The city.** Its treasury mints 3,000 gp an hour up to 20,000 gp and spends it on goods off the bots' stalls,
  errands and bounties (1.7).

Coin leaves through the shopkeepers' counters (reagents, bandages, cloth, bottles, blank scrolls, tools, horses,
cooking lessons), through guild levies for halls and ground (1.6), and — found in these logs — through the treasury's
cap (3.5) and through every restart (3.4).

Between those two ends sits everything that only moves coin from one bot to another: the market, the 1 % levy on
it, a guild's counter, the Captain's lesson fees, a hired healer's wage, tithes and tolls, robbery.

### 1.2 The market: stalls, wants and escrow

`BotAuction` is the bots' own market (`BotAuction/README.md`).

- **A stall** is one bot, one kind of thing, a quantity and a price. It lives for the life of the process;
  topping it up with more of the same keeps the price the stall has learned. The goods sit out of the world, so a
  bot cannot sell the same ore twice or lose it on the way home. Nothing expires; an unsold stall gets cheaper
  instead.
- **A want** is the same thing with the sign turned round: a bot asks for something it cannot make, and **the coin
  is taken from its pack and bank account the moment it asks**. That coin, held by the market, is the **escrow**.
  It is paid to whoever fills the want, or handed back if the want gives up. "An offer costs exactly what it says":
  in the first version of the bots somebody offered 1,500 for twenty feathers with an empty purse.
- **A sale settles in a fixed order**: charge the buyer (pack first, then the bank), deliver, refund whatever could
  not be delivered, pay the seller into the bank, less the levy. The reverse order mints gold, because the engine's
  deposit adds to an account without touching what the depositor carries. The first version could not account for
  110,900 gp over one night.
- **A bot cannot be on both sides of one kind of thing.** One number with a sign: plus is a stall, minus is a want.
  This is what killed the first version's worst loop, in which bots passed the same fifteen ginseng and the same
  seventy-five gold round in a circle, and two bots sold the same shopkeepers 4,152 single items
  (`DECISIONS.md`, C1).
- **Crossing.** Every market beat (30 s) puts stalls and wants for the same thing together and pays the want's offer
  (`BotAuction.Cross`, since 04.09; 4.7 below). One supplier fills at most 5 units of a want at a time, so the first
  bot with a pile does not own every want for that pile.
- **Capacity**: 1,024 stalls and 512 wants. Both caps were raised after being hit: at 256 stalls the market refused
  302 listings in ten hours, 50 of them iron ingots and 41 leather; at 128 wants every errand that needed one failed
  on its first beat (code comments in `BotAuction.cs`; 4.8).
- **1 % of every settled sale**, at least 1 gp, goes to the one bot of the Architect class, "paid by the health of
  the market rather than by any errand in it" (`BotClasses/BotArchitect.cs`).
- **The market is not saved.** Stalls and wants live in memory and are gone after a restart (`DECISIONS.md` §2.8).
  This matters more than it did when it was written; see 3.4.

### 1.3 How prices move, and who sets them

No configuration file sets a price. The dials say how fast a bot changes its mind; the values below are the
running shard's boot line of 26.09:

| event | what happens |
|---|---|
| the same stall sells again within 10 minutes | its price rises 15 % |
| a stall sits unsold for 10 minutes | its price falls 10 % |
| a want goes unfilled for 10 minutes | its offer rises 15 %, and the buyer must put the difference down first |
| any price | stays between ×0.25 and ×4 of the opening ask |
| a want at ×4 still unfilled after the stale period | gives up and takes its escrow back |
| a stall at its lowest ask for 30 minutes | goes back to the seller's pack, as far as the pack will take it |
| anything worth less than 2 gp | is not put on a stall at all |

A new stall or want opens at what `BotAuction.Worth` says: first what somebody is **offering** for one on the board,
then what one last **changed hands for**, and only then the bot's own guess. Build 259 (26.09) capped what is read
back from a standing offer at four times the asker's own estimate, after the armour orders of that day (4.21).

A stall that nobody buys from for 10 minutes is carried to a shopkeeper (the boot line: "goods the market has
ignored for 10 minutes are carried to a counter").

### 1.4 Shopkeepers

A bot buys and sells over the engine's own counters (`BaseVendor.OnBuyItems`, `OnSellItems`), at the shard's own
prices and stock limits, exactly as a player would. Two rules decide which way coin goes:

- **The bots' market is asked before a counter, and a tie goes to the stall** (`DECISIONS.md` §2.4, M1). A
  shopkeeper is the ceiling on what a bot can charge. Coin paid to a bot stays in the population; coin paid to a
  shopkeeper leaves it.
- **A bot never opens a stall above what a shopkeeper in reach asks** (`BotShops.Shelf`, since 05.09; 4.13).

### 1.5 The pack and the bank

The engine takes a bill under 2,000 gp out of the backpack and never looks at the bank for it. A bot keeps a float of
100 gp and banks the rest whenever it stands at a counter; so a purchase first draws the shortfall from the bank into
the pack (since 25.08; 4.5). A seller on the market is paid into the bank, because a seller may be standing in a mine.
A pack holds 125 things (the engine's cap), and a full pack can neither pay nor be paid at a counter (4.19).

### 1.6 Guilds: levies, chests, tolls, counters

- **Levies.** A guild raises a hall (5,000 gp) and claims ground (four squares free, then 5,000 gp a square up to
  ten, each square past ten 1,000 gp dearer than the one before) by a levy on its members, richest first
  (`BotEstate`, `BotClaim.Cost`). What is paid for ground goes to
  the city's treasury, up to its cap (1.7, 3.5).
- **The chest** (build 93, 16.09). A tenth of the coin a bot takes by hunting on ground its guild holds goes into
  the guild's chest, and the chest pays the guild's next claim or hall before any member is asked.
- **Tolls** (build 239, 26.09). A stranger hunting a guild's land pays that guild 10 % of its hunting coin (posted), or
  20 % once a tollman has told it (patrolled).
- **The counter.** A merchant in the hall sells the members what the guild's couriers bought in town, at cost, so
  nine members do not each walk to Britain. "It is not a second faucet": the same coin leaves the world once instead
  of nine times (`BotEstate/README.md`).
- **The crown's tax** takes a share of what guilds can spare into the watchers' prize purse (`BotRevel.Tax`).

### 1.7 The city's treasury

`BotCity` (build 75, 16.09) is the one purse that mints: 3,000 gp an hour up to a cap of 20,000 gp. It spends on:

- **stalls that have stood longest**, bought whole, and **standing orders** for a named thing at a named price;
- **fairs**: for an hour the city takes 20 stalls a minute, longest-standing first, at 80 % of the asking price, by
  itself every six hours when the purse holds at least 5,000 gp, or when a watcher calls one;
- **errands and bounties**, with the reward set aside when they are posted.

Two facts about it matter for measuring. Its purchases are **booked as ordinary market sales**, so the stall learns
from them (`BotAuction.Purchase`); the market's own turnover therefore includes the city. And the price guilds pay
for ground is added to the purse **only up to the cap** (`BotCity.Tax`, build 79); whatever does not fit is gone.
The purse itself survives a restart (build 89); before that, every restart opened a full treasury at 1,000 gp again.

### 1.8 The Captain and the Architect

Two classes are paid by other bots as a matter of course. The **Captain** teaches: a lesson costs 60 gp and 20 gp a
point (`DECISIONS.md` §4.2, 02.09). The **Architect** takes the 1 % levy on every sale, and it is born already
skilled — at 78 of its targets of 100 — in mining, smithing, tailoring and tinkering (`BotClasses/BotArchitect.cs`,
`BotClass.Seasoning`), which for long stretches made it the bot that filled the armour orders. The two stories of
one purse holding most of the money are these two classes (4.12, 3.6).

(The `Lessons:` summary line is something else: cooking lessons a bot buys from a shopkeeper, the engine's own
teaching at 10 gp a point, `BotCraft/BotTutor.cs`. That coin leaves the world.)

---

## 2. How it is measured

### 2.1 The five-minute block

Every five minutes the shard writes a block of summary lines (`BotPopulation/BotBeat.cs`). The ones read here:

| line | what it says |
|---|---|
| `The market:` | stalls, things on them and their worth at the asking price; wants, things wanted and the escrow; sales, fills and their gold; crossings; price moves; wants given up; the levy; stalls taken back |
| `Trade:` | things bought from shopkeepers and for how much; things sold to them and for how much |
| `Money:` | the purses of every bot that earns its own living: poorest, median, fattest and **who holds it**, and the total split into pockets and bank accounts |
| `City:` | the treasury, what it minted, was paid in tax, spent, and spent at fairs |
| `Estate:` | guild levies, the crown's tax, ground paid for, tithes, chest draws, the chests, tolls, the guild counters |
| `The board:` | the six kinds most wanted and most stocked, in units |

**The counters are running totals since the process started** (`DECISIONS.md` §2.7, rule O2: "Summary counters are
cumulative since the world loaded. Rates are differences; sessions compare only at equal age"). So:

- a session's total is its **last** summary; a window is the difference between two consecutive summaries;
- the board (stalls, wants, escrow), the purses and the treasury are **snapshots** and are read as printed;
- each process has its own log file, so the logs are summed session by session. No counter went down inside a
  process in these logs (the `segments` column says so).

The project's own window script (`night-window.py`, "N sales and M fills for Xgp in the window") takes exactly these
differences; this folder does the same for every five-minute window.

### 2.2 The three numbers

The project reads the economy by three numbers. From the watch script that first printed them: "The one number that
says whether this is an economy or a faucet and a drain: what the population earns from shopkeepers, what it hands
back to them, and how much of it went to another bot instead." That is:

1. **In**: what shopkeepers paid bots (`Trade:` "sold for").
2. **Out**: what bots paid shopkeepers (`Trade:` "bought for").
3. **Between**: what bots paid each other on their market (`The market:` "sales and fills for").

Coin frozen in wants is not one of the three. It is a fourth reading, printed on the market line ("with Ngp down"),
and build 259 names it as the line to watch. This folder extracts all four.

### 2.3 What this folder adds to the reading

- **The city is taken out of "between".** The city's purchases off the stalls are booked by the market as sales
  (1.7). Each one also writes "The city bought N ... for Ngp", in the same call. Adding those lines up per window, in
  log order, and taking them out of the turnover leaves what bots paid bots: **466,672 gp of the market's
  2,319,774 gp turnover (20.1 %) was the city**, 459,680 gp of it at fairs. Turnover figures in the decision log's
  session notes include the city.
- **What a restart takes from the board** (3.4), from the last summary of each session whose next boot brought the
  bots back from the save.
- **What the treasury kept of the ground money** (3.5), from its own printed purse, mint and spending.

### 2.4 Instruments that lied before

The decision log's class C5, as it touched money:

- **The watch script's FLOW counted fills only** and missed stall sales, so every share of trade between bots read a
  third low (04.09). Fixed by reading the market's own turnover.
- **Cumulative counters read as per-window numbers** gave half a day of wrong comparisons (05.09). That produced rule
  O2.
- **A grep pattern for purchases off a stall** matched nothing and reported "zero bought from stalls" (05.09): a
  purchase off a stall ends "... N SulfurousAsh off the market for Ngp", and the pattern looked for the counter's
  wording.
- **A counter of beats read as a count of bots**: "short of Bandage 9,637 times" was a handful of bots asking every
  beat (05.09).
- **Crossed and Dear stood still for five windows** (04.09): the stalls and the wants held different kinds, so the
  two counters of the new crossing had nothing to count; the price cuts were the reading that moved.
- **A throttled log line used as a measurement** hid four armour defects (27.08).

### 2.5 Limits of these numbers

- **Coin from kills is not in any summary line**, so "In" here is the shopkeepers' part only; what actually entered
  the bots' hands is larger by an unknown amount. The individual "took N things and Ngp" lines exist, but a company's
  and a dungeon party's takings are split elsewhere, and adding them up was not attempted.
- **The purses count only bots in the world that earn their own living.** A bot on a stipend, a minded bot, and
  since 24.09 a bot resting offline are left out, so the purse total moves when bots come and go. The richest bot
  disappears from the line while it rests.
- **The last minutes of each session are missing**: activity after its last summary (under five minutes) is in no
  total. 35 of the 152 session logs printed no summary at all.
- **Two figures are estimates, and say so**: what a restart took (3.4) and what the treasury's cap destroyed (3.5).
- **Stall worth is at the asking price**, not at what the goods would fetch.
- **"Put down" in `wants_by_session.csv` is gross**: a new want's bill and every raise's top-up. A bot asking again
  for more of the same tops up silently and is not in it, and the coin a want hands back when it closes is not
  printed.

---

## 3. What the logs of 18–26 September show

### 3.1 The totals

| | gp |
|---|---:|
| The market's turnover (43,130 sales and 61,136 fills) | 2,319,774 |
| of it, the city's purchases off stalls | 466,672 |
| of it, bots paid bots | **1,853,102** |
| Shopkeepers paid bots | **2,329,629** |
| Bots paid shopkeepers | **1,835,294** |
| Levy paid to the Architect | 91,699 |
| City: minted | 181,911 |
| City: paid in tax for ground and fines | 1,044,468 |
| City: spent (goods, errands, bounties) | 483,120 |
| City: spent at fairs | 459,680 |
| Guilds: levied off their members | 1,106,200 |
| Guilds: paid for ground | 1,042,000 |
| Crown's tax off guild members | 199,896 |
| Tithes into guild chests | 14,153 |
| Tolls (from 26.09) | 84 |
| Members' purchases at their guild's counter | 233,307 |
| Cooking lessons bought from shopkeepers (printed from 24.09) | 38,265 |

Bots paying bots was **30.8 %** of the three numbers together. 59,342 things went straight off a stall into a want;
1,934 wants gave up; 13,952 stalls were taken back at their lowest ask; 60,109 things were worth less than the 2 gp
floor and stayed in packs.

### 3.2 By day

Gold per hour of shard time, by the day a session started (`data/days.csv`):

| Day | Sessions | Hours | Bots paid shopkeepers | Shopkeepers paid bots | Bots paid bots | City bought off stalls | Bots paid bots, share of the three | Highest escrow |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 18.09 | 34 | 18.2 | 37,963 | 37,576 | 38,004 | 3,223 | 33.5 % | 66,751 |
| 20.09 | 1 | 5.2 | 20,261 | 38,044 | 14,913 | 2,990 | 20.4 % | 104,041 |
| 21.09 | 5 | 3.7 | 27,181 | 20,456 | 18,910 | 4,430 | 28.4 % | 23,428 |
| 22.09 | 14 | 15.6 | 24,154 | 31,580 | 23,046 | 9,336 | 29.3 % | 11,103 |
| 24.09 | 30 | 17.8 | 14,245 | 24,911 | 15,140 | 3,493 | 27.9 % | 12,609 |
| 25.09 | 10 | 10.8 | 18,217 | 29,669 | 24,687 | 9,513 | 34.0 % | 70,732 |
| 26.09 | 23 | 12.9 | 8,714 | 9,114 | 9,241 | 5,119 | 34.1 % | 35,552 |
| all | 117 | 84.1 | 21,819 | 27,696 | 22,031 | 5,548 | 30.8 % | |

19.09 and 23.09 have no summaries. 26.09 is a new world: it was wiped on 25.09 at 17:32 and restarted with 50 bots.
The day the escrow reached 104,041 gp, 20.09, had the lowest share of trade between bots.

### 3.3 Coin frozen on the want board

The decision log records one escrow freeze in this period, on 26.09 (4.21). The summaries show four larger or
comparable ones, none of them in the log:

| Highest escrow of the session | At | Session ran | Wants | All purses counted then | What climbed |
|---:|---|---:|---:|---:|---|
| 104,041 gp | 20.09 13:32 | 5.2 h | 117 | 100,094 gp in 79 purses | Leather Chest offers to 5,321 gp; Chain Legs 3,504; Plate Chest 3,200; Leather Skirt 2,613 |
| 70,732 gp | 25.09 13:39 | 6.5 h | 72 | 46,232 gp in 55 | raw ribs to 124 gp a rib, against 3–7 gp in the day's other sessions; Lightning Scroll to 3,024 gp; Energy Bolt Scroll to 2,940 gp |
| 66,751 gp | 18.09 10:20 | 3.8 h | 129 | 40,120 gp in 74 | Chain Legs to 4,013 gp; Leather Chest 2,002; Leather Legs 1,120 |
| 42,422 gp | 18.09 05:05 | 2.3 h | 195 | 32,528 gp in 74 | Chain Legs to 1,314 gp; Plate Chest 1,200 |
| 35,552 gp | 26.09 09:02 | 5.7 h | 68 | 19,331 gp in 28 | Leather Bustier Arms to 1,505 gp; Leather Skirt 1,332 (4.21) |

(The offers are the highest offer per piece in `data/wants_by_session.csv`; "all purses" is the `Money:` line at the
same summary.)

What they have in common:

- **At each peak the want board held more coin than all the purses the summary counted.** On 20.09 at 13:32,
  104,041 gp stood in wants while 79 bots held 100,094 gp between them.
- **The same ratchet.** A want nobody fills raises its offer 15 % every ten minutes, and a new want opens at the
  highest offer already standing (`BotAuction.Worth`, 1.3). Build 259 found this on armour; the logs show it on raw
  ribs and spell scrolls too. The cap build 259 put on `Worth` is not specific to armour, but whether it holds for
  ribs and scrolls is not measured yet.
- **Only long runs climb.** All five sessions above 30,000 gp ran between 2.3 and 6.5 hours without a restart. No
  session shorter than two hours went above 24,177 gp. But not every long run climbed: 22.09 from 05:11 ran four
  hours and peaked at 11,103 gp. The restarts that kept escrow low did it by destroying it (3.4).
- **While the coin stood, trade slowed.** On 20.09 bots paid bots 24,740 gp in the first hour and between 10,853
  and 14,075 gp in each hour after it, while the escrow climbed from 18,011 to 97,778 gp (`summary.json`,
  `escrow.largest_by_session[].by_hour`).

Two of the freezes ended in a burst of fills at the escalated prices rather than at a restart. On 25.09, in the hour
from 13:49, bots paid bots 98,581 gp and the escrow fell from 67,511 gp to 6,763 gp; raw-rib wants paid 116,206 gp
on fills in that session, against 16,022 gp in the five hours of 20.09. On 26.09 it was the armour, paid to one bot
(4.21, 3.6).

### 3.4 The board does not survive a restart

Stalls and wants are kept in memory only (`DECISIONS.md` §2.8: "lost: the market (stalls and wants)"). Until
build 163 (18.09) that cost nothing extra: every bot was deleted at a boot and raised again with a new purse. Since
build 163 the bots come back from the save with their packs and their bank accounts, and the board still does not.
The coin a bot put down on a want was taken out of its pack and account when it asked (1.2), so after a restart it
is in neither place, and the goods on its stalls are out of the world with no stall to come back from.

The purses agree. Across the restart after 20.09 the want board held 104,041 gp and the purses 100,094 gp at the last
summary; at the first summary after the next boot that ran (21.09 18:11) the purses held 62,279 gp and a fresh board
9,143 gp. Across the restart of 26.09 at 10:41 the board held 27,829 gp and the purses 12,011 gp; at the first summary
after the boot the purses held 7,552 gp. Had the escrow come back, the purses would have risen by it.

Measured at the last summary before each of the **85 restarts after which the bots came back from the save**:
**638,810 gp of escrow** and **986,562 gp of goods on stalls, at their asking price**. This is an estimate: the last
summary is up to five minutes before the process stopped, and what was really lost is what stood at the last world
save, which the logs do not print.

### 3.5 The treasury's cap

What a guild pays for ground has gone into the city's treasury since build 79, "into the city's treasury rather than
out of the world: the one drain that used to vanish" (comment in `BotEstate/BotClaim.cs`). It goes in only up to the
cap of 20,000 gp (`BotCity.Tax`), and the treasury stood at its cap in **286 of the 1,007 summaries**.

Every change to the treasury is the mint, the tax, a payment, or coin moved between the purse and the errands it holds
money for, and the City line prints all of them. So between a session's first and last summary, the tax the purse
kept is its change (with the errands' money) less what was minted plus what was spent. Summed over the sessions:
**of 1,026,508 gp paid in, the purse kept 304,254 gp; 722,254 gp (70 %) arrived at a full purse and vanished.** The
drain build 79 closed is mostly still open, because guilds paid for ground in bursts larger than the room under the
cap: 203,000 gp in the 6.5 hours of 25.09 from 08:44, 95,000 gp in the four hours of 22.09 from 05:11.

### 3.6 One purse

The `Money:` line names the richest bot. In **692 of the 1,007 summaries it was the Architect**, holding a median of
**52 %** of all the coin the line counted:

- **Hale the Architect**, from 18.09 to the wipe of 25.09: 10,067 gp at the end of 20.09, 61,889 gp by 22.09 09:16,
  and **66,408 gp at 24.09 21:16, of 104,226 gp** counted.
- **Faron the Architect**, after the wipe: **35,415 gp of 39,742 gp (89 %) at 26.09 15:23**.

Where it comes from, as far as the logs say: the 1 % levy (91,699 gp in all) and, mostly, the armour orders the
Architect was often the only bot able to fill. Hale was paid 58,046 gp of the 66,403 gp that armour fills paid on
18.09, and 47,790 of 58,866 gp on 22.09; Faron 26,215 of 40,099 gp on 26.09 (`data/summary.json`, `armour_wants`).
The rest of the Architect's income (its own stalls) is not separable in the log.

This is the same shape as the Captain's till of 04.09 (4.12): coin flows in from every other bot, and nothing asks the
one purse to spend it.

### 3.7 Goods nobody wanted: logs on 20.09

On 20.09 the goods on stalls rose from 2,178 things (13,069 gp at the asking price) at 08:27 to 55,632 things
(118,084 gp) at 13:27. The board's own line says what they were: **logs, 204 at 08:27 and 53,183 at 13:27**, while the
most wanted things were arrows (677), raw ribs (470) and bolts (361). Wood was being cut far faster than anybody
turned it into arrows or bought it. This is the open defect "goods nobody buys are still made" (4.20) at a larger
scale than the one it was filed on.

---

## 4. The defects, in the order they were found

Dates are 2026. "C1", "C12" and so on are the defect classes of `DECISIONS.md` §3; the numbers are the ones recorded
there or in the code comments named. What sections 3.3–3.7 found in these logs is summed up in section 5.

**4.1 The first version could not say where the money went (17.08).**
*Symptom:* 110,900 gp over one night that nobody could account for; two bots sold the same shopkeepers 4,152 single
items; the same fifteen ginseng and seventy-five gold went round in a circle. *Cause*, as the present market's
documentation reads it: orders were filled whole without asking whether the filler needed the thing, so the filler
then posted an order of its own, and nothing kept a ledger that could say where each coin went. *Fix:* the first version was deleted (21.08); the present market settles charge, deliver, refund,
pay, and a bot cannot be on both sides of a kind (1.2). *Status:* holds.

**4.2 No starting purse (21.08).** *Symptom:* every piece of work with an outlay failed on its first beat, so digging,
which is free, was the only thing on the shard (C8). *Fix:* a starting purse. *Status:* holds.

**4.3 A board nobody asked on (24.08).** *Symptom:* the Needs board stood empty for days. *Cause:* one call in the
whole assembly raised a want (C12). *Fix:* demand from the armoury, upkeep and the smiths' metal orders. *Status:*
holds.

**4.4 No leather anywhere (25.08).** *Symptom:* leather orders could stand on the board and nothing could fill them.
*Cause:* no shopkeeper sells raw leather, and no bot knew how to carve a corpse (C4, C12). *Fix:* carving folded into
going through a corpse; the first closed chain, carve → market → tailor. *Status:* holds.

**4.5 Money that could not be spent (25.08).** *Symptom:* 1,929 fruitless trips to restock in 30 minutes with money in
the bank (C4). *Cause:* the engine pays a bill under 2,000 gp out of the pack only, and the bots' money was in the bank.
*Fix:* a purchase draws the shortfall from the bank first (§4.2); a bot keeps a float of 100 gp in its pack.
*Status:* holds.

**4.6 Paid orders passed over (26.08).** *Symptom:* four paid armour orders stood an hour while 17 sewings went by, 0
filled (C1). *Cause:* the tailor chose its recipe by its skill and never read the board. *Fix:* paid orders before the
hardest recipe; the first fill came 23 seconds later (§4.2). *Status:* holds.

**4.7 Stalls and wants that never met (04.09).**
*Symptom:* "69 sales and 9 fills with 290 stalls and 90 wants" (C12). Calla stood with 60 gp down for twenty
feathers while five bots carried feathers to Missy the shopkeeper, and 261 fletchers were turned away for want of
feathers in the same half hour (comment on `BotAuction.Cross`). *Cause:* every way into filling a want started from
a bot holding the goods in its pack; goods already on a stall were invisible to every want, and a want to every
stall. It is the type case of class C12, two working mechanisms with no edge between them, whose sign is two healthy,
growing counters and a third, the trade between them, near zero. *Fix:* `BotAuction.Cross` on the market's beat,
paying the want's offer.
Beside it, the shopper compared `stall < counter` where the seeker used `<=`, so an arrow made on the island could
never be sold on it (C2); the tie now goes to the stall everywhere. *Status:* holds; 59,342 things crossed in these
logs.

**4.8 A full board checked in the wrong place (04.09).** *Symptom:* 273 errors per 30 minutes; 85 of 125 purchases
were one bot on one scroll (C1). *Cause:* the want cap (128) was checked inside the errand, so the errand failed on its
first beat and was offered again. *Fix:* the check moved to the choice (273 → 7); the caps raised to 1,024 stalls and
512 wants. *Status:* holds.

**4.9 Goods the market could not see (04.09).** *Symptom:* "476 carrying enough" beside "243 could not find wood"
(C12). *Cause:* goods reached the market only through the unload errand, and unload was triggered by the weight of the
pack; a gatherer that had stopped at a working amount below that weight kept its goods, and the fletchers had none.
*Fix:* `BotUnload.Wanted`: a bot carries to the market whatever the board has money down for; "could not find wood"
fell from 77 % to 13 % (§4.2). *Status:* holds.

**4.10 An order with no producer (04.09).** *Symptom:* within half an hour of adding empty bottles to the crafters'
orders, trade between bots fell from 3,683 gp a window to 490 (31.8 % of the flow to 4.7 %), sales from 38 to 12,
fills from 63 to 23, and the richest locked-out crafter from 158 gp to 89. Twelve bottle wants were raised and one
filled, each freezing 100 gp (comment in `BotShops/BotStores.cs`). *Cause:* nobody makes bottles as a trade; glass
comes back a bottle at a time from whoever drinks a potion. Money in escrow is money not spent. *Fix:* reverted; the
rule left behind is that a material nobody's work produces is never ordered by the armful. *Status:* rule holds.

**4.11 Wants for what nothing makes (04.09).** *Symptom:* 25,890 gp of escrow frozen against 13,744 gp in all purses;
the bandage bid climbed from 5 to 48 gp (C1). *Cause:* bandages were ordered on the board, and no bot crafts them.
*Fix:* `BotShopper.Makeable`: a want only for what some bot's work makes. *Status:* holds for bandages; the same
shape came back on other kinds (3.3).

**4.12 The Captain's till (04.09).** *Symptom:* over 15 hours to 18:04 on 04.09, Dain the Captain grew from 8,577 gp
to 98,205 gp, "two thirds of every coin on the island in one account", while the median purse stayed under a thousand
(comment in `BotDrill/BotSchool.cs`; `BotPurse.cs` records 98,242 gp, 68 % of the coin 50 bots held). A purse that
large reads like coin being minted somewhere. *Cause:* not minting: the school. In the last of those sessions the
office took 10,222 gp in lessons and paid 198 gp back out in wages; the two numbers were printed in different places
and nobody had put them side by side. A sink, not a faucet. *Fix:* fees and wages printed in one sentence; the richest
purse named in the `Money:` line. Then the 98k vanished whole when a class change dropped the progress record that
held it (C6). *Status:* the lesson price and the till are listed among the decisions waiting for the project's owner
(§6); the one-purse shape returned with the Architect (3.6).

**4.13 Opening above the shelf (05.09).** *Symptom:* 81 % of the money spent on supplies left the world (C2); the
decision log records the shoppers' choice, shopkeeper against cheaper stall, as 173/41. *Cause:* 1,986 reagents stood
on stalls at 5 gp while a herbalist sold garlic at 3 gp, and a shopper takes the cheaper (comment on
`BotShops.Shelf`). *Fix:* a bot's goods open at what a shopkeeper in reach asks; the choice went to 1/311 (§4.2).
*Status:* holds.

**4.14 Flat supply against skewed demand (05.09).** *Symptom:* the gatherers worked, the stalls were full, and the
population still met only 41 % of its own reagent demand (C12). No single number showed it; only the two distributions
side by side. *Cause:* a gatherer picked its reagent at random, an eighth of each, while one run wanted sulfurous ash
1,320, bloodmoss 393, nightshade 314, ginseng 243 and almost nothing of the rest; the ash ran out and was bought over a
counter, and of 2,661 reagents listed a thousand stood unsold (comment in `BotHarvest/BotHerbs.cs`). *Fix:* a
gatherer picks the reagent least stocked on the stalls now: 96 % (§4.2). *Status:* holds.

**4.15 A glut the markdown could not keep up with (05.09).** *Symptom:* opening the supply side took the stalls from
1,558 things to 3,584 in forty minutes, while trade between bots fell from 7,288 gp to 4,898 gp and the takings from
shopkeepers rose (comment on `BotAuction.StaleMs`). *Cause:* a price came down once per half hour, so a price set at
twice what anybody pays needed three and a half hours to reach it. *Fix:* the stale period cut to 10 minutes, one of
the owner's orders of that night; after the whole set, trade between bots went from 7,288 to 20,485 gp (§4.2).
*Status:* holds.

**4.16 The hunter and the cook are one bot (08.09).** *Symptom:* 900 raw-meat wants, 0 supplies, 6,919 gp in escrow
(C12). *Cause:* the cook's rule to keep raw meat for its own pan did not look at the board, and the hunters and the
cooks were the same bots, so the meat stayed in their packs while the paid orders for it stood. *Fix:* spares respect
the board's wants. *Status:* holds.

**4.17 Surplus with no exit (10.09).** *Symptom:* 74 % of everything the population carried was surplus; one
gatherer held 659 bandages (C12). *Cause:* three doors out of a pack, all shut. (a) A refusal at the 2 gp floor, made
where no shopkeeper was in reach, was written down as a verdict on the whole kind: 34,505 things of 56 kinds stayed in
packs (C2). (b) The trip to sell a valuable pack compared a sum in gold (400) with a count of lots, which can never
exceed 125: 0 trips in eight hours (C2). (c) Putting things down was allowed only to a bot that could not walk. And a
guild courier kept buying for a counter that was already full: 1,239 "the merchant would not take any more" and 1,222
"the pack would not hold the coin" in eight hours (C1). *Fix:* what the market refuses goes to the bank box; the
trip counts lots (8); floor refusals only from measured prices; the counter's room is checked. Carried surplus fell
from 73–78 % to 43 % in 105 minutes (§4.2). *Status:* holds.

**4.18 Goods made to order almost never (08.09).** *Symptom:* 71 items made to order against 2,952 on speculation
(C12). *Status:* open.

**4.19 A jammed pack (14.09).** *Symptom:* 8,719 failed trips to the shops in half an hour by one bot (C1). *Cause:* a
tailor's 197 unsold oil cloths, handed back from the market without asking the pack, took it past the engine's
125-thing cap, and a full pack can neither pay nor be paid at a counter (`BotAuction/README.md`, `BotShops/README.md`).
*Fix:* work at a counter first asks whether the pack can take a coin (build 37); the take-back asks the pack.
*Status:* holds.

**4.20 Goods nobody buys are still made (14.09).** *Symptom:* Hale sewed oil cloths all day: 40 price cuts and 5
take-backs (C12). *Cause:* a maker does not read the market before making. *Fix:* proposed as §5 S7 ("a maker does
not choose a product whose stalls stand at their lowest price unsold"), not built. *Status:* open; see 3.7 for 20.09.

**4.21 The armour orders that froze thirty thousand gold (26.09).** *Symptom:* 30,656 gp in escrow at 08:42 on
26.09, in 67 wants; an offer for Leather Bustier Arms climbed from 48 gp (the first want, 01:09) through 862 gp
(08:39) to 1,505 gp (Ivo, 09:21:11). *Cause* (build 259): Faron, the one bot on the shard able to sew leather armour,
wore out his sewing kit at 01:48:58 and did not replace it for seven and a half hours. A trade tool gone altogether
was priced at the shopping price, 12 a minute, while one merely worn was priced at 180, so the restock kept losing to
mining. Meanwhile the armourer counted his skill as if he held the kit, so the orders kept coming, and each new order
opened at the highest escalated offer standing. The first kit he bought (09:22:27, 3 gp) filled **18 orders for
19,736 gp in ten minutes** — the log's fill lines give the same 18 and 19,736 (`data/summary.json`,
`faron_fills_after_kit`). *Fix* (build 259): a class's own trade tool that is gone is bought at the spare price; a
tailor counts only with a sewing kit in hand; an offer standing on the board is read back no higher than four times
the asker's own estimate. *Status:* too early to judge. Build 259 went live at the boot of 13:16 on 26.09, the first
log to say "The wake is on everywhere a bot goes" (its first decision). In the sessions from then to the cut no armour
offer went above 376 gp, against 1,505 gp that morning and at most 655 gp in the three sessions between 10:41 and the
build (`wants_by_session.csv`). But the escrow still rose inside every session — from 3,107 gp at 13:21 to 11,351 gp
at 14:01 — and the longest session after the build ran 45 minutes to its last summary.

---

## 5. Still open

- **The board is lost at every restart** (3.4): about 638,810 gp of escrow and 986,562 gp of goods, at asking price,
  over 85 restarts. Either the board goes into the save, or escrow goes back to the bank and goods to the bank box
  before a planned stop. Nothing does either now.
- **The treasury's cap destroys most of the ground money** (3.5): about 722,254 gp of 1,026,508 gp. The comment on the
  claim says the drain is closed; the cap reopens it whenever the purse is full.
- **Escrow climbs in any long run** (3.3). The cap on `Worth` from build 259 is the first general brake; it has not
  been measured on a long session yet.
- **One purse holds half the money** (3.6), the Architect's, as the Captain's did on 04.09. Nothing makes it spend.
- **Goods nobody buys are still made** (4.20, 3.7): §5 S7 in the decision log, not built.
- **Coin from kills is not in any summary** (2.5), so the one measure of what enters the economy that the project
  trusts is missing its largest source for fighters.

---

## 6. The files

| file | what is in it |
|---|---|
| `extract.py` | regenerates everything in `data/` from a folder of session logs, and refreshes the data block in `index.html` |
| `data/windows.csv` | one row per five-minute summary (1,007): the board as it stood, what happened in the window, the counters, the purses, the treasury, the guilds, the six most wanted and most stocked kinds |
| `data/sessions.csv` | one row per session that printed a summary (117): totals, the board and purses at the end, peaks, the treasury's kept tax, whether the next boot brought the bots back (`board_lost_at_restart`) |
| `data/days.csv` | the sessions added up by the day they started (7) |
| `data/wants_by_session.csv` | per session and kind of thing (2,581 rows): wants opened, raised, filled, lowered, given up; the highest offer per piece and when; coin put down, paid on fills and taken back |
| `data/armour_wants.csv` | every want event for a piece of armour (26,002): time, event, buyer, kind, units, offer per piece, gold, seller |
| `data/summary.json` | every total, peak and estimate quoted above, and the series `index.html` draws |
| `index.html` | the charts; self-contained, no network requests, light and dark |

Columns: a name starting `d_` is the difference of a running total over the window; `*_gp` is gold pieces; times are
the shard's local time as written in the logs. In `windows.csv`, `d_bot_to_bot_gp` is `d_turnover_gp` less
`d_city_bought_gp`.

To regenerate, with the logs folder as the argument (Python 3, standard library only):

```
python extract.py <logs-folder> --until 2026-09-26T15:30
```

`--until` fixes the cut, because the logs of a running shard keep growing; the numbers above were made with
`2026-09-26T15:30`. Without it the script reads everything in the folder. `--out <folder>` writes the data
elsewhere; `--no-page` leaves `index.html` alone.

## Sources

- `Projects/BotAIv2/DECISIONS.md`: §2.4 (market and money invariants), §2.7 (observation rules), §2.8 (what
  survives a restart), §3 (defect classes C1–C12 with every recorded instance), §4.1 (builds 75, 79, 89, 92, 93, 97,
  163, 239, 259), §4.2 (the month by period), §5 (S7), §6.
- `Projects/BotAIv2/BotAuction/README.md`, `BotShops/README.md`, `BotEstate/README.md`, `GUIDE.md`.
- Code comments that carry measurements: `BotAuction/BotAuction.cs` (`Cross`, `StaleMs`, `MaxListings`,
  `MaxWants`, `Purchase`), `BotAuction/BotCity.cs`, `BotShops/BotShops.cs` (`Shelf`), `BotShops/BotStores.cs`,
  `BotPopulation/BotPurse.cs`, `BotDrill/BotSchool.cs`, `BotEstate/BotClaim.cs`, `BotEstate/BotChest.cs`,
  `BotClasses/BotArchitect.cs`, `mindedBots/debugger/BotRevel.cs`.
- The shard's session logs of 18–26 September 2026, read by `extract.py`.
