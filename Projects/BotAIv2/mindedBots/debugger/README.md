# The Debugger

One invisible bot in a white robe that appears beside the others, watches, and writes down what is wrong.

It is not one of the population. It has no work, no wage, no boredom, no contentment, no class and no
opinion about what anybody should be doing. It cannot be seen, cannot be hurt, cannot hurt anything, and
does not stand in anybody's way. All it does is measure, ask a local model what it makes of the
measurements, and write both into `logs/bot-debugger.log`.

## Why it is not a `BotMobile`

Everything the population does to a bot — the clock, the auction, the ladder, the urges, the kit, the
revival — is keyed on that one type. A debugger derived from it would be raised, dressed, asked what it
wanted to do, counted in every census, and would appear in its own reports as one of the subjects.

**An observer that shows up in its own measurements is not an observer.** So the body derives from
`PlayerMobile` directly, nothing in BotAIv2 has any way to reach it, and every count it takes is a count of
the population rather than of the population plus itself.

## The three properties that make it safe, and the engine facts behind them

| Wanted | How | The engine's own rule |
|---|---|---|
| Nobody but the owner sees it | `Hidden = true`, `AccessLevel = Administrator` | `Mobile.CanSee(Mobile)` passes a hidden mobile only to a viewer above Player whose level is at least the hidden one's. Counsellors, game masters and seers therefore do not see it either. |
| It never blocks anybody | the same two flags | `CanMoveOver` in the fork's movement implementation passes a mobile that is hidden and above Player. A bot walks straight through the tile it is standing on. |
| It is outside every fight, both ways | `Blessed = true` | `Mobile.CanBeHarmful` refuses on either party being blessed. There is no combat code in this folder at all; the flag is what makes "it is not aggressive" a property of the engine rather than a promise made by this assembly. |

`PlayerMobile.OnAccessLevelChanged` also sets `IgnoreMobiles` from the access level, which closes the other
direction: it does not have to path around anybody either. It never walks in any case — it teleports.

**Why teleporting rather than walking.** An observer that walked would spend its life in the same roads,
doors and crowded doorways it is watching for, and would be subject to them. The one bot whose job is to
notice that nobody can get out of a yard must not be able to get stuck in one. It also has to be able to be
beside a bot on the far side of the map inside a second, because what is worth watching changes faster than
anything can walk.

## The three clocks

They are three different questions, not one question at three speeds.

| Every | What happens | Cost |
|---|---|---|
| 2s | Every bot is sampled into a `BotWatch`. This is the only thing here that produces facts. | one pass over the roll |
| 20s | It goes and stands beside whoever most wants looking at. | nothing |
| 2m | The model is asked what the worst thing in front of it is. No thinking. | a few seconds of the model's one slot |
| 15m | The model is asked, with thinking on, what all the findings have in common. | up to a minute and a half of that slot |

**It shares one slot with the three minds and gets no priority.** There is one graphics card and one model
on it, and while a thinking call runs nothing else can be asked anything — fifty-eight and ninety-nine
seconds, measured, on this card. So the debugger asks only when the slot is free, and holds it long only
four times an hour. A watcher that starved the population it was watching would change the thing it is
measuring, which is the one failure a watcher may not have.

## What it measures, and why none of it is read off the bot's own stamps

A bot carries several tick stamps — when it took its work on, when its work last said anything but
"working". Every one of them is unusable for this purpose the moment it has not been set: on some hosts the
tick counter is the machine's uptime and starts enormous, so an unseeded stamp does not read as "never", it
reads as "eleven days". A debugger built on those would report a population frozen since before the shard
started.

So every duration here is of the form **"for as long as I have been watching"**, measured by `BotWatch` on
its own clock, and the prompt says so in those words.

| Measure | What it catches |
|---|---|
| **Frozen** — standing on one tile while its own journey wants it elsewhere | the bot that cannot take a step and is counted as busy |
| **Silent work** — the undertaking has answered "working, here" and nothing else | the shard's one unjudged answer. Three tailors once held a bench for two hours on a craft lock the engine never released, and every summary counted them as working |
| **Bouncing** — undertakings that ended inside 15s, counted per trade by name | the take-and-drop loop: a proposer offering work it cannot do |
| **Refused roads** | ground with no way out of it |
| **Given up** — the journey has decided it will never reach anything | the same, as the journey sees it |
| **Nothing worth doing** — barren minutes | a bot the whole shard has no work for |
| **Ghosts** — dead and not back up | a revival that is refusing silently |
| **Development** — trade progress now against when first seen | the actual question: is this population becoming anything |
| **Money** — worth now against when first seen, pack and bank apart from each other | the other half of it |

Each of those is reported as a count **with its denominator**, and the population census names every case
with no bucket called "other". Both rules are this shard's, and both were bought: a count with no
denominator cannot say whether nought means "it never happens" or "nobody ever got as far as the check", and
while the population summary had a default branch, an economy working perfectly reported itself as eighteen
bots walking in circles.

## What it is told

`BotDebugSight.System` is the standing instruction, and it is long on purpose. A mind choosing between
trades gets a short prompt because everything situational belongs in the state; the debugger's task is not
situational. It is to recognise the shapes this shard's defects come in, and those shapes cost an evening
apiece to learn. A model that has to rediscover "a hard zero in a summary is two thresholds disagreeing, not
broken code" will spend every report rediscovering it.

So it is taught the world, the vocabulary (rungs, proposers, undertakings, journeys, urges, the auction),
the facts that change how a number reads — towns forbid fighting, a bot pays out of its pack, the roam bound
means a distant shop is never even proposed — and ten named defect shapes, every one of which has actually
happened here.

It is also given, verbatim, **every line the shard writes about itself**: each subsystem's own `Describe()`,
gathered by reflection rather than from a list in this folder. A list would be right on the day it was
written and silently short by one the first time somebody adds a folder — and a debugger that has never
heard of a subsystem will never find a defect in it while looking exactly like one that checked.

## What comes back

Constrained by a JSON schema, like everything else asked of a model here — a constraint the sampler enforces
cannot be reinterpreted, and a sentence in a prompt always can.

- `kind` — one of `stuck`, `loop`, `starved`, `mismatch`, `waste`, `unreachable`, `nothing`.
  **`nothing` is the most important entry on that list.** A watcher with no way to say "the population is
  fine this minute" will invent a defect every time it is asked.
- `bot` and `watch` — constrained to the names of bots that actually exist. Asked in words, a model
  eventually answers "the miner" or a name off an earlier report, and every one of those is a finding that
  cannot be looked up.
- `finding`, `evidence`, `cause`, `fix`, `confidence`.
- `last` — whether what it claimed the time before still stands: `holds`, `gone`, `unclear`, `first`. A
  watcher that never revisits its own claims produces a log of confident paragraphs with no way to tell the
  true ones from the rest.

**Two answers are turned away before anything is filed, believed or raised: a label, and an echo.** A label is
a finding with no space in it, or with a run of more than 32 characters without one; an echo is word for word
the claim a squad-mate, or this watcher, made last. Neither becomes the watcher's last claim, so neither reaches
the others' briefs. Both are counted (`Labels`, `Echoes`, per watcher and in all). On the evening of 14.09.2026
every one of twenty-six findings in ninety minutes was the same run-together token about five different bots:
the prompt had quoted "SameTwoTiles" as its example of what a finding is not, beside example sentences with bot
names and coordinates; the schema's minimum length made the model pad the word instead of writing a sentence;
and the long memory merged every repetition as a fresh sighting, so "SameTwoTiles", found 102 times, was the
strongest belief recited into every prompt. The examples now carry no label, name or number, labels are never
believed or recited, and the ones already held are dropped at load with a log line each.

The reflection asks a different question and has one field the finding does not: **`wrong`** — what would
show this to be wrong, or what to measure next to tell. A conjecture that names nothing which could falsify
it is not a conjecture, and a log full of those is a log that gets acted on and never checked.

## The log

`logs/bot-debugger.log`, its own file, beside the shard's session log and stamped in local time so the two
can be read together.

Every entry is written with **the measurements it was made from, underneath it, unedited**. The model's
sentence is labelled a conjecture; the digest is not. Read on its own, a confident paragraph about a defect
is indistinguishable from a true one; read beside the numbers it can be checked in a minute. That is the
whole reason this is worth running.

## Configuration

`Distribution/Configuration/bot-debugger.json` — **PascalCase keys**, and that is not a style question: a
lower-case key is not an error and not a warning, it is a value silently left at its default. An empty file
means "keep the numbers the code chose", which is what is written on the first boot.

```json
{
  "Name": "Argus",
  "RobeHue": 1153,
  "SampleMs": 2000,
  "HoverMs": 20000,
  "ReportMs": 600000,
  "ReflectMs": 1800000,
  "Rows": 6,
  "FrozenMs": 90000,
  "ImmortalMs": 300000,
  "SettledMs": 1200000
}
```

Switch: `bots.debugger.enabled` in `modernuo.json`. It is on by default, deliberately — a watcher that has
to be remembered is a watcher that is off on the night something goes wrong.

## The door, and the dials behind it

The shard is headless, so its console cannot be typed into. `BotConsole` is the way in instead: write a line
into `Distribution/argus-in.txt`, read the answer out of `Distribution/argus-out.txt` a couple of seconds
later. The input file is emptied as it is read, which is the whole of the protocol — what is in it is what
has not been answered yet.

| Word | What it does |
|---|---|
| `state` | the whole digest, the same text the model is given |
| `bot <name>`, `idle`, `trades`, `fighting`, `rollcall` | one bot, the loiterers, the trade table, the combat table, the last roll-call |
| `memory`, `note <text>` | what it remembers; a remark from a person, filed as one |
| `think <question>` | puts the question to the model with the measurements attached |
| `hands`, `do <verb> <target>` | the bounded verbs of `BotHand`, and their use |
| `dials <word>` | every dial whose `Class.Name` contains the word, with its live value |
| `dial <Class.Name> <value>` | moves one. `reset` puts it back. Bare name reads it |

Four hand verbs exist to put the shard into a known state on purpose and watch what answers it: `do jam <bot>`
fills a pack to the engine's item cap, `do trip <bot> <kind>` puts in as many failures of one kind of work as trip
`BotBreaker`, `do arm <bot>` puts a vanquishing copy of the bot's birth weapon in its pack so that the re-arm can be
watched wielding the better one (build 56), and `do breaks` lists the rests standing. `Projects/BotAIv2/scenarios.py` drives them through this
door after a build and checks the log for the lines that should follow; a state no verb can make yet is printed
as not scripted rather than left out.

**Dials are `BotDials`, and they are the reason a shard no longer has to be restarted to try a number.**
Every `public static` property of a simple type in either bot assembly is reachable — 561 of them, found by
reflection rather than from a list, so a new subsystem's numbers are there the day it is written. A change
takes effect on the living object at once and lasts until the shard stops.

Three things about it are deliberate:

- **Nothing is written to `Configuration/bot-*.json`.** A tool that edited the files could make a shard that
  boots differently from the one anybody tested, and a wrongly-cased key is ignored in silence here. Keeping
  a value means writing it in by hand; `dials changed` prints the list to copy from.
- **The model cannot reach it.** No dial verb is in `BotHand`. An observer that can move the thresholds it
  judges against negotiates with the shard instead of measuring it.
- **Every change is journalled to `logs/bot-dials.log` before it happens, and refused if that write fails** —
  the same order as the hand's log, for the same reason. What was moved is also said at the foot of every
  digest, so the model that reads the numbers afterwards knows a hand was on them.

## Files

| File | What it is |
|---|---|
| `BotDebugCore.cs` | the way in: one `Configure`, one module registered, nothing else |
| `BotDebugModule.cs` | phase `World`, after `Population`. Requires nothing else — a population that decides nothing is exactly the state somebody would want a debugger for |
| `BotDebugger.cs` | the body: hidden, blessed, robed, and it teleports |
| `BotWatch.cs` | one bot as the debugger has seen it. Every measurement lives here |
| `BotVigil.cs` | the three clocks, the census, the choosing of whom to stand beside, and the two questions |
| `BotDebugSight.cs` | what it is told. The whole of its judgement, and every line a defect surface |
| `BotDebugNote.cs` | the schemas and the reading of answers |
| `BotAudit.cs` | the roll-call: three questions to every bot, every two minutes |
| `BotHail.cs` | being spoken to in the world, and answering there |
| `BotHand.cs` | the ten verbs it may use, and the journal that makes them safe; the by-hand verbs no mind is offered, `roads [<x> <y> ...]` (the road map from home and the steps of road to any tile, build 63) and `peril <x> <y>` among them — what the death map and the quadrant record hold about a place (build 50), and `arm <bot>`, a better copy of the bot's own weapon for the re-arm to wield (build 56) |
| `BotConsole.cs` | the door: `argus-in.txt` in, `argus-out.txt` out |
| `BotDials.cs` | every tunable number in both assemblies, read and moved while the shard runs |
| `BotRevel.cs` | the revels: what is declared, what it pays, the crown's purse and the tax that fills it |
| `BotMarshal.cs` | the marshal of events, the fourth watcher: asked every ten minutes for a revel, a camp, a tournament, an errand on the board, a standing order, a bounty, a price on a head or a fair, and carries it out with the verbs the door already had; while it stands the revels are its (build 94); from build 102 its answer's form is one shape per event open now, each with only its fields, its words from lists and its numbers whole and in bounds |
| `BotWaves.cs` | something to fight when a hunt is called into an empty field: waves that grow, and clear away after |
| `BotDebugMemory.cs` | what it carries between reports |
| `BotDebugLog.cs` | `logs/bot-debugger.log` |
| `BotDebugConfig.cs` | `Configuration/bot-debugger.json` |

---

## Revels, waves, and the crown's purse

Added 08→09.09.2026. The watcher already read the whole shard every ten minutes and formed an opinion about
it; these turn that opinion into something that happens.

**A revel** (`BotRevel`) makes one trade worth three times as much for twelve minutes and puts a prize behind
it. It invents no mechanism: bots know nothing of events, and what a revel moves is the price of a kind of
work — `BotAppraisal.Revelry`, a factor competing honestly with crowding and caution. A revel nobody enters
pays nobody, which is itself a measurement.

**A revel cannot name prowl any more** (`BotRevel.Trades`, 14.09.2026). A prowl is a walk to wherever a fight might
be and pays in the fights it finds, not in goods. At 20:35 that evening the watchers tripled it because it "hasn't
produced any goods or skills", which is true of every prowl by design; companies went looking across the whole map,
found plague beasts north-east of Britain, and twenty-eight bots died in that field in twenty minutes. A hunt revel
with a camp puts the fighting where it was chosen to be.

**It is two contests at once.** The best bot takes half the purse; the guild that did most of it between them
splits the other half. That is how a guild takes part in an event without a war — Patrick's order of
09.09.2026 — and it is why the score is kept twice, by serial and by guild name.

**Waves** (`BotWaves`) answer the oldest complaint in this log: *nothing within 50 tiles of the bots is worth
fighting.* When a hunt is declared and the field really is empty, the watcher puts one weak thing down, then
two, then three, then a strong one with two weak, up to two strong and five weak — Patrick's own table. The
next wave waits for the last to be dead; nothing is spawned in a guarded town, where the watch would kill it
before a bot arrived; and whatever is still standing when the revel ends is deleted. A shard that accumulates
ettins because a watcher had an idea an hour ago is a shard nobody can measure.

**The purse** used to be six thousand for the session and no more, which meant a shard left running all night
stopped being able to pay for anything after the fifth revel. It is now a running balance filled by a tax:
six per cent of what each guild can spare, every quarter of an hour, taken by the same collector the hall levy
uses so no member is ever left below its keep. **The tax mints nothing** — money goes from the guilds to the
crown and comes back as prizes. This project has paid for a faucet once already.
