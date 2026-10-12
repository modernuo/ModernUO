using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Items;
using Server.Mobiles;
using Server.Spells;

namespace Server.BotAI.V2;

/// <summary>
/// What a bot must carry before it goes into a fight it chose — a dungeon's delve, or a war company sent against somebody
/// else's ground — and the guild's money for what it cannot buy itself.
///
/// <para>
/// <b>Patrick's order of 26.09.2026: "supply and the purchase of every consumable needed — THIS IS COMPULSORY. A mage may
/// not go without reagents for its casts, a healer without bandages, and the other classes have no right to go without
/// bottles and bandages either. Set everybody an entry threshold."</b> Until then nothing at any door asked what a bot
/// carried: the delve, the war company and the hunt asked only its health and its class. The proving ground showed what
/// that costs once the world is awake — a healer with no bandage, mages with no reagents, archers with a dozen arrows,
/// all losing to one orcish mage.
/// </para>
///
/// <para>
/// <b>The threshold is the class's own kit, at a share.</b> Bandages: half the kit's. Bottles: one of each kind the class
/// carries. Ammunition: half the quiver it was born with. Reagents: <see cref="Casts"/> casts of its key spell — a caster's
/// strongest attack in its book, a healer's greatest heal and its cure, a warrior-mage's heal. Every share sits at or below
/// the point where the shopper already buys (<c>BotShopper.Short</c>), so a bot turned away is always a bot the shopper
/// will send for more.
/// </para>
///
/// <para>
/// <b>Asked at the doors a bot chooses to walk through, and at no other.</b> A delve's leader and every member it calls,
/// and a member of a war company going onto somebody else's ground. Not a solo hunt — that is how a poor bot earns the money
/// for its bandages, and closing it would lock the poor out of the thing that makes them less poor — and not the defence
/// of its own ground: a bot does not ask its pack before it defends its door.
/// </para>
/// </summary>
public static class BotProvision
{
    public static bool Running { get; set; } = true;

    /// <summary>
    /// Of the kit's bandages, the share a bot must carry. A quarter since 02.10.2026 (build 389): with the stand capped and the
    /// dues taken only above the keep (Patrick's option 1), the guild pays for less of a poor member's supplies, and a bar at
    /// half the kit would shut the companies — the island's whole income in gold — to the bots that most need them.
    /// </summary>
    public static double BandageShare { get; set; } = 0.25;

    /// <summary>Of each bottle its class carries, how many (at most the class's own count).</summary>
    public static int Bottles { get; set; } = 1;

    /// <summary>Of the quiver it was born with, the share an archer must carry.</summary>
    public static double AmmoShare { get; set; } = 0.5;

    /// <summary>How many casts of its key spell a caster must carry the reagents for.</summary>
    public static int Casts { get; set; } = 5;

    public static long Asked { get; private set; }

    public static long Turned { get; private set; }

    public static long NoCloth { get; private set; }

    public static long NoBottle { get; private set; }

    public static long NoAmmo { get; private set; }

    public static long NoHerbs { get; private set; }

    /// <summary>Restocks the guild paid for, and what it paid.</summary>
    public static long Funded { get; private set; }

    public static long FundedGold { get; private set; }

    /// <summary>Restocks the guild could not pay for either.</summary>
    public static long Unfunded { get; private set; }

    private static readonly Dictionary<int, Type[]> _herbs = [];

    private static readonly List<int> _spells = [];

    /// <summary>Whether this bot carries what the threshold asks; <paramref name="why"/> says what it lacks when it does not.</summary>
    /// <summary>Bandages cut out of cloth in the pack instead of bought. See BotWeave.Bandages.</summary>
    public static long Cut { get; private set; }

    public static bool Fit(Mobile body, out string why) => Check(body, true, out why);

    /// <summary>The same question, not counted: for the shopper, which asks it far more often than any door.</summary>
    public static bool Short(Mobile body) => !Check(body, false, out _);

    /// <summary>What a bandage, a bottle, a herb and a scroll are guessed to cost when the market has priced none.</summary>
    public static int BandageGuess { get; set; } = 5;

    public static int BottleGuess { get; set; } = 15;

    public static int HerbGuess { get; set; } = 3;

    public static int ScrollGuess { get; set; } = 10;

    /// <summary>
    /// What it would cost this bot, now, to carry what the threshold asks: the bandages, bottles and reagents it lacks at the
    /// market's price for each. Asked of a bot that has just died, with its pack on the corpse, it is the bill the next door
    /// will present — see <c>BotYield.Settle</c> (02.10.2026, build 389; Patrick's option 4). Ammunition is left out: it is
    /// bound by count and comes back with the bot.
    /// </summary>
    public static int Bill(Mobile body)
    {
        if (!Running || body is not BotMobile bot || bot.Class is not { } klass || bot.Backpack is not { } pack)
        {
            return 0;
        }

        var bill = 0;
        var kit = klass.Kit;

        if (kit.Bandages > 0)
        {
            var need = (int)Math.Ceiling(kit.Bandages * BandageShare) - pack.GetAmount(typeof(Bandage));

            if (need > 0)
            {
                bill += need * BotMarket.Worth(typeof(Bandage), BandageGuess);
            }
        }

        var bottles = BotOutfit.PotionsFor(klass);

        for (var i = 0; i < bottles.Count; i++)
        {
            var (kind, many) = bottles[i];
            var need = Math.Min(many, Bottles) - pack.GetAmount(kind);

            if (need > 0)
            {
                bill += need * BotMarket.Worth(kind, BottleGuess);
            }
        }

        if (kit.Reagents > 0)
        {
            Key(bot, klass, _spells);

            for (var s = 0; s < _spells.Count; s++)
            {
                var herbs = Herbs(bot, _spells[s]);

                for (var i = 0; i < herbs.Length; i++)
                {
                    var need = Casts - pack.GetAmount(herbs[i]);

                    if (need > 0)
                    {
                        bill += need * BotMarket.Worth(herbs[i], HerbGuess);
                    }
                }
            }
        }

        return bill;
    }

    /// <summary>
    /// What one cast of <paramref name="spell"/> costs this bot in goods: the scroll it would spend, or the herbs its book
    /// would burn, at the market's price. One at the least, so a cap reckoned from it never divides by nothing. See
    /// <c>BotSlay.Thrifty</c> (02.10.2026, build 389; Patrick's option 4).
    /// </summary>
    public static int CastCost(Mobile bot, int spell)
    {
        if (bot == null || spell < 0)
        {
            return 1;
        }

        if (BotStrike.Scroll(bot, spell) != null)
        {
            var scroll = BotGrimoire.ScrollFor(spell);

            return Math.Max(1, scroll == null ? ScrollGuess : BotMarket.Worth(scroll, ScrollGuess));
        }

        var herbs = Herbs(bot, spell);
        var cost = 0;

        for (var i = 0; i < herbs.Length; i++)
        {
            cost += BotMarket.Worth(herbs[i], HerbGuess);
        }

        return Math.Max(1, cost);
    }

    private static bool Check(Mobile body, bool count, out string why)
    {
        why = null;

        if (!Running || body is not BotMobile bot || bot.Class is not { } klass)
        {
            return true;
        }

        var pack = bot.Backpack;

        if (pack == null)
        {
            why = "no pack";

            return false;
        }

        if (count)
        {
            Asked++;
        }

        var kit = klass.Kit;

        if (kit.Bandages > 0)
        {
            var need = (int)Math.Ceiling(kit.Bandages * BandageShare);
            var have = pack.GetAmount(typeof(Bandage));

            // Cloth and scissors in the pack are bandages a bot can cut itself (BotWeave.Bandages, 29.09.2026).
            if (have < need && pack.GetAmount(typeof(Cloth)) > 0)
            {
                Cut += BotWeave.Bandages(body, need - have);
                have = pack.GetAmount(typeof(Bandage));
            }

            if (have < need)
            {
                why = $"{have} of {need} bandages";

                return Turn(count, ref _cloth);
            }
        }

        var bottles = BotOutfit.PotionsFor(klass);

        for (var i = 0; i < bottles.Count; i++)
        {
            var (kind, many) = bottles[i];
            var need = Math.Min(many, Bottles);

            if (need > 0 && pack.GetAmount(kind) < need)
            {
                why = $"no {kind.Name}";

                return Turn(count, ref _bottle);
            }
        }

        if (bot.Bond is { } bond && bond.Weapon is { Ammunition: { } quiver })
        {
            var granted = BotBinding.BoundCount(quiver, bond);
            var need = (int)Math.Ceiling(granted * AmmoShare);
            var have = pack.GetAmount(quiver);

            if (granted > 0 && have < need)
            {
                why = $"{have} of {need} {quiver.Name}";

                return Turn(count, ref _ammo);
            }
        }

        if (kit.Reagents > 0)
        {
            Key(bot, klass, _spells);

            for (var s = 0; s < _spells.Count; s++)
            {
                var herbs = Herbs(bot, _spells[s]);

                for (var i = 0; i < herbs.Length; i++)
                {
                    var have = pack.GetAmount(herbs[i]);

                    if (have < Casts)
                    {
                        why = $"{have} {herbs[i].Name} for {Casts} casts";

                        return Turn(count, ref _herb);
                    }
                }
            }
        }

        return true;
    }

    private static long _cloth;

    private static long _bottle;

    private static long _ammo;

    private static long _herb;

    private static bool Turn(bool count, ref long bucket)
    {
        if (count)
        {
            Turned++;
            bucket++;
            NoCloth = _cloth;
            NoBottle = _bottle;
            NoAmmo = _ammo;
            NoHerbs = _herb;
        }

        return false;
    }

    /// <summary>
    /// The spells whose reagents a bot must carry: a caster's strongest attack in its book; a healer's greater heal (or its
    /// heal) and its cure; anybody else whose kit carries reagents, its heal. A book that holds none of them asks for none.
    /// </summary>
    private static void Key(Mobile bot, BotClass klass, List<int> spells)
    {
        spells.Clear();

        switch (klass.Role)
        {
            case BotRole.Caster:
                {
                    var strongest = BotStrike.Strongest(bot);

                    if (strongest >= 0)
                    {
                        spells.Add(strongest);
                    }

                    break;
                }
            case BotRole.Medic:
                {
                    if (BotGrimoire.Holds(bot, BotArsenal.SpellGreaterHeal))
                    {
                        spells.Add(BotArsenal.SpellGreaterHeal);
                    }
                    else if (BotGrimoire.Holds(bot, BotArsenal.SpellHeal))
                    {
                        spells.Add(BotArsenal.SpellHeal);
                    }

                    if (BotGrimoire.Holds(bot, BotArsenal.SpellCure))
                    {
                        spells.Add(BotArsenal.SpellCure);
                    }

                    break;
                }
            default:
                {
                    if (BotGrimoire.Holds(bot, BotArsenal.SpellHeal))
                    {
                        spells.Add(BotArsenal.SpellHeal);
                    }

                    break;
                }
        }
    }

    /// <summary>The reagents one cast of a spell takes, read off the engine's own spell once and kept.</summary>
    private static Type[] Herbs(Mobile bot, int spell)
    {
        if (_herbs.TryGetValue(spell, out var herbs))
        {
            return herbs;
        }

        herbs = SpellRegistry.NewSpell(spell, bot, null)?.Reagents ?? [];
        _herbs[spell] = herbs;

        return herbs;
    }

    /// <summary>
    /// The key spells (<see cref="Key"/>) and, for a class that is not a caster but carries reagents, the attack it should be
    /// stocking too: the highest attack its book holds at or below <see cref="BotStrike.Stock"/>'s circle, so its Magery and its
    /// pool decide, and nothing when Stock has nothing. The spells whose herbs <see cref="Floor"/> asks for.
    ///
    /// <para>
    /// <b>A healer's one attack was nobody's errand (08.10.2026, cast30).</b> The Healer's kit is heal, cure and magic arrow, the
    /// Warrior-Mage's heal and magic arrow, and magic arrow burns sulphurous ash — which neither class's key spells name. So the
    /// floor asked a healer for garlic, ginseng and silk and a warrior-mage for its heal's herbs, and the ash came only out of a
    /// purse above its keep, which a poor bot has not got. In the save of 01:35 on 08.10.2026, 2 of 21 Healers carried any ash and
    /// 15 of 26 Warrior-Mages had herbs for none of their attacks; in the arena that night 17 of 19 Healer bouts threw nothing.
    /// </para>
    ///
    /// <para>
    /// <b>Who reads it, and who does not (cast30 reviews A2/B F3).</b> <see cref="Floor"/> carries it, and so do its readers:
    /// <c>BotShopper.ByPurse</c> (the attack's herbs are bought out of the bot's own purse whatever its keep — never out of the
    /// guild's: <c>Fund</c> is sized by <see cref="DoorFloor"/>), <c>BotShopper.Wanting</c> (floor reagents first),
    /// <c>BotAskFirst.Sized</c> (the same purse sum without the guild) and <c>BotAskFirst.Urgent</c> (a bot with none of its attack's
    /// herbs walks to a counter rather than waiting on a carrier — kept: a caster with nothing to throw is as urgent as one with no
    /// heal), and <c>BotQuartermaster</c>'s surplus allowance (no change: the kit's keep of 30 is above it). <b>Not</b> the guild
    /// store: <c>BotGuildStore.Lacks</c> and <c>Issue</c> read <see cref="DoorFloor"/>, so the store never issues the attack's herbs
    /// as a debt that would stand in front of the next heal-kit draw. Nor the doors: <see cref="Check"/> and <see cref="Bill"/>
    /// read <see cref="Key"/> alone, so no healer is turned away from a delve for want of ash. No reagent is handed out by any of
    /// this.
    /// </para>
    /// </summary>
    private static void Armed(Mobile bot, BotClass klass, List<int> spells)
    {
        Key(bot, klass, spells);

        if (klass.Role == BotRole.Caster)
        {
            return;
        }

        var attack = BotStrike.Stocked(bot);

        if (attack >= 0 && !spells.Contains(attack))
        {
            spells.Add(attack);
        }
    }

    /// <summary>
    /// <see cref="Floor"/> without the attack <see cref="Armed"/> adds for a class that is not a caster: the doors' own threshold, the
    /// part the guild pays for (<c>BotShopper.ByPurse</c>) and the guild store issues (<c>BotGuildStore</c>). Cast30 reviews A1/B F2:
    /// the guild was paying for a poor healer's ash while the garlic that made it needy stayed unbought.
    /// </summary>
    public static int DoorFloor(Mobile body, Type kind) => Floor(body, kind, false);

    /// <summary>
    /// How many of this kind the threshold asks this bot to carry: a quarter of the kit's bandages, a bottle of each kind its class
    /// carries, half the quiver it was born with, <see cref="Casts"/> of each reagent its key spells burn — and, for a class that
    /// is not a caster, its strongest attack's (<see cref="Armed"/>, 08.10.2026, cast30); nought for anything else. Read by the
    /// shopper, which buys up to this whatever the purse and the rest of the kit only above the keep (<c>BotShopper.ByPurse</c>,
    /// 02.10.2026, build next).
    /// </summary>
    public static int Floor(Mobile body, Type kind) => Floor(body, kind, true);

    private static int Floor(Mobile body, Type kind, bool armed)
    {
        if (!Running || kind == null || body is not BotMobile bot || bot.Class is not { } klass)
        {
            return 0;
        }

        var kit = klass.Kit;

        if (kind == typeof(Bandage))
        {
            return kit.Bandages > 0 ? (int)Math.Ceiling(kit.Bandages * BandageShare) : 0;
        }

        var bottles = BotOutfit.PotionsFor(klass);

        for (var i = 0; i < bottles.Count; i++)
        {
            if (bottles[i].Item1 == kind)
            {
                return Math.Min(bottles[i].Item2, Bottles);
            }
        }

        if (bot.Bond is { } bond && bond.Weapon is { Ammunition: { } quiver } && quiver == kind)
        {
            return (int)Math.Ceiling(BotBinding.BoundCount(quiver, bond) * AmmoShare);
        }

        if (kit.Reagents > 0 && typeof(BaseReagent).IsAssignableFrom(kind))
        {
            if (armed)
            {
                Armed(bot, klass, _spells);
            }
            else
            {
                Key(bot, klass, _spells);
            }

            for (var s = 0; s < _spells.Count; s++)
            {
                var herbs = Herbs(bot, _spells[s]);

                for (var i = 0; i < herbs.Length; i++)
                {
                    if (herbs[i] == kind)
                    {
                        return Casts;
                    }
                }
            }
        }

        return 0;
    }

    /// <summary>Whether this is one of the things the threshold asks for: bandages, bottles, reagents, ammunition.</summary>
    public static bool Consumable(Type kind) =>
        kind != null && (kind == typeof(Bandage) || typeof(BasePotion).IsAssignableFrom(kind)
            || typeof(BaseReagent).IsAssignableFrom(kind) || kind == typeof(Arrow) || kind == typeof(Bolt));

    /// <summary>
    /// Makes sure this bot can pay <paramref name="cost"/>: what it lacks comes out of its guild's chest (tolls and tithes),
    /// then from its guildmates (<c>BotGuilds.Stand</c>), into its bank, where every purchase on this shard is paid from.
    /// False when neither could cover it, or the bot has no guild.
    /// </summary>
    public static bool Fund(Mobile body, int cost)
    {
        var short0 = cost - BotYield.Spendable(body);

        if (short0 <= 0)
        {
            return true;
        }

        if (body is not BotMobile bot || bot.Guild is not Guild guild)
        {
            Unfunded++;

            return false;
        }

        var paid = 0;

        // Only what the chest can spare above what its guild is saving for: supplies come out of the members' spare as they
        // always did, and the dues saved for a hall stay saved. See BotDues.Goal (29.09.2026).
        var spare = BotDues.Spare(guild);

        if (spare < short0)
        {
            BotDues.Keep(guild, short0, spare);
        }

        var drawn = BotChest.Draw(guild.Name, Math.Min(short0, spare));

        if (drawn > 0)
        {
            if (!Banker.Deposit(bot, drawn))
            {
                bot.Backpack?.TryDropItem(bot, new Gold(drawn), false);
            }

            paid += drawn;
        }

        var rest = short0 - drawn;

        if (rest > 0)
        {
            if (!BotGuilds.Stand(bot, rest))
            {
                if (paid > 0)
                {
                    BotYield.Aside(bot, -paid);
                    Funded++;
                    FundedGold += paid;
                }

                Unfunded++;

                return false;
            }

            paid += rest;
        }

        // Handed over by the guild, and not the takings of whatever the bot is in the middle of. See BotYield.Aside.
        BotYield.Aside(bot, -paid);
        Funded++;
        FundedGold += paid;

        return true;
    }

    public static string Describe() =>
        !Running
            ? "no supplies are asked at any door"
            : $"supplies: asked {Asked} times at a door and {Turned} turned away ({NoCloth} for bandages, {NoBottle} for bottles, "
            + $"{NoAmmo} for ammunition, {NoHerbs} for reagents); the guild paid {Funded} restocks, {FundedGold}gp in all, "
            + $"and could not pay {Unfunded}";

    public static void Forget()
    {
        Asked = 0;
        Turned = 0;
        _cloth = _bottle = _ammo = _herb = 0;
        NoCloth = NoBottle = NoAmmo = NoHerbs = 0;
        Funded = 0;
        FundedGold = 0;
        Unfunded = 0;
    }
}
