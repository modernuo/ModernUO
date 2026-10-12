# Where the bots' names come from

The names, the peoples and the races of the population are one file: `Distribution/Configuration/bot-names.json`. The
shard writes it the first time it runs, from the bank in `BotPeoples.cs`, and reads it from then on. Edit the file and
restart; nothing in the code has to change.

## The file

```json
{
  "Peoples": {
    "Human": {
      "Race": "Human",
      "Share": 5,
      "Beards": false,
      "Male":   ["Aldric", "Berengar", "Cedric", ...],
      "Female": ["Adela", "Beatrix", "Cerys", ...],
      "Houses": ["Ashdown", "Blackbriar", ...],
      "Classes": {}
    },
    "Elf": {
      "Race": "Elf",
      "Share": 3,
      "Male":   ["Aelrindel", "Caelith", ...],
      "Female": ["Aelith", "Caelynn", ...],
      "Houses": ["Silverleaf", "Moonshadow", ...],
      "Classes": { "Mage": 3.0, "Archer": 3.0, "Warrior": 0.5, "Crafter": 0.5 }
    },
    "Dwarf": {
      "Race": "Human",
      "Share": 2,
      "Beards": true,
      "Male":   ["Balgrim", "Dorin", ...],
      "Female": ["Amberle", "Bardryn", ...],
      "Houses": ["Ironfoot", "Stonebeard", ...],
      "Classes": { "Crafter": 3.0, "Warrior": 2.5, "Mage": 0.3 }
    }
  }
}
```

| key | what it does |
|---|---|
| `Race` | The engine's race the people is born as: `Human` or `Elf`. The client of this era draws humans and elves; there are no dwarves, so the dwarves are humans with beards and dwarvish names. |
| `Share` | How many of every hundred newborns are of this people, before the class's leaning. Any scale: 5, 3 and 2 mean half, three tenths and a fifth. |
| `Beards` | Whether the people's men are born with a beard. |
| `Male`, `Female` | The given names, dealt in the file's order. A name is never dealt while somebody wears it, and a newcomer never gets a name whose learning the shard still remembers. |
| `Houses` | Family names, used once the given names run out: `Aldric`, then `Aldric Ashdown`, then `Aldric Blackbriar`. Never a number. |
| `Classes` | How much likelier a newborn of a class is to be of this people. `"Mage": 3.0` makes an elf three times as likely among mages; a class not named weighs 1. |

## How a bot is born

1. **The class comes first**, from the mix in `bot-population.json` (or from the newcomers' clock in `BotGrowth`), as it
   always did.
2. **The people is drawn for the class**: each people weighs `Share × Classes[class]`, and the lot is cast. With the bank
   as shipped, a mage is an elf three times in five, a smith is a dwarf more often than not, a warrior is anybody.
3. **The sex is a coin.**
4. **The name follows the people and the sex**, the next unworn one in the file's order.
5. **The body follows the race**: the engine's human or elf body, the race's own skin and hair hues, and a beard where the
   people wears one.

Which people a bot belongs to is read off its given name — nothing new is saved, and a wipe has nothing to clear. The
dashboard's bot list shows `people` and `race`.

## The bank as shipped

Two hundred names of the period, three peoples:

- **Humans** (80): the towns' own — `Aldric`, `Berengar`, `Godfrey`, `Sigurd`, `Tancred`; `Beatrix`, `Freya`, `Rosalind`,
  `Ursula`, `Rowena` — with the twenty-two houses the population always had.
- **Elves** (60): of the greenwood, leaning to the bow and the book — `Aelrindel`, `Daeron`, `Haldir`, `Thalion`;
  `Elenwe`, `Liriel`, `Miriel`, `Nimue` — houses `Silverleaf`, `Moonshadow`, `Starbrook`.
- **Dwarves** (60): of the hills, leaning to the hammer and the shield — `Balgrim`, `Thorgar`, `Durgan`, `Ragnar`;
  `Dagna`, `Helja`, `Brynhild`, `Solveig` — houses `Ironfoot`, `Stonebeard`, `Anvilhand`.

## Rules worth knowing

- **A name is a key.** What a bot has learned (`Saves/BotProgress`) and every line about it in the log are filed under its
  name, so two bots must never share one. The population refuses a name that is worn; the thinking bots' names in
  `bot-mind.json` (`CrafterNames`) are checked against the bank at boot, and a collision is an error in the log.
- **Taking a people out of the file** means nobody is born of it from then on; the bots already alive keep their names.
- **An empty or missing file** is rewritten from the bank. To go back to the old pool of sixty-four names, give the file
  an empty `Peoples` object — the population then deals from `BotPopulation.Names` as it did before 29.09.2026.
- **Elves need a client that draws them.** Body 605/606 is in every client from Mondain's Legacy on; the shard runs the
  Renaissance rules with a modern client, so they show. On an older client an elf is an invisible bot: set every people's
  `Race` to `Human`.
