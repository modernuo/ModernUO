# Classes

Thirteen classes, one file each — four melee, three ranged, two casters, a medic and three producers, four
of which cast; the engine says so in one line at boot and that line is the only count worth trusting. A class is **a description and a set of limits, with no behaviour**: it decides
nothing and controls nobody. Deciding is `BotWill`'s, and it reads classes the way it reads the map — as facts
about the world.

## Layout

| File | What is in it |
|---|---|
| `BotClass.cs` | the contract: what any class must be able to say about itself |
| `BotRole.cs` | role in a group — Melee, Ranged, Caster, Medic, Producer |
| `BotPotionKind.cs` | potion families, which is the granularity a carrying limit needs |
| `BotKit.cs` | what is issued at birth, and on what terms of binding |
| `BotWeaponOption.cs` | weapon + skill + target + ammunition, as one fact |
| `BotLeaning.cs` | one way a newborn of the class may lean: the skill, the weight, the band (or the engine's material ladder it is read off), the floors it keeps; applied by `BotBirth` (01.10.2026) |
| `BotArsenal.cs` | the weapons of the era, the spell ids and the potion types, named once |
| `BotClasses.cs` | the registry of nine, and applying the configuration overrides |
| `BotClassConfig.cs` | reading `Configuration/bot-classes.json` |
| `BotCalling.cs` | whether a piece of work is the class's own trade, another class's, or anybody's; what that does to its price; how each class's minutes divide |
| `BotArcher.cs` | the bow and nothing else, and the only class that can triple a hit |
| `BotArchitect.cs` | the bot that is paid by the health of the market rather than by any errand in it |
| `BotBaron.cs` | the one bot on the shard that is not trying to make a living |
| `BotBrawler.cs` | fights with its hands, and is therefore never holding anything it has to put down |
| `BotCaptain.cs` | the one bot on the shard that exists for the others rather than for itself |
| `BotClassModule.cs` | the class layer as a module: reads its file, then says what the nine came out as |
| `BotCrafter.cs` | metal, cloth and leather |
| `BotGatherer.cs` | ore and timber, and the only bot that can find a reagent in the grass |
| `BotHealer.cs` | the green staff |
| `BotMage.cs` | a spellbook, a blue staff, and no metal |
| `BotSage.cs` | the captain's opposite number, for the half of the population a captain cannot teach |
| `BotWarrior.cs` | the plain fighter |
| `BotWarriorArcher.cs` | shoots, and has a knife for when that stops working |
| `BotWarriorMage.cs` | plate, a blade, and spells anyway |
| `BotTrader.cs` | the merchant: neutral in wars, buys where a thing is and carries it to whoever asked (01.10.2026; see `BotTraders/README.md`) |
| the other nine | the classes themselves |

## The numbers, as the code has them

Stats total 100 for everybody. The potion limit is one of each family bar two exceptions. Brewing is once every
10 minutes for everybody except the healer.

| Class | Role | Str/Dex/Int | Main skill | Skill targets | Talent |
|---|---|---|---|---|---|
| Warrior | Melee | 50/35/15 | by the weapon roll | weapon 75, Tactics 70, Anatomy 60, Healing 60 | — |
| WarriorMage | Melee, casts | 40/25/35 | by the weapon roll | weapon 70, Magery 65, Tactics 60, Anatomy 50, Healing 50 | 2 mana / 4 s without a staff, plate allowed, 2 mana potions |
| WarriorArcher | Ranged | 40/45/15 | Archery | bow or crossbow 75, Tactics 65, Anatomy 55, Healing 50, dagger 40 | — |
| Archer | Ranged | 35/50/15 | Archery | bow 80, Tactics 70, Anatomy 60, Healing 50 | ×3 crit at Archery/1000 — 3 % at the start, 10 % at grandmaster |
| Brawler | Melee | 40/50/10 | Wrestling | Wrestling 80, Tactics 70, Anatomy 65, Healing 60 | hands always free, 2 heal potions |
| Mage | Caster | 25/25/50 | Magery | Magery 80, **Inscribe 80**, Meditation 65, Alchemy 40, Wrestling 30 | blue staff 2 mana / 4 s, no plate |
| Healer | Medic | 25/30/45 | Healing | Healing 80, Anatomy 70, Magery 60, Meditation 60, Alchemy 50 | green staff 4 mana / 4 s, brews every 7 minutes, no plate |
| Crafter | Producer | 50/30/20 | **Blacksmith** | Mining 75, Blacksmith 70, Tailoring 60, Tinkering 50, Tactics 40, Healing 40, weapon 40 | once an hour, one forging gives two items for one lot of material |
| Gatherer | Producer | 55/30/15 | Mining | Mining 80, Lumberjacking 75, Tactics 40, Healing 40, weapon 40 | every 15 minutes, 3–6 reagents of one kind |
| Trader | Producer, `Neutral` | 45/40/15 | **Arms Lore** | Tactics, Arms Lore, Healing, Anatomy (100 each, opening 50/30/20), weapon by the roll | in no guild, no company, no toll; hauls spoken wants and keeps an order book (`BotTraders/`) |

Everybody's weapon is rolled from six families: three swords, a war mace and two fencing weapons. The
warrior-mage is offered a quarterstaff instead of the dagger — an ordinary fighting staff, not the one that pays
mana back.

**The mage's Inscribe 80 is its trade.** Without it a mage has no work at all: no pickaxe in the kit, no sewing
kit, so the only proposer that answers it is a trip to a shop, which by design is worth nothing. Eighty rather
than the seventy-five the eighth circle asks for, because the engine gives a nil chance at exactly the minimum:
at 75 the top circle is not hard, it is impossible. See `BotSpells/README.md`.

## Three places where a number is explained rather than chosen

**A weapon comes with its skill.** `BotWeaponOption` holds the weapon, the skill, the target and the ammunition
as one fact, because it is one fact. In the first version the profile trained `Swords` and handed out war maces
at random, so a third of the melee bots spent their lives swinging something they could not use — and the gear
appraiser, comparing damage numbers, approved. Damage says what a weapon does on a hit; whether it hits is the
skill's business.

**The weapon target sits above `Tactics`.** The opening points go to the three highest targets in order, and in
the first version `Swords` and `Tactics` were both at 70 — a tie resolved by a dictionary's enumeration order.
Half the warriors on the shard spent their best fifty points on Tactics and could not hit anybody.

**And the tie is now resolved rather than worked around.** Until this was fixed, those warnings were the only
protection: `List.Sort` is an unstable sort, so with the mage's Magery and Inscribe both at eighty the best
fifty points could have gone to writing. A tie now breaks on **declaration order**: a class writes its skills in
the order they matter to it, and that is the only thing a class can say about it.

**The crafter's `Mining` is higher, and its main skill is `Blacksmith`.** Smelting ore into ingots is a `Mining`
check, so a smith with low mining burns the ore and never reaches the anvil at all: a measured smith at `Mining`
26 turned two ore in a hundred into ingots. So mining has to come first. But while the trade was inferred from
the largest target, the champion of smiths came out a grandmaster **miner** holding an apprentice's hammer. So
`MainSkill` is declared outright rather than derived.

## What a class is for, and what that does to a price

A class never commands a bot, and it still has to be visible in what the bot does: a healer that spends its
day looking for fights is not a healer, whatever its skills say. Two mechanisms carry that, and both read only
what the classes already declare.

**The calling** (`BotCalling`, 14.09.2026). A class names the skills it works towards (`BotClass.Wants`); a piece
of work names the skill it trains (`BotDeed.Trains`). Work that trains one of the class's skills is its *own
trade* and is worth ×1.3; work that trains a skill some other class is for is *another's trade* and is worth ×0.6;
work that trains nothing, or a skill no class claims — selling, carrying, looking for a fight, standing for the
guild, cooking — is *anybody's* and is left alone. The multiplier sits outside the appraisal's fifth root, because
inside it a statement about who the bot is would be flattened like a consideration about a place. Both numbers
are dials (`OwnTrade`, `OtherTrade`, in `bot-will.json`), and the five-minute `Roles:` line and Argus's `roles`
verb report how every class's working minutes actually divide.

Measured before it existed, on the morning of 14.09.2026: crafters spent 73% of their working minutes on their own
trade and gatherers 67%, healers 10%, warriors 12%, mages 15%. Almost none of the gap was another class's work; it
was anybody's work, above all looking for a fight and not finding one. The calling fixes the smaller half of that.
The larger half — giving fighters and healers something role-true to do — is open; see `RESEARCH-decisions.md` §6.

**What a class learns first** (`BookFirst`, 14.09.2026). A caster's book used to be filled cheapest gap first, and
because spell ids run by circle that meant Clumsy, Create Food and Feeblemind before anything a healer or a mage
fights or mends with — a healer with fifteen gold was measured spending it on six curses while short of Greater Heal.
Now a class names the spells it wants first: the healer cures, protection and the greater heals, the mage the direct
damage of each circle and protection, the warrior-mage a short list of the same. The rest of the book is still filled
cheapest first.

**The safety rules** — `Sworn` (the only trades a class may be offered) and `DefendsOnly` (a medic does not go
looking for a fight). The second was written for the King's Rangers' healer and went with the rangers on
02.09.2026, which left the rule enforced by `BotHunter` and set by nobody. `BotHealer` answers it again now,
through the `BotHealer.Defends` dial.

## What the configuration may change

`Configuration/bot-classes.json` overrides any **number**: stats, skill targets, potion limits, talent
intervals, crit chance, mana regeneration, the plate ban. The file writes itself on first boot — nine names and
not one value, so by default it changes nothing.

What the configuration **cannot** touch: weapon lists, tools, the contents of a spellbook. Those are not
numbers, they are what a class *is* — and a file able to empty a smith's tool list would be a file able to
deliberately reproduce the first version's worst defect.

## Loose ends

- **The mana potion is now brewable** — by the healer and the mage. In the first version it deliberately was not
  brewable and only arrived by ship; the item is out of era, the engine's `DefAlchemy` knows nothing about it,
  so it needs a recipe of its own at the alchemy step.
- **The archer's crit should be visible to the danger arithmetic.** "Fight or not" is computed as toughness ×
  damage, and an archer that quietly hits three times harder than its numbers will refuse fights it would win.
- **Two numbers are mine rather than from the specification:** the 10-minute brewing threshold for everybody
  (the mage's number, since alchemy is open to all) and the weapon target of 40 for the crafter and gatherer.
- **Six of the eight potion families are declared and unused.** `PotionLimits` covers all eight; only Heal and
  Cure are issued and drunk (see `BotMend/`). The other six are buffs and weapons with nothing to use them.
