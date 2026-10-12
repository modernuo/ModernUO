# BotMindAI

**The five crafters think, and nothing else on this shard does.** Patrick's order of 07.09.2026: the
Captain, the Architect, the Sage and the Baron were stood down, and the whole of the Crafter class — five
bodies since 11.09.2026, `Roderic`, `Emeric`, `Ulric`, `Wulfric` and `Alaric` — is held by minds. There are no
unthinking crafters.

This is the first office where thinking is not a supplement to the arithmetic but the whole of how the trade
is run, and the reason is the shape of the work. A crafter's job is a chain: a want on the board, a material
that may not exist yet, a skill that may not be high enough, and a price that has to beat buying the thing
outright. The auction can weigh one link of that. It cannot decide to go and cut wood today so that there
are arrows tomorrow.

The four stood-down minds are **commented, not deleted**, in `BotMinds.Start` — the Baron's line was
commented the same way on 01.09.2026 and putting him back cost exactly one line. Their lessons are still on
disk in `Configuration/bot-minds.json` under their old names.

A fifth thinking thing lives in this assembly and is not one of them — see **`debugger/`** below.

## What a crafter is shown, and why each block is there

Six blocks are added to the state for `crafter` minds only. Each exists because of something that went
wrong without it, and every fact in them is read out of the running shard rather than written down here.

| Block | What it says | The mistake it prevents |
|---|---|---|
| `Bench` | every skill of its class, **which trade on the menu is made with it**, what it is at, what the class targets, and two or three things it could make right now | a mind with Blacksmith 20 announced it would buy cloth and make ringmail **as a tailor** |
| `Board` | the wants: amount, price, **gold already lodged**, deliveries, **how many times the price has been raised**, and **which craft makes it, which trade to take, what skill it needs, what this bot has** | the first thinking crafter read "2 Mind Blast Scrolls, paid for" and announced it would *forge* them |
| `Materials` | each kind of thing on the stalls, how many stalls, the cheapest price | a crafter that can only dig stops when the seam does |
| `Island` | **counted at that moment**: bots with a bare armour slot and how many slots, shooters with nothing to shoot, bots that have not eaten | "work for the good of the island" is an exhortation until it is a number; and every subsystem's own total is cumulative, which reads as *now* and is not |
| `Shelf` | its own stalls: what sold, what it earned, **what has been cut in price and still not sold** | a mind judging its trades on its own forecasts is grading its own homework |
| `Fellows` | what the other three are **holding** right now | four minds given identical state make identical choices |

The board is filtered through `BotShopper.Makeable`, so a want nothing on the island can produce is never
shown as work — and the number of those filtered out is shown instead, which is a different fact worth
having.

**The raises are the point of a board row, not the price.** A want lifted five times with nothing delivered
is the island saying it has been trying to buy that for half an hour, and nothing else in the state carries
that.

**The minds are staggered across the thinking interval.** Four created in the same millisecond ask in the
same second, read a state none of them has changed yet, and choose the same trade — which they did, three
rounds running, with `Fellows` in front of them saying "nothing yet". `BotMind.Stagger` offsets each by a
share of `ThinkEveryMs`, so by the time the second thinks, the first is holding something.

**The state's length is printed in the `Minds:` summary.** Every block costs some of the budget the model
has to think in, and the debugger's prompt reached 19981 characters before anybody noticed the answers
getting worse. A number that grows silently is the one to instrument.

## What the log holds about one decision

`logs/bot-minds.log`, four lines per choice, and each answers a different question:

```
Roderic — chose Tailor, expects 142/min over 60 min (2489ms)
    <the model's own sentence for it>
Roderic — could have taken
    Miner, Woodsman, Tailor, Armoury, Hunter
Roderic — and
    saw: 240gp in pack, 0gp banked, 25% loaded; board: 0 wants it could make now, 9 beyond its skill;
    stalls: 81 listings from 3gp; others: Emeric mine, Ulric mine, Wulfric mine
```

**The second pair exists because a reason without its evidence cannot be checked.** The model's sentence is
its account of itself, which is the thing least worth trusting: when a crafter says "the board is empty so I
will sew", the question the next morning is whether the board really was empty, and until this line there
was no way to answer it without having been watching. `BotMindSight.Brief` writes it from the same objects
the state was built from.

A fifth line appears when the auction preferred its own arithmetic:

```
Emeric — its choice of Tailor was not taken up; the auction is holding mine instead
```

Two thirds of these minds' choices were going unused and the log said only that they were. A bare count
cannot tell "the arithmetic disagreed" from "the mind keeps choosing work that is gone by the time the
auction comes round"; the word after *holding* answers it.

For everything else there is `dial BotMindSight.Dump true` — the next state written out in full, once.


## What the model actually decides

It chooses **one trade** — the next piece of work — and predicts what that work will be worth per minute.
Nothing else.

It does not steer. It never picks a tile to step onto, never picks a target, is not consulted about
health, flight, or what to do when something starts hitting the bot. Those are reflexes that run ten
times a second, and a thing that answers in three seconds cannot be in that loop. This is the same
division the first version of this arrived at, and it is the only one that works on a single graphics
card shared with a running world.

The decision is **offered into the shard's own auction**, where it competes with the arithmetic on equal
terms:

```
BotMindProposer.Propose(bot)  →  BotMinds.Offer(bot)
    the mind's choice names a trade  →  that trade's real proposer is asked for real work
    the work is wrapped in BotMindDeed, which bids the work's own worth × Insistence (1.25)
    BotWill weighs it against every other offer, and refuses it when it deserves to
```

So a thinking bot never does anything the others cannot do. It does one of the same things, chosen for a
different reason — and the shard still refuses the choice when something else is plainly better.

**The bid is not the forecast, and that separation was bought the hard way.** The prediction used to be
both: the figure the auction weighed the offer by *and* the figure the mind was afterwards judged against.
One number serving as promise and wager at once can be won by lying in one direction, and on 25.08.2026 the
models found it — Aldric wrote itself the rule *"never select prowl for profit; always predict zero return
on this shard"*, reasoning aloud that this avoided "being overruled by the shard's arithmetic", and then
bid nought on three trades running. Twenty-four decisions, two taken up. Now the mind's number cannot win it
the work and cannot lose it the work; what it can do is be right, which is the only thing worth measuring.
The mind's weight in the auction is a constant nothing the model says can move.

## The learning loop

1. **Choose.** A free bot is asked every 20 s: the state, **the trades that have work in them right now**,
   its own last six outcomes, its own rules. Answer constrained by a JSON schema — `intent`, `expect`,
   `minutes`, `why`. The menu is read from what the auction's last free review actually collected
   (`BotResolve.Offered`), never re-asked of the proposers: `Propose` leaves a mark in at least one place
   and every proposer counts its own refusals, so a second round of questions would corrupt both. Before
   this, the menu listed trades that merely *existed*, and 45 of Aldric's 48 decisions in one evening named
   a trade with no shopkeeper, no ore and no quarry behind it. **A trade that keeps losing rests** (build 102,
   Patrick's decision of 17.09.2026): three choices of it running that go untaken with the bot at other work
   take it off that mind's own menu for five minutes, five more for each repeat up to half an hour, never below
   three trades; a choice outbid while the bot does that very trade is agreement and forgives it. The guild's
   `gather` and `make` still name any trade with work in it.
2. **Measure.** `BotMindDeed` records the bot's total worth (pack + bank) when the work starts and when it
   ends, over the wall-clock minutes it took. The mind is judged on the number it predicted, measured by
   something that is not the mind.
3. **Reckon.** After a piece of work worth reviewing, a *thinking* call (`think: true`, ~20 s) is asked
   for one short rule. Rules are kept per name, deduplicated by **word overlap ≥ 0.67** — a model rewrites
   the same rule in different words every time, and a check on the opening characters lets every one
   through.
4. **Remember.** Rules go to `Configuration/bot-minds.json`, keyed by name, written the moment one is
   added rather than at shutdown — a shard is usually killed, not stopped.

Bots survive a restart now, but a mind is still tied to a **name** rather than to a body: the bodies are
claimed and renamed on the way in (`CrafterNames`, in order), and the name is what the rules belong to.
Without that, "it learns" is a claim nothing can support.

## Files

| File | What it is |
|---|---|
| `BotMindCore.cs` | The way in. Registers one module and nothing else. |
| `BotMindModule.cs` | Phase `World`, after `Population`, `Will`, `Classes`. |
| `BotMinds.cs` | The two minds, the bodies they claim, the beat, the rule store. |
| `BotMind.cs` | One bot's cycle: choose, settle, review, learn. |
| `BotMindSight.cs` | The world as one bot can see it, written for the model. **Every line is a defect surface.** |
| `BotMindChoice.cs` | The JSON schemas and the reading of answers. |
| `BotMindDeed.cs` | The wrapper: forwards real work, bids the work's own worth, measures the result against the mind's forecast. |
| `BotMindProposer.cs` | The one place a thought becomes an offer. |
| `BotOllama.cs` | The only thing that leaves the game thread. |
| `BotMindLog.cs` | `logs/bot-minds.log` — decisions and reckonings, in order. |
| `BotMindConfig.cs` | what Configuration/bot-mind.json is allowed to say |
| `BotMindTalk.cs` | the one place the thinking bots can hear each other |
| `BotMindClaims.cs` | which mind has claimed which row of the guild board (`take`), for a quarter of an hour, so two makers do not both start the same order (build 68) |

## Configuration

`Distribution/Configuration/bot-mind.json`. **PascalCase keys** — a lower-case key is not an error and not
a warning, it is a value silently left at its default.

```json
{
  "Model": "qwen3.5:9b",
  "Endpoint": "http://127.0.0.1:11434",
  "KeepAlive": "30m",
  "CrafterNames": ["Roderic", "Emeric", "Ulric", "Wulfric"],
  "ThinkEveryMs": 20000,
  "ReviewEveryMs": 180000
}
```

Master switch: `bots.mind.thinking` in `modernuo.json`. The module's own switch, like every other module's,
is `bots.mind.enabled`.

## What was paid for in advance

Facts from the first version's live runs, all of them still true:

- **The prompt is a defect surface.** "Carrying 39 of 215 stones" — the engine's unit for weight — was read
  as cargo, and the first plan that bot ever made was to walk to the market and sell its stones.
- **A goal the bot already stands in ruins the cycle.** The plan closes in the same tick, the prediction is
  measured over zero time, and the expensive review writes a lesson about a day that did not happen.
- **Give the bot what the shard already knows,** or it goes looking for a forge while standing at one.
- **Two models do not live in 12 GB.** One model, `keep_alive` on every request; a bigger model is a slower
  model, and 30B+ falls back to the processor.
- **Thinking tokens are in neither `eval_count` nor `eval_duration`.** A call Ollama measured at 2.6 s took
  19. Time it by the wall clock or not at all.
- **Structured output is not optional.** Asked in words for JSON, the model glues a paragraph in front of
  it about once in twenty answers.
