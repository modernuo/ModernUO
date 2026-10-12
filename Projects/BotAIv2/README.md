# UOBot — an ant farm of Ultima Online bots

## Work, and how much of it gets finished

| Work | What it is | Taken on | Finished | Finished share |
|---|---|---:|---:|---:|
| weave | shearing sheep, spinning, weaving and cutting cloth and bandages | 1,000 | 502 | 50 % |
| hunt | killing a creature for its loot and hide | 1,000 | 820 | 82 % |
| brew | brewing potions | 1,000 | 844 | 84 % |
| prowl | walking out to ground where there is something to fight | 1,000 | 928 | 93 % |
| forage | picking up reagents lying about | 1,000 | 867 | 87 % |
| acquire | buying off the market | 1,000 | 922 | 92 % |
| herbs | picking reagents | 1,000 | 906 | 91 % |
| delve | going down into a dungeon as a party | 1,000 | 733 | 73 % |
| band | calling a company together for something one bot cannot take | 1,000 | 721 | 72 % |
| rescue | going to the help of a bot under attack | 1,000 | 765 | 76 % |
| unload | taking a full pack to a stall or a counter | 1,000 | 731 | 73 % |
| mine | digging ore | 1,000 | 933 | 93 % |
| inscribe | writing spell scrolls | 1,000 | 947 | 95 % |
| escort | seeing another bot safely somewhere | 1,000 | 782 | 78 % |
| order | putting money down on the market for what it cannot make | 1,000 | 992 | 99 % |
| mend | healing itself or another bot | 1,000 | 824 | 82 % |
| restock | buying what its class must carry — bandages, bottles, arrows, reagents, tools | 1,000 | 965 | 96 % |
| flee | running from a fight it is losing | 1,000 | 565 | 56 % |
| pickings | going through a corpse | 1,000 | 978 | 98 % |
| glean | picking up its spent arrows | 1,000 | 964 | 96 % |
| housecall | a healer going to a bot that sent for it | 1,000 | 816 | 82 % |
| homeward | walking home | 1,000 | 698 | 70 % |
| cook | cooking meat at a fire | 1,000 | 933 | 93 % |
| chop | felling trees | 1,000 | 923 | 92 % |
| tinker | making tools out of iron for the other trades | 1,000 | 961 | 96 % |
| sew | tailoring leather and cloth | 1,000 | 975 | 98 % |
| forge | smithing weapons and armour | 1,000 | 966 | 97 % |
| peddle | selling goods to an NPC shopkeeper | 1,000 | 942 | 94 % |
| fletch | making arrows and bows | 1,000 | 990 | 99 % |
| stake | standing on a square its guild is claiming | 1,000 | 708 | 71 % |
| ward | telling a stranger hunting the guild's land about the toll | 1,000 | 950 | 95 % |
| supply | stocking its guild's counter from the shops | 1,000 | 898 | 90 % |
| rally | joining its guild's war company | 1,000 | 381 | 38 % |
| travel | walking to another town | 999 | 830 | 83 % |
| harrow | a guild's or the Baron's company clearing a deadly square | 832 | 638 | 77 % |
| enlist | joining a Captain's company | 801 | 565 | 71 % |
| scout | walking ground nobody has looked at | 732 | 608 | 83 % |
| reclaim | going back to its own corpse for its things | 675 | 561 | 83 % |
| bowyer | making bows and crossbows out of logs | 506 | 495 | 98 % |
| evict | moving a stranger out of its guild's hall | 502 | 494 | 98 % |
| tutor | teaching a class for a fee | 358 | 334 | 93 % |
| tame | bringing a beast to heel | 339 | 210 | 62 % |
| quarrel | fighting a member of a guild it is at war with | 163 | 52 | 32 % |
| other | 51 rarer kinds: plunder, camp, haul, fireside, deliver, meet, shopkeep, envoy, tavern, supper, guard-duty, drill-in, blanks, watch, ragpost, drill, draw, sweep, keep-store, grudge, borrow, herald-stand, stroll, guard, scout-shadow, errand, liberate, draw-kit, mark, caravan, rumour, steal, militia, defend, fair, relic, bounty, resettle, home, hire, infirmary, house, hall, outpost, raid, library, fit, enlarge, manhunt, remove, relic-home | 17,930 | 14,075 | 78 % |
| **all work** | | **56,837** | **46,692** | **82 %** |

## Working on now — October 2026

- **A world that happens round the work.** Each bot has a nature, a life story, friends and enemies. The professions
  keep establishments: the cook's tavern, the guild's infirmary, the moneylender, the library, the ragman by the
  dungeon. Invasions come out of the dungeons onto the roads and a militia answers them; there is a daily fair with a
  lottery, caravans hire guards, the night is more dangerous, and Argus spreads rumours of treasure.
- **Politics and the law.** Guilds and towns hold elections, plot, vote on what the chest is saved for and send
  ultimatums instead of declaring war. A town has a steward who issues decrees, and auctions charters that give a guild
  a monopoly on a craft. Cutpurses work the banks, bandits lay ambushes on the carriers' roads, a fence buys the loot,
  and the wanted list carries bounties, terms, ransoms and repentance.
- **Guild officers, relics and the sea.** A quartermaster keeps the guild's store, a herald recruits at the bank and
  calls bouts between guilds, a scout watches the zones and the rival companies. Relics and banners can be stolen,
  taken in war and ransomed. Guilds sail expeditions, found colonies on the islands and run their cargo home, while
  The Shadow waits on the quays.
- **A championship worth watching.** Once a day at 20:00 local time, fifty bots and more fight it out at the round
  arenas, a party fighting while the next one gets ready.
- **Guards who come.** A wild creature that chases a bot into town and fights it there is now a guard candidate,
  whoever struck first, and a beaten bot runs for a town only when it can get there.
- **Next: bots that live, not only work.** A daily rhythm with holidays and weather, dreams, a bot's own history and
  the island's chronicle, furnishing a house, a master and an apprentice, a diary, stories by the fire, families
  (`Projects/BotAIv2/PLAN-life.md`).

## An ant farm

This is an ant farm, not a game with a player in it. You set up the world in a handful of files, start the shard,
and watch what the population makes of it. Nobody drives a bot. Each one is born a novice, prices every piece of
work the shard offers it, takes the best, and gets better at what it does; the prices are learned from what the work
really paid, so a trade that stops paying stops being chosen without anybody editing a number.

Everything the farm is made of is configured, and everything configured lives in `Distribution/Configuration/`, one
file per subsystem, written by the shard itself on first boot with the code's defaults:

| File | What it decides |
|---|---|
| `bot-population.json` | how many bots, of which classes, where they start, what they are born with, and whether the special offices exist at all: a **Captain** who drills the fighters, a **Sage** who teaches the rest, an **Architect** paid by the health of the market, a **Baron** who clears the ground that has killed people. Each is a class with a count; a count of nought is an office that does not exist. This farm runs all four. |
| `bot-classes.json` | the fifteen classes: their skills and targets, what they carry, what they are limited to |
| `bot-will.json` | how a bot decides: pricing the work on offer, commitment, how long work is held, the margin a better offer must clear |
| `bot-movement.json` | how a bot walks: search budgets, how long before a walk is given up, what counts as stuck |
| `bot-harvest.json`, `bot-craft.json`, `bot-shops.json`, `bot-market.json`, `bot-traders.json`, `bot-trades.json` | the economy: gathering, making, buying over a counter, trade from hand to hand, the Traders, the professions' establishments |
| `bot-hunt.json`, `bot-squad.json`, `bot-mend.json`, `bot-spells.json` | fighting, companies, healing, magic |
| `bot-estate.json`, `bot-delve.json`, `bot-baron.json`, `bot-drill.json` | guilds, halls, land, war and its limits; dungeons; the Baron's raids; the Captain's drills |
| `bot-character.json`, `bot-events.json`, `bot-politics.json`, `bot-crime.json`, `bot-charters.json`, `bot-steward.json`, `bot-officers.json`, `bot-relics.json`, `bot-sea.json` | the life of the world: natures and life stories; invasions, fairs, caravans, night and rumours; elections and councils; thieves, bandits and bounties; town charters and the steward; guild officers; relics and banners; expeditions and colonies |
| `bot-zones.json`, `bot-reach.json`, `bot-travel.json`, `bot-rest.json` | the danger zones, the map of reachable ground, roads between towns, rest at inns and how many bots are out at each hour |
| `bot-proving.json` | whether the world is awake round the bots, and the proving ground where a bot's real strength is measured |
| `bot-voice.json` | what the bots say, by occasion, and which channels are on |
| `bot-web.json` | the dashboard: address, port, how often the page is refreshed, whether the map is drawn |
| `bot-alarm.json` | when the shard raises an alarm about itself |
| `bot-mind.json`, `bot-debugger.json` | which bots think, with which model, through which protocol; and Argus, the observer |

Keys are PascalCase and a wrongly cased key is silently ignored: the proof of a setting is the line its module
writes at boot. Every number in the assemblies — 1,400 of them — is listed in `Projects/BotAIv2/DIALS.md` with the
key that reaches it, and most can be changed on a running shard through the door (`Distribution/argus-in.txt`).

## What the bots do

- **Earn a living.** They mine and smelt ore, fell trees and pick reagents. They forge weapons and armour, sew
  leather, brew potions, fletch arrows, tinker tools, cook and write spell scrolls. They sell to NPC shopkeepers and
  run a market of their own: stalls, and money put down for what they cannot make. The loop closes on itself: ore
  becomes ingots become the pickaxe that digs the next ore, and a hunter's hides become the armour the next hunter
  wears.
- **Fight and look after each other.** They hunt the island's creatures and carve them, bind their own wounds and
  their friends', and hire a healer to stand by them. They answer a call for help, band into companies for what one
  bot cannot take, and go down into dungeons — as a party, walking to the cave mouth and through it, or alone.
- **Travel.** The world's teleporters are read as gates between lands, so a walk can be drawn through a cave mouth.
  The towns are read from the map, the roads between them drawn and walked; a bot with nothing pressing sets out for a
  town it has not seen and tells its guild what the road was like. Guilds settle by other towns, not only Britain.
- **Rest.** Every bot has its own stamina for the game and tires like a player; a tired bot walks to the nearest inn or
  tavern, pays for a bed, and leaves the world from there. A paid night comes back as a third of the rest's length in
  regeneration, and the day's boredom falls.
- **Are born of three peoples.** Humans, elves and dwarves, two hundred names in a file, and a class that leans to a
  people: a mage is an elf more often than not, a smith a dwarf. See `Projects/BotAIv2/BotPopulation/NAMES.md`.
- **Get better.** Skills rise with use and survive a restart. A bot is a novice, then an apprentice, and in time a
  grandmaster of the trade it has actually practised, and the Captain and the Sage teach classes for a fee.
- **Form guilds and go to war.** Bots found their own guilds. A guild raises a hall, claims land a square at a time
  and keeps a chest. Guilds go to war under rules that end the war: so many dead or so much plunder wins, and the
  loser gives ground.
- **Commit crimes, and pay for them.** A bot that kills another bot goes red. Murderers found The Shadow, a band of
  thieves with a hideout and a fence, who rob from ambush. The city puts prices on heads, law-abiding bots hunt
  murderers down, the Baron raids the hideout, and whoever is caught sits in a cell.
- **Take errands.** A board offers paid errands (kill, deliver, scout), and once a day fifty bots and more fight a
  championship at the round arenas.
- **Talk.** They say what they are doing, shout for what they want to buy, cry for help, and tell their guild what
  happened.

There are 167 bots at first, the mix in `bot-population.json`, and newcomers join five at a time until there are 310. The bots are a separate assembly
that ModernUO loads beside its own content. The engine itself changes in eleven files and gains one, through the small patches in
`Projects/BotAIv2/engine-patches/`, and gains the tiered navigation the bots walk by, in
`Projects/UOContent/Engines/Pathing/Tiered/`.

## Watching them

- **The dashboard**, `http://127.0.0.1:2599/`, while the shard runs. Nothing on it can change the shard.
- **The logs.** `logs/session-<date>.log` records every choice with the price it won at and the offer it beat, and
  every five minutes a summary that accounts for each mechanism. `logs/bot-events.ndjson` is the same as events,
  `logs/bot-speech.log` is what the bots said, and `logs/alerts.ndjson` is the shard raising alarms about itself.
- **In game**, `[bots` as an administrator opens the dashboard gump.
- **Argus** is an optional invisible observer. It uses a model through Ollama, or any service that speaks the OpenAI
  protocol, to watch the population and report what it thinks is wrong. Nothing else needs a model unless you give
  the crafters minds: see `Projects/BotAIv2/mindedBots/LLM.md`.

## Setting it up

You need the .NET 10 SDK, git, the Ultima Online Classic client files and a client such as ClassicUO.

1. **Clone this repository and run the setup script.** The script clones ModernUO into `../ModernUO`, at commit
   `be3a085`, which the bots are built against. Then it copies the bots in, applies the engine patches, registers the
   two bot assemblies and builds everything.
   ```bash
   git clone https://github.com/DmitriyBol/UOBot.git
   cd UOBot
   ./setup.sh
   ```
   On Windows, the same from PowerShell:
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\setup.ps1
   ```
   To put ModernUO somewhere else, give the folder: `./setup.sh ~/shards/ModernUO` or
   `.\setup.ps1 -Target D:\ModernUO`. After a `git pull`, run the script again: it copies the new files over, skips
   what is already done and rebuilds.
2. **Start it once and answer its questions**: the path to the client files, the expansion (type **`UOR`**) and the
   owner account. Enter accepts the default for everything else. On Windows, run this from PowerShell or cmd: the
   Git Bash window is not a real console, so the server cannot ask its questions there.
   ```bash
   cd ../ModernUO/Distribution
   dotnet ModernUO.dll
   ```
   The bots are alive once the log says `Population raised: 167 bots`, and the dashboard is at
   `http://127.0.0.1:2599/`.
3. **Give them a world.** Log in as the owner at `127.0.0.1:2593` and type `[Decorate`, then
   `[ImportSpawners Data/Spawns/shared/felucca/*.json`, then `[Save` and `[Restart`. These put in the shops, forges
   and creatures, and the bots survey them after the restart.
4. **Shape the farm.** Edit the files in `Distribution/Configuration/` — the population and its classes first — and
   restart. Most numbers can also be moved on the running shard through the door.

### Playing on it, and with friends

The bots do not need a player, but the shard is an ordinary ModernUO shard and takes ordinary clients.

1. **Connect yourself.** Point a client (ClassicUO, or the classic client with a launcher such as Razor) at
   `127.0.0.1`, port `2593`, with the same client files the server was told about. Accounts create themselves on first
   login (`accountHandler.enableAutoAccountCreation` in `Distribution/Configuration/modernuo.json`); the owner account
   is the one you named at the first start, and has every command.
2. **Let friends in.** The shard already listens on every interface (`"listeners": ["0.0.0.0:2593"]` in
   `modernuo.json`). Two things stand between a friend and the shard:
   - **A way to reach your machine.** On one network, your local address (`ipconfig` / `ip addr`) and port 2593 are
     enough. Over the internet, either forward TCP 2593 on your router to this machine and give friends your public
     address, or put everybody on a virtual network such as ZeroTier, Tailscale or Hamachi and give them your address on
     it — no router changes, and nothing of yours is on the open internet.
   - **The firewall.** Windows asks once whether `ModernUO.exe` may accept connections; say yes for the networks you use,
     or add the rule by hand: `netsh advfirewall firewall add rule name="ModernUO" dir=in action=allow protocol=TCP
     localport=2593`.
   Friends connect the same way you do, with their own client files, to your address and port 2593; each first login
   makes an account.
3. **Keep it fair.** `accountHandler.maxAccountsPerIP` (10) is the only limit on accounts. To close the door to strangers,
   set `"enabled": true` and the allowed addresses in `Distribution/Configuration/ip-allowlist.json`, or turn account
   creation off once your friends have theirs.
4. **Keep it up.** `START_SHARD.bat` starts the shard so it outlives the window that started it and writes its
   console to `logs/`. Nothing starts it on its own; run it again after a reboot. The world saves itself every five
   minutes.

The dashboard at `http://127.0.0.1:2599/` is bound to this machine only (`Bind` in `bot-web.json`); give it `0.0.0.0` to
show it to friends, and know that nothing on it needs a password.

<details>
<summary>The same by hand, without the script</summary>

1. **Clone ModernUO and this repository side by side.** The bots are built against ModernUO `be3a085`. Newer
   ModernUO has moved its serialization packages to 4.x, and the bots do not build against those yet.
   ```bash
   git clone https://github.com/modernuo/ModernUO.git
   git -C ModernUO checkout be3a085
   git clone https://github.com/DmitriyBol/UOBot.git
   ```
2. **Copy the bots in and patch the engine.**
   ```bash
   cp -r UOBot/Projects UOBot/Distribution ModernUO/
   cd ModernUO
   git apply Projects/BotAIv2/engine-patches/*.patch
   ```
3. **Register the two assemblies.** Add them to `ModernUO.slnx` beside the other projects:
   ```xml
   <Project Path="Projects/BotAIv2/BotAIv2.csproj" />
   <Project Path="Projects/BotAIv2/mindedBots/BotMindAI.csproj" />
   ```
   `Distribution/Data/assemblies.json`, which the copy brought in, already lists them after `UOContent.dll`.
4. **Build.**
   ```bash
   dotnet build ModernUO.slnx -c Release
   ```

</details>

## Read more

The documents live in `Projects/BotAIv2/`:

| | |
|---|---|
| `GUIDE.md` | the long read: every kind of work, the classes, how a bot decides, guilds, war, crime, the economy, and running and watching a shard |
| `INSTALL.md` | installing into an existing fork, the switches, the engine patches, Argus |
| `mindedBots/LLM.md` | connecting a model: Ollama, other services, what the bots are shown, how to write a prompt for one |
| `BotWeb/README.md` | the dashboard and its API |
| `BotVoice/README.md` | the voices and the phrase bank |
| `MAP.md` | where anything is, file by file |
| `DIALS.md` | every tunable number and the config key that reaches it |
| `DECISIONS.md` | what was decided and tried, and the defects that keep coming back |

The research behind the design is in [`docs/investigation/`](docs/investigation/): how strong a bot really is, how
it finds its way, how its money moves, and how it weighs one piece of work against another. Each study has its data,
the script that reproduces its numbers and a page that draws them.

New work is written in the fork, [DmitriyBol/ModernUO-fork](https://github.com/DmitriyBol/ModernUO-fork), and comes
here after it has run on a live shard. The paths here match the paths there. The code in this repository keeps only
its class summaries, and the reasoning is in the documents.
