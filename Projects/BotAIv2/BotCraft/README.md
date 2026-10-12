# Craft: a producer that depends on nobody

Five trades that **create** rather than extract: the smith at a forge, the tailor out of leather or cloth,
the alchemist over a bottle, the fletcher out of shafts and feathers, and the cook at any fire. Each is a
proposer that decides whether there is work, and a deed that walks to the place, swings until something comes
of it, and puts the result into the order it was made for or onto the market. It began as buy cloth → sew →
put it out, which is still the shape of every one of them.

| File | What is in it |
|---|---|
| `BotThread.cs` | sewing: what can be made from a material, and the attempt itself |
| `BotSew.cs` | an obligation with three legs: shop → work → counter; a kit worn through after the first attempt ends the batch at the counter |
| `BotTailor.cs` | the proposer: whoever is carrying a sewing kit. Orders are leather; spec work is hide and the pack's own cloth (the weaver's) — cloth is not bought over a counter for it since 29.09.2026 (`BuysCloth` off), after 2080 cloth went into oilcloth nobody bought |
| `BotCraftConfig.cs` | `Configuration/bot-craft.json` |
| `BotCraftModule.cs` | module, phase `World`, requires `Classes`, `Will`, `Shops` |
| `BotAlchemist.cs` | offers the mortar to anybody carrying one, and offers the board's orders first |
| `BotAnvil.cs` | what the smith's trade knows that no other trade does: its system, its skill, its hammer, its metal, and the one thing about black |
| `BotBake.cs` | one turn at the skillet: raw meat in the pack becomes suppers, and what the cook does not keep goes out to the market; a hearth found cold is rested for every cook |
| `BotBrew.cs` | brewing: buy the glass if it is short of it, work the mortar, and hand the bottles to whoever put the money down; a mortar worn through takes what was brewed to market |
| `BotCook.cs` | offers a turn at the skillet to anybody carrying one and some meat |
| `BotCraftwork.cs` | the part of making things that is the same whatever is being made |
| `BotFlask.cs` | brewing: what a potion is made of, and the swing that makes it |
| `BotFletch.cs` | making arrows: buy the wood if it is short of it, cut the shafts, feather them, and hand them to whoever put the money down |
| `BotFletcher.cs` | offers a bot with a fletching tool and a handful of feathers a turn at making arrows, and offers it the board's orders first |
| `BotFletching.cs` | what a fletcher needs to know: the craft system, the tool, and the two-step chain that turns a log and a feather into an arrow |
| `BotForge.cs` | beating iron into something, at a forge, and handing it to whoever asked for it; a forge whose anvil the engine refuses rests for every smith (`BotGround.Unfit`) |
| `BotMeal.cs` | a bot eating a cooked meal: what it does to the bot, and for how long |
| `BotOven.cs` | cooking: what a meal is made of, and who can make one |
| `BotSmith.cs` | offers a bot with a hammer and some metal a turn at an anvil, and offers it the board's orders first; remembers the metal the dearest order it passed over for want of iron would take (`Short`, 03.10.2026), which the metal board orders and the porter keeps |
| `BotCraftEar.cs` | what the craft system last told each bot, and how often it has said each thing, heard through the `CraftItem.Said` engine patch |
| `BotTinker.cs` | Making a tool out of iron, wherever the bot is standing, and handing it to whoever asked for it or to the market. |
| `BotTinkerer.cs` | Offers a bot with tinker's tools and some iron a stint at making the tool the island is shortest of, and offers it the board's orders first. |
| `BotTinkering.cs` | What a tinker needs to know: the craft system, the tool, and which of the things it can make are the tools the rest of the population wears out. |
| `BotTutor.cs` | The first steps of a trade, bought from a shopkeeper who knows it — the engine's own teaching, paid for in gold. |
| `BotAdvance.cs` | a brewer's glass and herbs for a draught somebody asked for, lent out of that want's own reservation at the counter and settled at the hand-over; what is not settled the brewer pays back (03.10.2026) |
| `BotCraftFirst.cs` | a crafter takes the work asked of it before it digs (the auction's rule is in `BotWill`), and is offered its own material out of its bank box for an order it passed over (03.10.2026) |
| `BotDraw.cs` | the walk to the bank box for that material |

---

## Why the tailor and not the smith

**A tailor needs no place.** A smith wants a forge and an anvil, so a smith without a workshop is a bot with an
opinion about metal; a sewing kit works wherever the bot is standing. That is the whole reason the first
crafting chain is cloth and not ore.

And the second reason: **a tailor depends on nobody.** Cloth is on a shelf in town, and there is no waiting for
miners. That is the answer to the question this chain was written for — what does a producer do on a shard where
nothing has been produced yet.

---

## The work is the engine's

Everything goes through the shard's own `CraftSystem` — the same call a player's craft window makes. The skill
check, the failure, the material burnt on a bad attempt and the item that appears are all the engine's. That is
what makes the gain real: a tailor here learns tailoring **by tailoring**, not by being credited.

**Attempts are not output.** Crafting runs on its own timer and fails often at the edge of a skill, so what is
counted is what is in the pack afterwards, not how many times the bot swung. In v1 the counter counted attempts
and reported "44 made" about a smith that had produced nothing in three minutes.

**What to sew** is chosen the way anybody learning a trade chooses: **the hardest thing that still comes out
reliably** — five points below its own skill. Difficulty is where the growth comes from; an easy piece teaches
nothing. Recipes needing a second material are skipped: a bot with cloth is a bot with cloth.

---

## Why this pays better than digging

Because **Tailoring is on the crafter's own vector** and Mining is not. With the vector rule
(`BotYield.StrayFactor`) the same tenth of a point counts at full rate for sewing and at a third for digging.
The bid for a trip to the needle is 55 a minute against 45 at the mine, and that is not a nudge: it is the same
statement said as a number.

Cloth costs money and the finished piece is worth more — in skill certainly, in coin if the market agrees.
`GoldPerPiece` (12) is a stand-in of the same kind as the price of an ingot: it **opens a stall**, and from
there the price is the market's business.

---

## Crafters first, and draughts on a want's advance (03.10.2026)

Patrick, 03.10: flow9's proposals 3 and 4, "do it". Measured on build 396 (04:19–12:47, 210 bots).

**Crafters make what is asked before they dig.** The eighteen crafters spent 44 % of their finished minutes digging and
7 % making things; mining paid them 8gp an hour in coin and their ingots peddled 6 % of their gold. The auction never put
digging over an order a crafter could make — a forge or a needle for an order opens at hundreds a minute — but it put it
over everything around one: a restock of the crafter's own material was the runner-up of 111 of its 1,281 mining takes,
a delivery of its own pieces 25, and smiths passed orders over for want of iron 6,746 times with their own ingots in their
bank boxes. So for a Crafter or an Architect (never a gatherer, whose living is the dig) a winning dig or prospect gives
way to the best *asked* work worth anything (`BotCraftFirst.Asked`): a piece for an order, a delivery to a want or the
meeting with a carrier bringing its own, a restock of its trade's material (metal, hide, cloth, feathers, wood) — also a
walk to a bot's shop for its own want of one — and its own material out of the bank box. Prices are not touched; nothing
else is reordered; work in hand is not taken away. A craft that passes an order over for want of material remembers it for
ten minutes (`BotCraftFirst.Short`: the smith's metal, the tailor's leather, the fletcher's feathers), and a crafter whose
bank box holds it is offered the walk to the nearest bank (`BotDraw`, claimed at 120 a minute like a spare tool's trip);
the memory is let go when the walk reaches the bank, whether or not the pack could take anything (counted apart), so a full
pack is not walked back to the box.
Counted in `Roles:` (the crafters' clause: making against digging by trade, what took a dig's place, the walks to the box).

**A potion want's reservation pays the brewer's glass and herbs.** 1,946 looks at the mortar were brewers whose purse would
not carry the glass for the herbs they held (the fattest 24gp), 53,006 held neither, 23 rounds in eight hours were brewed for
an order, and potion lines took a median 9.3 minutes (a fifth failed). A brewer with the Alchemy for a draught somebody wants
— the engine's chance at 80 % or better, under the draught's cap, no loan of its own standing — takes a want older than
three minutes (a carrier holding the draughts goes first; a brewer looks once every `BotAdvance.LookMs`) when one counter in reach sells what the pack lacks and that costs
no more than the draughts' offer. At the counter the buyer's account lends the money (`BotAdvance.Lend`, drawn on the want's
own reservation the way the meeting draws on it), the brewer buys and gives back at once what the counter did not take, and
the round holds its units on the want while it brews (`BotBrew` is an `IBotWantCarrier` while funded). The want then holds
that much less (`BotMarket.Holds`), the brewer's kit does not keep the draughts it owes (`BotUnload.Keeps`), and at the
hand-over the advance comes off the bill (`BotHandover.Exchange`). A loan whose want is gone, shrunk, or not carried for
twenty minutes is called and paid back out of the brewer's free money as soon as it has any. Not saved across a restart.
Counted in `Needs:` (after the brew clause) and `The market:` (gold settled out of an advance).

---

## What to check with a client

1. `Craft ready: a tailor buys 20 cloth at a time ...` at startup.
2. `took on sew` from the crafter, then `bought 20 Cloth from ... for Ngp`, then
   `finished sew: N Item in M attempts, N put out to sell`.
3. The `Market` tab in `[bots` — the tailor's pieces should appear beside the miners' ingots.
4. The `vector` column for the crafter should be **rising**.

---

## Rough edges

**One material and one trade.** Leather, smithing and "ingots → object" are the next step, and it is the same
shape: it will have a buyer among the bots, because the input of one trade becomes the output of another.

**Failures burn cloth**, which is right, but the bid (55/min) does not account for them — the ledger will
correct the estimate once it has measured the real output.

**It compiles.** The engine surface was read out of the fork and confirmed by the compiler:
`DefTailoring.CraftSystem`, `CraftSystem.CraftItems` (`List<CraftItem>`),
`CraftItem.Resources/Skills/ItemType/Craft(from, system, typeRes, tool)`, `CraftRes.ItemType/Amount`,
`CraftSkill.SkillToMake/MinSkill`, `SewingKit : BaseTool`, `Cloth`.
