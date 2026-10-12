# Politics: inside the guilds and the towns

Patrick's order of 07.10.2026, night: "Elections of the guild head (members vote by the candidate's appeal: kills,
generosity, victories); a conspiracy: the discontented can depose the head; the guild chest with a vote on what we save
for; a town council: the guilds with the best standing in a town get a post (tax collector, captain of the guard) and an
income from it; a tax on an outcast guild; ultimatums and tribute: a strong guild demands tribute from a neighbour instead
of war, the weak pays or fights."

| File | What is in it |
|---|---|
| `BotElection.cs` | the head by vote (a term, a head gone, a head fallen), the members' records it is elected by, and the plot that deposes a head |
| `BotPurpose.cs` | what each chest saves for by the members' vote: hall, house in Britain, widening, war chest, ransom fund or nothing |
| `BotCouncil.cs` | each town's tax collector and captain of the guard, the collector's commission, the captain's wage, the outcasts' surcharge |
| `BotGuardDuty.cs` | the captain's guard as paid work: a watch inside the walls for the town's wage |
| `BotTribute.cs` | ultimatums before a war or a seizure, and tribute paid chest to chest instead |
| `BotPolitics.cs` | the module, its file `bot-politics.json`, the phrases, the page's `politics` object, and the seams to wire at merge |
| `BotPoliticsStore.cs` | offices, records, purposes, councils and tributes across restarts |
| `BotStewardship.cs` | each town's steward (the head of the guild with the best standing there), its coffer, the tax at the counters, the town's regard, deposition, the guard line, the `Stewards:` line, the page's `towns[].steward` and `decrees` |
| `BotStewardshipDecrees.cs` | the decrees: what is lawful now and what it costs, the rule's order, the model's question, what each decree does, Argus's `steward` and `decree` |
| `BotStewardStore.cs` | the stewardships across restarts, `Configuration/bot-steward.json`, the store's registration (no line in `BotCore`) |

## Elections

Every six hours of running time (`BotElection.EveryMs`), at once when the head has left or been deleted, and when the head
falls (not within an hour of the last vote), the guild's three best members by record stand with the head. A record is
renown (the engine's fame, a point per hundred), kills of other guilds' members (5 each), wars the guild won while the
member was in it (10 each) and dues given to the chest (a point per 100gp). A voter under the keep counts gifts twice, a
fighting voter counts deeds one and a half times; a candidate votes for itself; the sitting head has 5 points of standing.
The winner is crowned by `BotGuilds.Crown`: the engine's leader, every member's fealty sworn to it — so the engine's own
daily count (`Guild.CalculateGuildmaster`, by fealty) agrees — and the hall and outpost in its name. Log line
`X elected Y the Class its head over <why>: N of M votes`; speech `politics:elected` / `politics:reelected`; event kind
`politics`.

## The plot

Every ten minutes, not at war and not within an hour of a vote, members are asked whether they are discontented: the guild
lost two wars in a row; or the member is under the keep while the chest holds the rich mark (half the hall's price) and
has paid for nothing for three hours under this head; or (when ties are wired) it thinks −30 or worse of the head. At 40 %
they call a vote of no confidence; the discontented vote against, and so does a content member to whom a rival appeals a
quarter more than the head. Over half carries it: an election at once without the deposed head, who stays a member, and the
new head is spared another vote for three hours (`GraceMs`).

## The chest's purpose

Every two hours, when the goal is reached and when it stops applying, the head proposes the first of: the hall (none), a
war chest of 3000gp (at war or paying tribute), the house in Britain (wished for), a ransom fund (when wired), a widening
(full), nothing. A member votes the hall while there is none, the war chest if it fights at war, the ransom if any, nothing
if it is under the keep itself, else the head's proposal. The result replaces `BotDues.Goal` — what the chest keeps back
from the everyday draws, at most three quarters of what it holds. No coin moves by the vote.

## The towns' councils

Every two hours, in each town, among the guilds with a task done there and not put out of it, the cheapest standing
(`BotBurgh` factor, then tasks done) collects the taxes and the next captains the guard. At the term's end the treasury's
tax take over the term (`BotCity.Taxed`) is shared evenly among the towns with a collector and each collector's chest is
paid a tenth of its town's share, out of the purse (`Escrow` + `Disbursed`; refused and counted when the purse cannot).
The captain's members are offered `guard-duty`: five minutes inside the walls for 20gp, held from the purse when taken and
paid into the account when stood, at most two at once and 400gp a town a term. A guild put out of the town, or thought
−60 of by a post-holder, is the town's outcast: ×1.05 at its counters, and that part of the bill is taxed into the
treasury instead of leaving the world.

## Ultimatums and tribute

At the duke's word of war, and when a guild would seize a held square, a would-be aggressor with twice the other's might
demands a quarter of the weak chest a day (240–4800gp) for 24 hours. The weak pays if its chest holds the first hourly
instalment; the first is paid at once, chest to chest (`BotChest.Pay`), both opinions are settled, and while it stands
neither may declare on the other (`BotWar.MayDeclare`) and the strong seizes none of the weak's ground. It ends after 24
hours, when the strong falls under 1.5 times the weak's might, or when an instalment cannot be paid — then the strong thinks
30 worse of the weak and the grievance takes its course. A refusal (a chest that cannot pay) lets the war go on by its own
rules, and no new ultimatum passes between the pair for six hours. Ultimatums only ever stand in front of a war that the
existing rules had already allowed, so they can lower the wars per hour and never raise them.

## Where it is counted

- `Guilds:` ends with the elections clause (held by cause, new heads and re-elected, votes, plots by reason, called,
  carried, failed, the heads with their votes and age).
- `Treasuries:` carries the purposes clause (votes by aim, overruled, reached, what each chest saves for now).
- `Diplomacy:` carries the councils clause after the towns' (terms, posts, commissions, watches, outcasts and their tax)
  and the tribute clause after the meetings' (sent, paid, refused, spared, instalments and gold, endings, refusals of a
  declaration).
- `/api/state` → `guilds[].politics`: `elected`, `electedHead`, `votes`, `voters`, `electedMin`, `elections`, `lostInRow`,
  `discontent`, `purpose`, `purposePrice`, `posts[]`, `outcastIn[]`, `tribute[]`.

## Seams left for other folders

`BotPolitics.TiesRegard` (`BotTies.Regard(a, b)`), `BotPolitics.Veteran` (`BotTraits.Veteran(bot)`) and
`BotPolitics.Ransom` (a guild's jailed members' ransom) are null until wired at merge; null reads as nought.

## The towns' stewards (07.10.2026, steward20)

Patrick, approved: "A town elects a steward (the head of the guild with the best standing in the town, or a thinking role like the
duke). Real levers, each a decree announced in town and logged … The steward answers: an empty purse or a falling town regard
deposes it." The class is `BotStewardship` (the estate's `BotSteward`, who raises halls, had the name first).

- **Who.** In each town reached from home, the guild with the best `BotBurgh` standing there (cheapest, then most tasks done,
  then fewest failed — the council's order), with a task done, not put out, not The Shadow, with a head. Chosen again every 2 h
  (`ChooseMs`), and at once when the guild is gone, headless, put out or barred, when its head falls, or when it is deposed; a
  deposed guild may not hold that town again for 6 h (`DeposedMs`). The council's posts stay; the steward hires the captain.
- **The coffer** is treasury coin held for the town (`BotCity.Escrow`, so `City:` counts it as held; kept across restarts and
  held again at the boot). It fills by an allowance of 100gp an hour while the treasury holds over 5000 (`Allowance`,
  `CityKeep`, up to `CofferMost` 5000) and by the town's tax. Every 15 minutes (`EveryMs`) the steward gives at most one decree.
- **The decrees and their honest cost.** *Tax* ±1–3 points within −3 %…+5 % (`TaxFloor`, `TaxCeiling`, once an hour): a
  factor on every bot's bill at the town's counters (`BotBurgh.Factor` × `Levy`); the part a raise adds goes into the treasury
  as tax and is held for the coffer, the part a cut takes off is paid out of the coffer (suspended under 20gp). *Captain*:
  the council's captaincy given to a guild the steward chooses for 2 h, and 200gp handed back to the purse as that town's
  extra wage budget, paid per watch stood (`BotCouncil.Appoint`). *Bounty*: +150gp on a head posted in the town at large for a
  quarter of an hour (once an hour a head, up to the crime folder's cap +1000), the gold handed to the purse that pays the
  bounty (`BotCrime.Raise`). *Militia*: when an invasion of the town has stood 5 minutes with no company, 300gp into the
  raid's own hold as head money (`BotSeasons.Hold`, so the `Events:` ledger balances) and the call made again. *Bar*: an
  outcast guild of the council, none of whose members live in the town, not seated by it, while a captain keeps the line —
  24 h, 100gp to the guard's watches; its members' work in the town is refused (`BotAppraisal`) and their walks in are turned
  back at the line (`BotWalk`, `Stalled`, never written against the ground). *Holiday*: the fair called now in the town
  (`BotFair.Asked` + `CallNow`), 8 h after any fair and 24 h after the town's last, 250gp held until it is announced and then
  spent on the feast (gone like any NPC purchase, counted), given back if the fair went elsewhere. *Caravan*: when the town's
  counters are short of a kind the bots found missing, 300gp handed to the purse and the caravan posted for the town
  (`BotCaravans.For` + `CallNow`); taken back if nothing was posted within a minute.
- **The rule's order:** militia; a tax cut when the regard sank three rounds by 1.5 under a tax; a bounty; a captain (none,
  at war with or loathed by the steward's guild, or wages spent with heads at large or an invasion); a bar; a tax raise when
  the coffer is under 100 and the allowance stopped or a decree could not be paid; a caravan; a holiday (regard under 45 or a
  rich coffer); a tax cut when the coffer is over 1200. **The model** (`BotStewardship.Ask`, filled by
  `mindedBots/debugger/BotStewardArgus` when the debugger runs) is shown every lawful decree, the rule's first, with an
  enumeration of exactly their ids plus `none`, when the rule has one or once an hour; an unreadable answer leaves it to the rule.
- **The regard** is 50 with the steward's guild's prices there taken off (×1.05 is −10) plus the mood (±40): a raise −2 a
  point, a cut +2, militia +5, holiday +6 (when announced), caravan +3 (when posted), bounty/captain/bar +2; every round a tax
  over nought −0.15 a point, each head at large posted in the town −0.5 (four at most), an unanswered invasion −2, a coffer
  under the floor −1. **Deposed** when the coffer stands under 50 for an hour, or when the regard has not risen once in 2 h and
  is 4 lower. A point of tax is borne; two depose within two hours unless the steward does something the town likes.
- **Counted** on the `Stewards:` line every 5 minutes (each town's steward, tax, coffer, regard and trend; chosen, changed,
  deposed by cause; rounds, idle, decrees by kind and by rule/model/Argus, refused; the model's asks; every flow of coin and
  what the coffers hold now; bars, refused work and walks turned back); a line per decree (`X of G, T's steward, decreed …
  by the rule: why; the coffer holds …`), per deposition, per choice, per taxed round's takings. `/api/state` →
  `towns[].steward` (`guild`, `head`, `minutes`, `regard`, `trend`, `coffer`, `pending`, `tax`, `allowance`, `takings`,
  `bars[]`) and `towns[].decrees[]`. Speech: the steward or one of its guild in the town says "By the steward's decree: …"
  (else the crier for a loud one), and every decree is a `politics` event on the stream.
- **Argus:** `do steward [<town>]`, `do decree <town> tax +2 | captain [<guild>] | bounty <bot> | militia | bar <guild> |
  holiday | caravan [<town>]` (by hand only). **Dials:** `BotStewardship.Running`, `EveryMs`, `TaxFloor`, `TaxCeiling`,
  `TaxMostStep`, `Allowance`, `CityKeep`, `Floor`, `Rich`, `PoorMs`, `FallMs`, `FallLeast`, `TaxPress`, `ModelChooses`,
  `WalkerBars` and the decrees' prices; `BotStewardArgus.Running`.
