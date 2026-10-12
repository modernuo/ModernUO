# Issuing the kit, and the bind

Turns a class's description (`BotClasses/`) into things a bot is actually holding, and decides what death has no
right to take from it.

## Layout

| File | What is in it |
|---|---|
| `BotOutfit.cs` | the issuing step: the weapon roll, the order of equipping, tools, supplies, potions |
| `BotBinding.cs` | what "bound" means: the flag, zero weight, the stack count, giving back what was lost, and the market's two doors that turn it away |
| `BotBond.cs` | the record of what a bot was issued. Lives **on the bot**, not in a table |
| `BotCasterStaff.cs` | the casting staff, blue or green — the colour comes from the class |
| `BotBrawlerGloves.cs` | the gloves a brawler fights in |
| `BotHarness.cs` | what a bot ought to be wearing, worked out from what this shard can actually make and what it costs |
| `BotStable.cs` | buying a horse, and calling it up; keeps back `BotPurse.KeepBack` (build 64) |
| `BotSteed.cs` | a horse a bot carries in its pack and calls up when it has somewhere to be |
| `BotChampionSteed.cs` | A champion's steed: a mount of a kind no stable sells, won in the championship. |

**Eight kinds of reagent are issued, not six.** Six were chosen to match exactly the three starting spells, and
that was right while a book held three spells and could hold no more. Without bloodmoss and mandrake root half a
full book is unspeakable and almost nothing can be inscribed — see `BotSpells/README.md`.

The scribe's pen goes to whoever's build wants `Inscribe`, by the same rule that gives the mortar to whoever's
build wants alchemy. A trade is open to anybody with the skill; what a class has is the disposition, and a skill
target is exactly that.

No configuration file: the only numbers here are the bottle count for a brewer and what the class's kit already
states. When there is a dial there will be a file.

## The bind is three promises, and it is only for the weapon

**It does not weigh.** Past `40 + 3.5 × Str` stones the engine charges five stamina or more for **every single
step**, and refuses the step outright at zero. An overloaded bot exhausts itself in a dozen paces and then stands
there for the rest of the shard's life. In the first version **three bots stood exactly that way for a whole
session** while the log reported six hundred times that the ground was clear and the engine approved of the step.
It did approve. Stamina was not in the message. For a gatherer, whose whole job is filling a pack, gear that
weighs is ore that cannot be carried.

**It is not lost on death.** A weapon is what a bot gets back up with. The first version's smith that lost its
hammer stopped being a smith: it could not forge, could not take commissions, and quietly saw out its life
hitting skeletons like everybody else. A whole mechanism was built under that — spare tools in a bank box and a
trip across Britain for a new one. **The bind does not improve it, it abolishes it.**

**It is not merchandise.** Bound things are never sold, never listed, never scrapped. Without that rule zero
weight becomes a hole — a bot would carry a free anvil to market — and worse, the scrapper would eat the hammer:
"destroy it" was the first version's last answer to anything nobody would buy.

Asked in one line: `BotBinding.IsBound(item, bond)`. And the engine enforces the third promise for free:
`OnSellItems` requires `IsStandardLoot()`, which a `Newbied` item is not.

## What is bound and what is not

**The weapon, its ammunition, the casting staff and the spellbook** are bound. **Tools are not.**

Patrik's decision, and it turns the smith-with-no-hammer argument on its head. A tool is a thing that **wears
through**: the engine gives a fresh one 25 to 75 uses, spends one an attempt and destroys it at zero. That is the
mechanic, not a defect in it, and the honest answer to a smith with no hammer is not an unbreakable hammer but a
smith walking to a shop with its own money.

Three things follow, and all three are wanted. A tool **weighs** — but a pickaxe is 11 stones against a
gatherer's ceiling of 232, six per cent rather than the disaster zero weight was written for (an archer's 150
arrows stay bound, because ammunition comes with the weapon). A tool **drops into the corpse** — so death costs a
hammer as well. And a tool **has to be bought** — so the shard has a reason to keep toolmakers, and a trade has a
floor under what it costs to run.

**The one thing that must never be allowed is the silent version.** A bot whose tool has run out and does not
know to replace it looks exactly like a bot that never had one: the proposer simply stops offering the work, and
nothing in the log looks wrong. So `BotOutfit.ToolsFor(klass)` is **one list asked by two callers**: birth issues
from it and `BotShopper` reads the same one to notice a loss and buy another, at the highest priority, above
bandages. A second list of "which tools does a mage need" kept next to the shopping code would disagree with this
one the first time a class changed.

`BotOutfit.PotionsFor(klass)` works the same way for the bottles, from the class's own `PotionLimits`.

The mortar and the pen are **derived from the build** rather than named in the kit: brewing and writing are open
to anybody with the skill and the material, so what a class has is the disposition — and a skill target is
exactly that.

## The arrow count

The engine's `LootType.Newbied` flag can only say "this whole object". It cannot express a partial stack, and
that is not a quibble: **stacks merge.** A hundred bound arrows and fifty bought ones become one stack of a
hundred and fifty with one flag between them, and whichever way the merge set that flag it is wrong for half the
stack. So ammunition is bound by **count**, not by flag.

The rule: after death the bot keeps `min(carried, granted)`. A ceiling, not a refill.

| Granted at birth | Carried at death | Kept by the bot | Left in the corpse |
|---|---|---|---|
| 150 | 1 (spent 149) | **1** | 0 |
| 150 | 150 | **150** | 0 |
| 150 | 350 (bought 200 more) | **150** | 200 |
| 150 | 0 | **0** | 0 |

Computed in `BotBinding.TrimAmmunition`, from the bot's own death hook, and that is where the reason it is
reliable lives: by the time `OnDeath(Container c)` runs, the corpse is **already assembled**. So "how much is the
bot carrying" is readable from both sides at once — pack plus corpse — and the arithmetic does not depend on which
way a stack merge turned the flag. That independence is the point: the flag on a merged stack is the one thing
here that cannot be relied on.

Verified against the first version's code: `BotExpedition.ClearCorpse(bot, c)` is called from `OnDeath` and
deletes the corpse and its contents entirely — so the corpse exists and is full at that moment.

## What the bot calls

| Where | What to call |
|---|---|
| at birth | `Bond = BotOutfit.Give(this, klass)` |
| `OnDeath(Container c)` | `BotBinding.TrimAmmunition(this, Bond, c)` |
| on resurrection | `BotBinding.Restore(this, Bond)` |

Plus one check anywhere the economy might part a bot from something: `BotBinding.IsBound(item, bond)`.

## The bound weapon rides in the pack once a better one is in the hand

Patrick's order of 15.09.2026: a bot wields the better weapon even when it is already holding one, and the bound
weapon stays in its pack, so that a bot stripped of the better one in a war still has something to fight with.
`BotMobile.Rewield` does the first half — only within the bot's own kind (the birth weapon's skill, distance and
ammunition), only for something that lands at least `WeaponMargin` times as much a second by the engine's own
arithmetic, and never for a class whose staff gives back mana. The second half was already the bind: weightless,
kept through death, never sold. After death the better weapon is on the corpse and the bound one in the pack, and
the re-arm puts the bound one on.

What changed is where the bound weapon lives. It used to be in the hand, and every scan of a pack missed it by
construction; now it is in the pack, so the choosers that hand goods on pass it over (`BotPeddle.Gather`,
`BotSupply.Lift`), and the market's two doors refuse it outright and count it:

```
N bound things turned away from a stall or a want
```

That number should stay at nought. Anything above it names a chooser that forgot to ask.

`BotBond` deliberately **does not serialise**. The population is rebuilt from configuration on every world load —
bots returning from a save are cleaned out, because the engine's entity serialiser has no per-entity refusal — so
the bond only has to survive the process. If v2 ever starts keeping bots between restarts, this is the first
thing that will have to start being written down.

## The order of equipping is not cosmetic

The engine refuses a two-handed item while anything at all is in the other hand. So whatever needs both hands
goes on first: the staff, or the bow. Then the one-handed blade. The archer's dagger goes **in the pack**, not in
a hand: it is what the bot falls back to, not what it opens with.

Handing out the dagger before the bow cost the first version ten archers who spent their entire lives stabbing
skeletons with knives **while carrying the bows** they were training for. And it was invisible: the bow was in the
pack, exactly where a spare weapon belongs.

## What to check at home

In the log as the population is raised:

```
N bots outfitted, M things bound to their owners
```

If the shard turns out to be AOS-era there will also be one loud line saying that `Newbied` does nothing and the
only remaining guarantee is handing things back on resurrection. On a renaissance shard it should not appear.

With a client:

1. **Weight.** Take an archer and look at `Stones` in the status. The bow, the dagger and the arrows should come
   to zero; the pickaxe of a gatherer should not. Then give it ore until it is overloaded — it must keep walking
   until it has picked up more ore than the ceiling allows by itself.
2. **An archer's death.** Note the arrow count, kill it, raise it. It should have the same number back — not a
   hundred and fifty. Then buy it more arrows than it was issued and kill it again: the surplus should be lying
   in the corpse rather than gone.
3. **A crafter's death.** The weapon comes back up with it; the hammer, pickaxe and sewing kit **do not** — they
   are in the corpse, and `BotShopper` should then be seen buying replacements.
4. **The staff.** A mage should be standing **with the staff in its hands** and the book in its pack. Blue for the
   mage, green for the healer. If the staff ended up in the pack the strength requirement did not pass after
   all — look at `BotCasterStaff.CasterStrRequirement`.

## The one unknown, now verified

`Item.Weight = 0.0` is a **real setter**, read out of the fork at `Projects/Server/Items/Item.cs:474`, and it does
exactly what is needed (it stores the weight in the item's compact record, where `-1` means "take it from the
type"). So `BotBinding.Weightless` compiles and works, and the workaround of a subclass per issued type is
unnecessary.

The call is still kept on one line of its own. Not out of caution, but because it is exactly one decision of this
project — "what is issued does not weigh" — and it should read in one place.
