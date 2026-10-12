# PLAN-life — bots that live, not only work

Patrick, 08.10.2026 evening: "Great ideas — let's develop the daily cycle, dreams and the generation of the bots'
history further; bots who bought a house must be able to furnish it, keep a diary, tell stories. Add 10 more ideas
and write them into the general plan. We will build them when the weekly Fable limit resets."

This is the plan, not a design: each item names what the player sees, the moving parts, what already exists to build
on, and how we will know it works. Every item is built the usual way — one implementer, two independent reviews with
arguments and assumptions, a fix pass, then deploy — and is held to the shard's standing rules: honest money (no gold
made from nothing, no handouts to the poor: poor bots hunt), honest counting (a new gate gets a new counter; never
improve a share by changing what counts), a bot sees with its own eyes, nothing across hostile guilds, traders
neutral, offices exactly one each (`BotClass.Office`), English in code and docs, PascalCase config keys.

Order of work (proposed): 1 → 2 → 4 → 5 → 3 → 6 → 7, then part B by Patrick's pick. Items 1–2 change what every bot
does every hour, so they go first and are measured for a day before the rest lean on them.

---

## Part A — the approved core

### A1. The daily cycle

**Seen:** the island has a rhythm. At dawn the markets fill; through the day bots work; at noon some eat; in the
evening the taverns and firesides are full and the roads empty; at night the watch walks, thieves come out, most bots
sleep at home, at an inn or by a fire. A player logging in at 22:00 finds a different island from one at 10:00.

**Parts:**
- A shard clock of hours (real local time, like prime time already is — `BotPrime`) with named parts of the day:
  dawn, morning, noon, afternoon, evening, night.
- A per-bot timetable drawn at birth from class and nature (`BotTraits`: homebody/wanderer, sociable/solitary…):
  wake hour, meal hours, evening habit (tavern, fireside, home, prowl), sleep place. Night owls exist (thieves, watch,
  some hunters).
- The timetable is a weight on the offers, not a wall: an evening raises tavern/fireside/stories/home ×N and lowers
  long journeys; a starving or broke bot still works at night (the floor stays — "a factor without a floor is a veto").
- Sleep: resting (`BotRest`) becomes "going to bed" at the bot's own home (A4) or an inn, with the rested buff;
  prime time's offline share is filled first by bots whose bedtime it is, so who is offline makes sense.
- Festivals calendar (every few days, announced a day ahead): harvest fair, night of lanterns, midsummer — built on
  `BotEvents` fairs and taverns; contests (B5) on festival days.
- Weather (cheap): storm/rain/clear per region, a few hours long; storms hold ships in port and thin the market,
  clear days raise hunting and caravans. Engine weather effects shown to players if available.

**Builds on:** `BotPrime` (prime time), `BotRest`, `BotInns`, `BotTaverns`/`BotSupper`, `BotEvents`/`BotFair`,
`BotTraits`, the offers' weighting (`BotWill`).
**Measure:** offers taken by part of day (a table in `/api/state`), tavern guests by hour, work and Needs shares
must not fall (they are the guard), sleepers at home vs inn vs field.

### A2. Dreams — a life goal for each bot

**Seen:** every bot has a dream on the dashboard ("Nessa dreams of a house by the sea", "Varen wants to lead The
Wolf", "Brokk will be a grandmaster smith", "Thalia swore revenge on the ogre that killed her friend"), its progress,
and the story of how it came true or was abandoned.

**Parts:**
- A list of dream kinds with a test of progress and of fulfilment, each from real state: own a house (A4), a bigger
  house, grandmaster in the class's main skill, lead or found a guild, hold an office in the guild (officers), win the
  championship, take a relic, see every town, kill a named creature (legend, A7), revenge (`BotGrudge`), marry (A6),
  teach an apprentice to journeyman (A5), save N gold, furnish a house fully.
- Chosen at birth and after each fulfilment from traits, class and life so far (a greedy bot dreams of gold, a
  vindictive one of revenge, a sociable one of a family). One dream at a time, with a small chance to abandon it after
  long failure (written into the biography).
- A dream is a bounded weight on offers that move it (×1.1–1.3), never a veto and never a payment.
- Fulfilment: a biography milestone, a line said in town, a tavern celebration (A1), and a new dream.

**Builds on:** `BotBiography`/`BotLife` (events), `BotTraits`, `BotGrudge`, `BotProgress` (titles), `BotAbode`
(houses), officers, tourney, relics.
**Measure:** dreams held by kind, fulfilled per day, abandoned, mean days to fulfil; must not starve work (shares).

### A3. Story generation — the history of each bot and of the island

**Seen:** on the dashboard each bot has a life story in chapters — born in Britain, apprenticed to Brokk, joined
The Anchor, lost a friend at the Orc Caves, bought a house, became a journeyman tailor — and the island has a
"Chronicle": its wars, champions, relics, founded and fallen guilds, legends.

**Parts:**
- Chapters assembled from the biography's events (already stored, weighted, trimmed): one chapter per life stage
  (youth, apprenticeship, guild, deeds, home, family, old age).
- Prose: a template writer always; the local model (Ollama, `BotMindAI`) optionally rewrites a chapter in a voice
  once per chapter (not per tick), cached; absent model → templates. No invented facts: the prompt is the event list.
- The island Chronicle: a daily page from wars, championships, relics, guild births/deaths, legends, festivals.
- `/api/story?bot=` and `/api/chronicle`; a "Story" tab in the dashboard.

**Builds on:** `BotBiography`, `BotLife`, `BotEvents` feed, `BotVoice`, `BotMindAI` (model seam, as the duke and the
steward use), `BotWeb`.
**Measure:** stories with ≥3 chapters, model calls per hour (bounded), cache hits; no loop cost (built off the loop
or on a slow clock).

### A4. Houses that are homes — furnishing

**Seen:** a bot who bought a house (`BotAbode`) furnishes it over time: a bed, a table and chairs, a bookshelf with
its diary (A5'), a chest, rugs, a forge or loom for crafters, trophies (a relic it won, a monster's head from a
legend hunt), a sign with its name. Friends come for a house-warming. A player can walk in and see a lived-in house.

**Parts:**
- A furnishing plan per house size and class (what goes where, as engine furniture items placed by the house's own
  rules; locked down so they stay).
- Bought honestly: furniture is a want on the Needs board / an order to carpenters, tailors (rugs, curtains),
  smiths — real demand for crafts that have little today. Paid from the bot's purse above its keep, slowly.
- Use: sleep at home (A1), store surplus in the house chest (`BotSurplus` already stows), host a house-warming and
  evening visits for sociable bots (a small gathering, speech).
- Upgrade: a dream (A2) to a bigger house; selling the old one honestly.

**Builds on:** `BotAbode`, `BotEstate`/`BotFittings` (guild halls are already fitted), `BotMarket`/`BotLines`
(wants), carpenters (`BotCarpenter`?), `BotSurplus`.
**Measure:** houses furnished %, furniture bought (gp, items), carpenter work taken/finished, visits; money flows
counted (no minting).

### A5. Master and apprentice

**Seen:** a grandmaster smith walks with an apprentice; the apprentice fetches ore, works the bellows, crafts
beside the master and grows faster; a year later (days, in shard time) the apprentice opens a stall and takes its
own apprentice. Mages learn Inscription from the Sage — the island gets five to ten circle-6 scribes, which is the
honest cure for the scroll wants that hold Needs at 95 %.

**Parts:** pairing (master ≥ adept in a craft, apprentice novice in it, same town, not hostile guilds), a shared
deed (the master works, the apprentice assists; skill gain to the apprentice via the engine's own gain with a
teaching bonus within the engine's caps — say exactly which rule), a fee or labour as payment (honest), graduation
at a skill, biography events on both sides, a dream link (A2).
**Builds on:** `BotTutor`/teaching (Captain and Sage teach already), crafts, `BotScribe`, `BotBiography`.
**Measure:** pairs, graduations, circle-6/8 scribes on the island over days, Needs scroll share.

### A5'. The diary

**Seen:** each bot keeps a diary — a short entry per day ("Sold 12 leather caps in Britain, 140gp. Ate at
Jasper's tavern; Thalia told the story of the ogre. Tired. Tomorrow the mines."). A player can read it: the diary is
a real in-game book in the bot's house (or pack if homeless), and on the dashboard.

**Parts:** a day's digest from the bot's own events (work finished, gold, meetings, fights, mood, dream progress),
written by template; optionally a model touch weekly; a book item updated once a day (bounded pages, oldest pages
roll off into a "volume II" kept on the shelf); `/api/diary?bot=`.
**Measure:** entries per day, books in houses, size bounded.

### A6. Telling stories

**Seen:** at a tavern or a fireside in the evening (A1) bots tell stories aloud — their own deeds, a friend's
death, the championship, a legend — and others answer ("I was there!", "Aye, I knew Thalia"). A told story becomes a
rumour that travels with the listeners to other towns; good tellers (sociable, high-fame) draw guests.

**Parts:** a story is a biography event with a teller; picked by weight (recent, dramatic, known to listeners);
spoken in local speech lines (`BotVoice`), visible to players; listeners record "heard of X" (feeds reputation,
B1 bards, rumours); fame per bot.
**Measure:** stories told per evening, listeners, rumours spread to other towns.

### A7. Families

**Seen:** bots with strong ties marry in a town chapel with guests and a feast; a newcomer may be born to a couple
with their family name; a family shares a house, parents teach children their craft, kin avenge kin.
**Parts:** courtship from `BotTies`, a wedding event, the household (shared house, shared chest by rule), births
via `BotGrowth` (a newcomer assigned to a family when the population grows — no extra bots beyond the mix), kinship in
grudges and biographies.
**Measure:** marriages, families, children born into families, kin-revenge events.

### A8. The legendary beast

**Seen:** every few days a unique monster roams (a dragon over Despise, a kraken off Magincia), moves its lair,
attacks caravans and roads; scouts track it, guilds race to kill it; the slayer guild hangs the trophy in its hall,
bards sing it (B1), the Chronicle (A3) records it.
**Builds on:** `BotEvents`/`BotRelicLair` (lair wardens), scouts (officers), companies, zones (temporary zone, D of
zonemodel30), relic trophies.
**Measure:** legends spawned, tracked, killed, by whom, deaths caused.

---

## Part B — ten more ideas (Patrick picks)

1. **Bards and songs.** A bard role (a class leaning or a Musicianship hobby) composes short songs from the
   biographies and the Chronicle and performs them in taverns and at festivals; a song raises the hero's fame and the
   tavern's guests; players hear them. Builds on A3/A6, `BotTaverns`, engine Musicianship.
2. **Pilgrimages and virtues.** Bots walk to the eight shrines after a death in the family, before a war, or as a
   dream; virtue standing (engine virtues where they exist) colours speech and choices; a shrine pilgrimage is a long
   journey with companions. Builds on travel, gates, companies.
3. **Letters.** Friends and kin in other towns write letters (a real in-game letter item); couriers carry them for a
   small fee; a letter can carry news, an invitation to a wedding, a request for help, or gold (honest, from the
   sender's purse). Makes the roads social, gives couriers work.
4. **Courts and trials.** A caught thief or murderer is tried in the town hall: witnesses (bots who saw it) speak,
   the steward or a judge decides: fine to the victim, the cells, exile; the trial is spoken aloud and logged.
   Builds on `BotCrime`, stewards, `BotWitness`.
5. **Games and contests.** Dice and chess in taverns (small honest stakes between bots), and on festival days an
   archery contest, a foot race, a fishing contest, a cooking contest with prizes from the town purse.
6. **Sickness and healers.** Occasional illness (a fever after a swamp, a plague in one town) lowers stamina and
   work; healers and herbalists cure it for a fee; a plague brings quarantine and Needs for cures — real work for
   Healers beyond combat (infirmaries today do almost nothing).
7. **Collectors and museums.** Some bots collect (rare dyes, paintings, gems, books, trophies); collectors trade
   among themselves and buy from adventurers; a guild hall can hold a museum room that raises the guild's appeal.
8. **The graveyard.** Fallen bots who are deleted at a wipe or who die many times get a tombstone with an epitaph
   from their story; friends visit on festival days; the Chronicle lists the fallen of the week. (Bots do not
   permadie today — tombstones mark heroic deaths and the old world's dead after a wipe.)
9. **Bots and players.** Bots remember players they meet (Patrick): a reputation per player (helped, robbed, fought
   beside); bots post requests a player can answer on a board in town (fetch, escort, kill), pay from their own purse,
   and tell stories about the player. The island reacts to the human living in it.
10. **The island gazette.** A daily newspaper "The Britannia Herald" compiled from the Chronicle (A3), market prices,
    wars, championship results, births and weddings, the legend's sightings — a dashboard page and an in-game book on
    every bank's counter, read aloud by heralds (officers).

---

## Notes for the implementers

- Each item lands behind a `Running` dial and its own summary line in the log (`Life:`, `Dreams:`, `Homes:`…), with
  `/api/state` keys for the dashboard. Dials in `DIALS.md`, decisions in `DECISIONS.md` (§4.1 journal), files in
  `MAP.md` (`regen-map.py`).
- The loop budget: bots take ~21 ms of every 100 ms at 310 bots (08.10). Stories and diaries are written on a slow
  clock or off the loop; no per-tick text generation.
- Persist through `GenericPersistence` stores (world save), never loose files under `Saves/` (moved to backup at every
  save); `do reset` must wipe every new store (see reset26).
- Population to 400 (Patrick asked 08.10): the hardware allows it (~27 % of the loop); raise `BotGrowth.Most/FillTo`
  after A5 (apprentices) starts curing the scribe shortage.
