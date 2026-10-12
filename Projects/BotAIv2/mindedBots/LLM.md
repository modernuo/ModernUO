# Connecting a model to the bots

What is allowed to think, what it is shown, what it must answer, and what it costs. Read from
`mindedBots/*.cs`; where this page and the code disagree, the code is right.

## 1. What thinks and what does not

**Nothing in the population thinks unless it is named.** Every bot chooses by the auction in `BotWill`:
proposers offer work, the arithmetic weighs it, the best offer wins. No model is involved.

A **mind** (`BotMind`) is attached to one crafter by name, from `CrafterNames` in
`Configuration/bot-mind.json`. With the key absent or empty — the live file today — there are no minds, the
beat never starts, and the boot log says `No minds are running`. The stood-down offices (warrior,
architect, sage, baron) are commented lines in `BotMinds.Start`; their name keys are read and do nothing.

A mind chooses a **commitment**, never a step. Every `ThinkEveryMs`, while the bot is Free, Busy or Hunted
and the model is free, it is asked for one trade out of those with work in them *right now*, a forecast in
gold-equivalent per minute, and how many minutes. Walking, fighting, fleeing and digging are reflexes the
model is never consulted about.

The choice is not an order. `BotMindProposer` hands it to `BotMinds.Offer`, which asks that trade's own
proposer for real work, wraps it in `BotMindDeed`, and bids the work's own worth × `Insistence`. The auction
weighs that against every other offer and refuses it when something else is better. The forecast cannot
move the bid; it is checked afterwards against what the work paid, and that comparison is what a lesson is
made from.

**Argus** is the other user of the model and is not a bot: an invisible, blessed observer
(`mindedBots/debugger/`) that samples every bot every two seconds and, every `ReportMs`, asks a model with
thinking on what the worst thing in front of it is, constrained to a schema whose most important entry is
`nothing`. It uses the same transport (`BotOllama`), so `Endpoint`, `Api` and `ApiKey` from `bot-mind.json`
apply to it too; only its model, keep-alive and timeout are its own.

Switches in `modernuo.json`: `bots.mind.thinking` (the assembly), `bots.mind.enabled` (the minds module),
`bots.debugger.enabled` (Argus).

## 2. Ollama

Install Ollama from ollama.com and pull the models:

```
ollama pull qwen3.5:9b        # the crafters
ollama pull deepseek-r1:14b   # Argus
```

If the daemon is down the shard logs `Could not reach the model at ...`, counts a refusal, and the bot
chooses by arithmetic that beat.

Measured on a 4070 Ti (12 GB) with this project's schema and prompts:

| Model | Size | Cold load | Warm answer | Verdict |
|---|---|---|---|---|
| qwen3.5:9b | 6.6 GB | ~34 s | 1.2–1.8 s | the population's model |
| qwen3:14b | ~9 GB | +27 s | 2.7 s plain; 19–26 s with `think` | reckonings only |
| deepseek-r1:14b | ~9 GB | cold each ask | 44–99 s thinking | Argus; tied symptoms into a cause |
| qwen3:4b | — | — | ~1.2 s | incoherent: chose Meditate at the mines |
| above ~14b | >12 GB | — | minutes | spills into system memory; unusable |

**One model at a time.** Two do not fit in 12 GB; loading the second evicts the first and the next question
pays the reload (5–27 s). So `keep_alive` is sent on every request, and Argus runs a short `KeepAlive`
(10 s) so it gives the card back at once. `MostInFlight` is 1: one question at a time across minds and
Argus. Time by the wall clock only: Ollama's `eval_count` and `eval_duration` leave out thinking tokens; a
call Ollama measured at 2.6 s took 19.

### `Configuration/bot-mind.json`

Every key optional. Keys are **PascalCase**; a wrongly cased key is silently left at its default.

| Key | Default | Meaning |
|---|---|---|
| `Model` | `qwen3.5:9b` | as the service names it |
| `Api` | `ollama` | or `openai` (section 3) |
| `ApiKey` | none | bearer token when set |
| `Endpoint` | `http://127.0.0.1:11434` | root of the service |
| `KeepAlive` | `30m` | Ollama only; every request |
| `TimeoutMs` | `120000` | per request |
| `CrafterNames` | `[]` | one mind per name; `[]` = none |
| `WarriorName`, `ArchitectName`, `SageName`, `BaronName` | Aldric, Godric, Cedric, Baldric | read; minds commented out |
| `ThinkEveryMs` | `20000` | how often a free bot is asked |
| `ReviewEveryMs` | `180000` | how often a mind may spend a thinking call on a lesson |
| `ChoiceHoldsMs` | `45000` | how long a choice waits for the auction |
| `MostLessons` | `8` | rules per mind (2 per trade) |
| `Insistence` | `1.25` | multiplier on the work's worth |

`MostInFlight` (1) and `ThinkingMostTokens` (4500, output cap on a thinking call) are dials only.

### `Configuration/bot-debugger.json`

| Key | Default | Meaning |
|---|---|---|
| `Name` | `Argus` | the lead watcher |
| `Model` | `deepseek-r1:14b` | its own, not the population's |
| `KeepAlive` | `10s` | short: it must let go of the card |
| `TimeoutMs` | `420000` | cold load plus a long think |
| `Helpers` | `["Lynceus","Heimdall"]` | raised beside the lead |
| `RobeHue`, `Hues` | `1153`, `[33,99,63]` | robes: lead, then helpers |
| `SampleMs` | `2000` | every bot measured |
| `HoverMs` | `20000` | moves beside somebody |
| `ReportMs` | `600000` | asks what is wrong |
| `ReflectMs` | `1800000` | asks the thinking question |
| `Rows` | `6` | bots described in full per report |
| `FrozenMs`, `ImmortalMs`, `SettledMs` | `90000`, `300000`, `1200000` | frozen; "working" too long; watched long enough |
| `WindowMs`, `MostTouched`, `RestMs` | `120000`, `4`, `300000` | roll-call cadence, bots shaken, rest after |
| `TaxShare` | `0.06` | crown's cut of guild money for prizes; 0 stops it |

### Turning the thinking crafters on and off

The live file is `{"Insistence": 2.0}`: no `CrafterNames`, so no minds run. To switch them on:

```json
{ "CrafterNames": ["Roderic", "Emeric", "Ulric", "Wulfric", "Alaric"] }
```

Each name claims and renames one body of the Crafter class, so `bot-population.json` must raise at least
that many crafters. Minds are staggered across `ThinkEveryMs` so they do not all ask in the same second.
Lessons are kept per name in `Configuration/bot-minds.json`. To switch off, set `"CrafterNames": []` or
remove the key, and restart; an empty array is an instruction, not "unset".

## 3. Other models and services (`"Api": "openai"`)

Anything that speaks the OpenAI chat-completions API works: LM Studio, llama.cpp's server, vLLM,
OpenRouter, OpenAI, or a gateway in front of any of them. Questions, schemas and the reading of answers are
identical; only the envelope changes. The URL is `Endpoint` + `/v1/chat/completions` (`/chat/completions`
if `Endpoint` already ends in `/v1`).

**Sent:** `model`, `stream: false`, `temperature: 0.4`, `messages` (one system, one user),
`response_format: {"type": "json_schema", "json_schema": {"name": "answer", "schema": ...}}`, and
`max_tokens` = `ThinkingMostTokens` on thinking calls only; `ApiKey` as a bearer header. **Not sent:**
`keep_alive`, `think`, `num_ctx`, `num_predict` — whether the model reasons first, and how long it stays
loaded, are the service's decisions. **Read:** `choices[0].message.content`, parsed as JSON.

Local LM Studio:

```json
{
  "Api": "openai",
  "Endpoint": "http://127.0.0.1:1234",
  "Model": "qwen3.5-9b",
  "CrafterNames": ["Roderic", "Emeric"]
}
```

A hosted API:

```json
{
  "Api": "openai",
  "Endpoint": "https://openrouter.ai/api/v1",
  "ApiKey": "sk-...",
  "Model": "qwen/qwen3-14b",
  "TimeoutMs": 60000,
  "CrafterNames": ["Roderic", "Emeric"]
}
```

**A hosted model is paid per token; reckon the rate first.** Per mind: one choice every `ThinkEveryMs`
(20 s) while it has trades to choose between — up to 180 an hour, each carrying the system prompt plus a
state of roughly five thousand characters — and one thinking reckoning at most every `ReviewEveryMs`
(3 min), up to `ThinkingMostTokens` out. Argus adds a report every `ReportMs` (10 min) on a prompt bounded
to 9000 characters, a reflection every `ReflectMs` (30 min), and the marshal's asks. Raise `ThinkEveryMs`
and `ReviewEveryMs` first.

The service must honour `json_schema` with `enum`, `minLength` and `number`: without structured output
about one answer in twenty carries prose before the JSON and is thrown away. The model never sees the
schema, so anything the schema closes must also be said in the text.

## 4. What the model is shown

One system message and one user message (`BotMindSight.System`, `BotMindSight.State`); the answer is
constrained to `BotMindChoice.Schema`.

**The system prompt** has three parts, none situational. The opening: *You are the mind of {name}, a
{trade} living on an Ultima Online shard among {N} other bots ... you choose this bot's next piece of work
and nothing else* — the shard runs the work; the one decision is which trade and what it is worth per
minute; the number cannot win or lose the work; fighting and staying alive are reflexes. Counts are read
from the population, never written down. **Calling** is the office's paragraph; for `crafter`: *YOU ARE
ONE OF THE FOUR CRAFTERS, AND THERE ARE NO OTHERS* (the number is in the text, whatever `CrafterNames`
holds); read the state in this order and stop at the first thing you can do — board, island shortages,
skill, material, selling — with three overriding rules: do not make what has failed to sell, do not stand
at a bench another crafter is at, do not take work for what nothing here can produce. **Charter**: you are
the maker of your guild; `gather`, `make`, `marchx`/`marchy` are prices to your band, not orders, lapsing in
a quarter hour; `expel`/`recruit`, `say`/`take`, `want`/`wantamount`, each with "say none when content".

**The state**, in the order written (crafter-only blocks marked):

- `YOURSELF` — health, stamina, mana %; gold in pack and bank; pack % full by weight **and** how many
  things; what is in hand.
- `WHERE YOU ARE` — x, y, region; tiles from camp; guarded ground; the most dangerous thing within 14
  tiles; own people nearby.
- `GROUND THAT HAS KILLED PEOPLE` — baron only.
- `WHAT YOU CAN MAKE, AND HOW GOOD YOU ARE AT IT` (crafter) — each skill, the trade it maps to, now vs
  target, the top reachable recipes and what the first one eats.
- `THE BOARD` (crafter) — wants grouped by label and price: amount, gold lodged, raises, who took it, which
  craft makes it and whether this bot can; unmakeable wants counted, not shown.
- `THE OTHER CRAFTERS` (crafter) — what each other mind is *holding* now, and any row it has taken.
- `WHAT IS FOR SALE ON THE STALLS` (crafter) — cheapest price and stall count per kind.
- `WHAT THE ISLAND IS SHORT OF, COUNTED THIS MOMENT` (crafter) — bare armour slots, shooters out of
  ammunition, bots not fed, with the craft that answers each.
- `YOUR OWN STALLS` (crafter) — held, sold, earned, cut-and-unsold.
- `WHAT YOUR CHOICES HAVE COME TO` — last 6 outcomes; a rate only when the work ran 30 s or more.
- `WHAT THE OTHERS HAVE SAID`, `WHAT YOU HAVE LEARNED`, `Your band` (members, purse vs hall price, hall,
  ground, orders, what pays lately, who thinks worst of you, the roll).
- `TRADES WITH WORK IN THEM RIGHT NOW` — the menu, one gloss per trade.

A real fragment:

```
WHAT THE ISLAND IS SHORT OF, COUNTED THIS MOMENT
- Armour: 7 of 49 bots have a bare slot, 11 empty slots between them.
- Arrows: every one of the 12 shooters has something to shoot. Nothing wanted here.
- Food: 3 of 49 have not eaten in the last while. Nobody starves here - a meal only quickens recovery and lifts the mood - and food is cooked (the trade called Cook) out of raw meat and flour.

TRADES WITH WORK IN THEM RIGHT NOW
- Miner — dig ore, smelt it into ingots, and put them on the market or in the bank.
- Tailor — buy cloth and sew goods to sell.
```

The state ends with the instruction to choose one trade, give expect, minutes and why, optionally `say`,
and leave the band fields at none and nought. Its length is kept in `BotMindSight.LastChars`.

**The answer schema**, built per ask from the live menu:

| Field | Type | Required |
|---|---|---|
| `intent` | enum of the trades on offer | yes |
| `expect`, `minutes` | number | yes |
| `why` | string, never parsed | yes |
| `say` | string | no |
| `gather`, `make` | enum, `none` first | no |
| `marchx`, `marchy`, `wantamount` | number, 0 for nothing | no |
| `expel`, `recruit`, `take`, `want` | enum, `none` first; absent when the list is empty | no |

Whole numbers are read with `TryGetInt32`, which fails on `10.0`: declare `integer` or read through
`double`. The reckoning answers `{"lesson": string, "keep": boolean}`. Argus answers `kind` (`stuck`,
`loop`, `starved`, `mismatch`, `waste`, `unreachable`, `nothing`), `bot` and `watch` (enums of live
names), `finding`/`evidence`/`cause`/`fix` with minimum lengths, `confidence`, `last` (`first`, `holds`,
`gone`, `unclear`), `probe` (a hand verb) and `at` — all required.

## 5. How to write a prompt for a bot

Each of these cost a session on the live shard.

1. **The wording of the state is a defect surface.** "Carrying 39 of 215 stones" — the engine's unit for
   weight — was read as cargo, and the first plan ever was "go to market and sell my stones".
2. **Never show a goal the bot is already standing on.** The plan closes in the same tick, the forecast is
   measured over zero time, and the expensive review writes a lesson about a day that did not happen.
3. **Give the bot what the shard already knows** — the nearest forge, the best ground for ore — or it
   walks off to "look for a forge" while standing at one.
4. **Constrain with enums, not persuasion.** "The peddler returned nothing 12 seconds ago, it will not
   work again" was read as evidence it would; removing empty trades from the enum worked. A constraint
   cannot be reinterpreted; a sentence can.
5. **A number whose good value is zero must be phrased so zero reads as good.** "0 of 12 shooters have
   nothing to shoot" was read as an alarm; "every one of the 12 shooters has something to shoot" was not.
6. **A number without a scale sets its own.** "49 of 49 have not eaten recently" made the model decide the
   island was starving; nobody starves, food only speeds recovery. Say what the number means.
7. **One phrase must not cover two causes.** "Mining 50: you cannot make anything with this skill yet" was
   said about a gathering skill that makes nothing by design.
8. **Group identical items.** Eight identical leather orders were eight of eight board rows; grouped, the
   prompt shrank from 5582 to 4866 characters and the board became readable.
9. **Identical minds given identical state choose identically.** Show what the others are *holding*, not
   intending, and stagger their clocks.
10. **Measure the length of what was actually sent before editing a prompt.** The marshal's view was cut
    off at the 9000-character limit for weeks; a prompt that grew from 14780 to 19981 characters silently
    strangled a thinking model. `LastChars` is in the `Minds:` line for this.
11. **Below 30 seconds of work nothing is a lesson.** "Expected 250/min, got 0/min" over six seconds is
    arithmetically true and means nothing (`WorthCountingMs`, `WorthReviewingMs`).
12. **Deduplicate lessons by word overlap ≥ 0.67** (`SameLesson`), never by prefix: the model rephrases
    the same rule every review.
13. **The model names a trade; the shard builds the work.** It chooses a commitment — goal, place,
    deadline — never a step; survival and fighting are never the model's.
14. **Always give the schema a `nothing`/`none` option.** An observer without the right to say "all is
    well" invents a defect every time; a hand without a way to stay in the pocket is used every time.

**A template for a new office:**

```
You are the mind of {name}, a {office} on an Ultima Online shard among {N} other bots.
You choose this bot's next piece of work and nothing else; the shard walks, fights and works.
[Calling: what this office is for, in one paragraph. No number that can go stale.]
You may choose only from the list you are given; every entry has real work in it now.
Your forecast cannot win or lose you the work; it is checked afterwards. Predict what you believe.
Answer 'none' for anything you are content to leave as it is.
```

State: the facts as numbers **with their scale**, grouped; what is closed right now and why — as a
sentence *and* as an absence from the enum; the last outcomes, rates only where there is one; the lessons;
the menu with a gloss per entry; one closing sentence saying exactly what to return, and that nothing else
is read. Schema: `intent` as an enum of the live menu; `integer` with `minimum`/`maximum` where a whole
number is needed; `none` first in every optional enum; no optional field whose only value is `none`.

## 6. Where to look when it goes wrong

- **`logs/bot-minds.log`** — opened only when a mind exists. Four lines per choice: `chose X, expects
  N/min over M min (ms)` with the model's sentence; `could have taken` with the menu; `and` with the
  evidence (`saw: ...`); then `its choice of X was not taken up; the auction is holding Y instead` when
  the arithmetic won. Reckonings, rests, barren choices and lessons are here too.
- **`logs/bot-debugger.log`** — every Argus finding with the measurements it was made from underneath.
- **The `Minds:` summary line**, every five minutes in the shard log: per-mind counts (decisions, taken
  up, outbid, rests, barren, idle, over/under, rules), the transport's `asked / answered / refused` with
  decisions and reckonings timed apart, and `the state they read last ran to N characters`.
- **Boot lines**: `N minds are awake on {model} at {endpoint}` or `No minds are running`. `Could not
  reach the model at ...` and `The model answered 404 to a question` are warnings; the bot chose by
  arithmetic that beat.
- **Dump the next full state**: write `dial BotMindSight.Dump true` into `Distribution/argus-in.txt`. The
  next state built goes to `bot-minds.log` in full, once, and the dial resets itself.
- **The door** (`argus-in.txt` in, `argus-out.txt` out): `state` prints the digest Argus is given; `think
  <question>` puts a question to the model with the measurements attached; `bot <name>` one bot's row;
  `dials <word>` and `dial <Class.Name> <value>` read and move any tunable while the shard runs. Nothing
  is written back to the json files.
- **The dashboard** at `http://127.0.0.1:2599`: the Minds tile shows minds awake, reachable or not, the
  model, the endpoint, the last answer in ms and choices answered. An absent thinking layer reads as
  absent, not as zeros.
