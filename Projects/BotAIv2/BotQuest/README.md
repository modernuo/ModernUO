# BotQuest — the board of errands

What the watchers and the door ask of the population, for a price. Patrick's order of 16.09.2026, evening: one board
instead of a list and a verb for each thing somebody wants done; the power to post on it at the door (Argus and the
debuggers) and with the marshal of events among them; any bot fit for an errand may take it and is paid when it is
done. The guilds' masters do not post — they are ordinary bots.

| File | What it is |
|---|---|
| `BotQuest.cs` | one errand: kind (kill, gather, scout), what and how many, where, the reward the board holds, who posted it, who holds it |
| `BotQuests.cs` | the board: posting with the reward escrowed from the treasury (`BotCity.Escrow`), taking and letting go, progress and finishing with the payout, lapsing untaken after two hours and returning to the board after twenty minutes without progress; the clock; save and load |
| `BotLairs.cs` | where a creature lives on the island: the spawners west of x 5120 read once per world load, by creature type and its kinds; a kill anywhere is sent to the nearest lair a road reaches (build 95) whose spawner holds a living one now (build 96); the creature kinds kept, for the marshal's form (build 102), and the fight kept alive near a place, for `BotKept` (build 103) |
| `BotQuestDeed.cs` | doing an errand (`errand`, steadfast, claimed at its reward over walk and work and corrected by the share of such claims earned): a kill wraps `BotSlay` one creature at a time and credits a kill when the creature it set on is dead afterwards, giving a place up after three minutes of seeing nothing; a delivery is a walk and a handing over by a bot already carrying the goods; a scouting is a walk to the place |
| `BotQuester.cs` | the proposer: the best-scoring open errand by reward against walk and work, kills to fighters that are no novices and dare the ground, deliveries to bots with the goods, scouting to anybody |
| `BotQuestStore.cs` | keeps the board across restarts, every errand open again after a boot |
| `BotQuestModule.cs` | the module: the board's clock and the proposer |

The marshal who posts on its own lives with the watchers: `mindedBots/debugger/BotMarshal.cs`.
