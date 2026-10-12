# Investigation

The research behind the bots, one topic to a folder. Each study was made on the live shard, from its own logs, while
the bots were being built, and each one changed the code. The decision log (`Projects/BotAIv2/DECISIONS.md`) records
what was changed and when; these folders show the evidence.

| Topic | The question |
|---|---|
| [Strength](strength/) | How strong is a bot really? The shard stopped computing it from a formula and started measuring it: a copy of the bot fights a real creature on a proving ground. |
| [Pathfinding](pathfinding/) | Why did the bots get stuck against rivers and cliffs for a month, and what replaced the single bounded search: a coarse chart, then navigation in three tiers. |
| [Economy](economy/) | Where the bots' money comes from and where it goes: the market of stalls and wants, the shopkeepers, the guilds and the city's treasury. How the money is measured, and the defects the measuring found. |
| [Work weights](work-weights/) | How a bot decides what a piece of work is worth, and so what it does next. The claim, the two corrections learned from what work really paid, the factors under a fifth root and the vetoes, checked against 462,156 logged choices. |

Each folder holds:

- `README.md`: the study itself. It covers the question, the method, what was found, what changed in the code, and
  what is still open.
- `data/`: the numbers behind it, as CSV and JSON.
- `extract.py`: the script that wrote `data/` from the shard's session logs. It uses only the Python standard library.
  Run it as `python extract.py <folder of session-*.log>` against your own shard's logs. The logs these numbers came
  from are not in this repository.
- `index.html`: the numbers drawn as charts. It is one file with its data inside; open it in a browser.

Dates are 2026 and times are the shard's local clock, as the logs print them.
