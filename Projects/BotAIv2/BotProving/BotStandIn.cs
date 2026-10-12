using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// A bot's double for one fight on the proving ground: the same body, build, skills and kit, and nothing that makes it one
/// of the population. See <see cref="BotProving"/>.
///
/// <para>
/// <b>A <see cref="PlayerMobile"/>, not a <see cref="BotMobile"/>, and that is the first decision rather than a detail.</b>
/// Everything the population does with a bot is keyed on that type: a blow on a BotMobile is written into the peril map and
/// the quadrants' record, a death is filed with the Will, the squads, the wars and the guilds' regard, and the dashboards,
/// the census and Argus's own reports walk the population by it. A double that was a BotMobile would teach the island that
/// Green Acres is deadly every time it lost a fight. Argus himself inherits PlayerMobile for the same reason.
/// </para>
///
/// <para>
/// <b>What is copied is what decides a fight and nothing more:</b> the raw stats (a player's hit points on this era are
/// Str/2 + 50, see <c>PlayerMobile.HitsMax</c>), every skill's base and cap, everything worn, and from the pack only what a
/// bot reaches for in a fight — bandages, bottles, reagents, scrolls, its spellbook and its arrows. The copies are the
/// engine's own dupe (<c>Dupe.DoDupe</c>), so a vanquishing blade stays vanquishing and an invulnerable chest stays
/// invulnerable; that is the whole point of measuring with the real kit rather than with a formula.
/// </para>
///
/// <para>
/// <b>It must not outlive its fight, and it must not outlive a save.</b> The trial deletes it and its corpse when the fight
/// is over; one that comes back out of a world save — the shard stopped in the middle of a trial — deletes itself on load.
/// </para>
/// </summary>
public class BotStandIn : PlayerMobile
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotStandIn));

    /// <summary>Doubles that came back from a world save and were deleted. See <see cref="Deserialize"/>.</summary>
    public static long Returned { get; private set; }

    public BotStandIn(Serial serial) : base(serial)
    {
    }

    public BotStandIn()
    {
    }

    /// <summary>Whose double this is, by name.</summary>
    public string Of { get; private set; }

    /// <summary>Damage the engine applied to it in the fight, never more than it had standing.</summary>
    public int Taken { get; private set; }

    /// <summary>Health given back in the fight — bandage, bottle or spell — as far as there was room for it.</summary>
    public int Healed { get; private set; }

    /// <summary>Things copied onto it: worn, and carried into the fight.</summary>
    public int Copied { get; private set; }

    public override void OnDamage(int amount, Mobile from, bool willKill)
    {
        base.OnDamage(amount, from, willKill);

        // Called before the hit points are lowered (Mobile.OnDamage), so what it had standing is still there to cap it.
        Taken += Math.Max(0, Math.Min(amount, Hits));
    }

    public override void OnHeal(ref int amount, Mobile from)
    {
        base.OnHeal(ref amount, from);

        // The engine clamps the heal to the room left after this call (Mobile.Heal); count the same thing it will apply.
        Healed += Math.Max(0, Math.Min(amount, HitsMax - Hits));
    }

    /// <summary>
    /// Makes the double: the original's body and face, its raw stats and skills, a copy of everything it wears and of what it
    /// fights with out of its pack, put down at <paramref name="at"/> with full health, stamina and mana. Null if the
    /// original is not a body that can be copied.
    /// </summary>
    public static BotStandIn Copy(BotMobile bot, Map map, Point3D at)
    {
        if (bot is not { Deleted: false } || map == null || map == Map.Internal)
        {
            return null;
        }

        var double_ = new BotStandIn
        {
            // The flag the engine reads a player's death by. Without it a body that dies is deleted rather than left a
            // ghost, which would do no harm here — but the engine also reads "alive" off it, and a double that reads as
            // alive after it has died is a fight the referee never calls. See BotMobile.Become.
            Player = true,
            Of = bot.Name,
            Name = $"{bot.Name}'s double",
            Female = bot.Female,
            Body = bot.Body,
            Hue = bot.Hue,
            HairItemID = bot.HairItemID,
            HairHue = bot.HairHue
        };

        double_.AddItem(new Backpack { Movable = false });

        double_.StatCap = bot.StatCap;
        double_.RawStr = bot.RawStr;
        double_.RawDex = bot.RawDex;
        double_.RawInt = bot.RawInt;

        for (var i = 0; i < bot.Skills.Length && i < double_.Skills.Length; i++)
        {
            var from = bot.Skills[i];
            var to = double_.Skills[i];

            if (from == null || to == null)
            {
                continue;
            }

            to.Cap = from.Cap;
            to.Base = from.Base;
        }

        // Worn first, then carried: a bow wants its arrows in the pack, a book its reagents.
        var worn = bot.Items;

        for (var i = 0; i < worn.Count; i++)
        {
            var item = worn[i];

            if (item == null || item.Deleted || item is Container || item.Layer is Layer.Mount or Layer.Hair or Layer.FacialHair
                or Layer.Backpack or Layer.Bank or Layer.ShopBuy or Layer.ShopResale or Layer.ShopSell)
            {
                continue;
            }

            var copy = Clone(item);

            if (copy == null)
            {
                continue;
            }

            if (double_.EquipItem(copy))
            {
                double_.Copied++;
            }
            else
            {
                copy.Delete();
            }
        }

        var pack = bot.Backpack;

        if (pack != null)
        {
            // Every level of the pack: a quiver or a pouch inside it holds arrows and bottles as surely as the pack does.
            _carried.Clear();
            Gather(pack, _carried);

            for (var i = 0; i < _carried.Count; i++)
            {
                var copy = Clone(_carried[i]);

                if (copy == null)
                {
                    continue;
                }

                double_.Backpack.DropItem(copy);
                double_.Copied++;
            }

            _carried.Clear();
        }

        double_.Closes = bot.Class?.Closes == true;
        double_.Shoots = bot.Class?.Role == BotRole.Ranged;

        double_.Hits = double_.HitsMax;
        double_.Stam = double_.StamMax;
        double_.Mana = double_.ManaMax;

        // Its weapon was put in its hand before its stamina was filled, and equipping sets the next swing a whole swing away
        // at the stamina of the moment (BaseWeapon.OnEquip, GetDelay): Cassia's first arrow waited 7.3 s against the 4.7 s
        // of every one after it (the trace, 04:33 on 26.09.2026). A bot comes to a fight with its weapon long in its hand.
        double_.NextCombatTime = Core.TickCount;

        double_.MoveToWorld(at, map);

        return double_;
    }

    /// <summary>Whether a bot reaches for this out of its pack in a fight: what it heals and casts with, what it shoots, and a spare weapon.</summary>
    private static bool Fights(Item item) =>
        item is { Deleted: false } and (Bandage or BasePotion or BaseReagent or SpellScroll or Spellbook or Arrow or Bolt or BaseWeapon);

    private static readonly List<Item> _carried = [];

    private static void Gather(Container container, List<Item> into)
    {
        var items = container.Items;

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];

            if (item is Spellbook || item is not Container inner)
            {
                if (Fights(item))
                {
                    into.Add(item);
                }

                continue;
            }

            Gather(inner, into);
        }
    }

    /// <summary>Whether the bot this doubles is of a class that draws a blade when something closes (BotClass.Closes).</summary>
    public bool Closes { get; private set; }

    /// <summary>Whether the bot this doubles is a shooter by class: it brings a bow up whenever it has something to put in it.</summary>
    public bool Shoots { get; private set; }

    /// <summary>Weapon changes the double made in its fight.</summary>
    public int Draws { get; private set; }

    /// <summary>
    /// The bot's own swap between the weapon it shoots with and the one it swings (BotMobile.Draw), for a body that is not
    /// a BotMobile: the held weapon into the pack, the best of the other kind out of it — a bow only if there is something to
    /// fire. True when the double is holding the kind asked for.
    /// </summary>
    public bool Draw(bool melee)
    {
        var pack = Backpack;

        if (pack == null || Deleted || !Alive)
        {
            return false;
        }

        var held = Weapon as Item;

        if (held != null && held.Parent != this)
        {
            held = null;
        }

        if (held is BaseWeapon inHand && inHand is BaseRanged != melee)
        {
            return true;
        }

        BaseWeapon wanted = null;
        var best = -1.0;
        var items = pack.Items;

        for (var i = 0; i < items.Count; i++)
        {
            if (items[i] is not BaseWeapon weapon || weapon is BaseRanged == melee
                || weapon.Layer is not (Layer.OneHanded or Layer.TwoHanded))
            {
                continue;
            }

            if (weapon is BaseRanged shooter && (shooter.AmmoType == null || pack.GetAmount(shooter.AmmoType) <= 0))
            {
                continue;
            }

            var worth = (weapon.MinDamage + weapon.MaxDamage) / 2.0;

            if (worth > best)
            {
                best = worth;
                wanted = weapon;
            }
        }

        if (wanted == null)
        {
            return false;
        }

        if (held != null)
        {
            pack.DropItem(held);
        }

        var shield = wanted.Layer == Layer.TwoHanded ? FindItemOnLayer(Layer.TwoHanded) : null;

        if (shield != null)
        {
            pack.DropItem(shield);
        }

        if (EquipItem(wanted))
        {
            Draws++;

            return true;
        }

        return false;
    }

    /// <summary>What the double holds, and what it has to fire, for the fight's line: the proof that it is armed as the bot is.</summary>
    public string Holding()
    {
        var weapon = Weapon as BaseWeapon;
        var name = weapon == null || weapon.Parent != this ? "nothing" : weapon.GetType().Name;
        var ammo = weapon is BaseRanged { AmmoType: { } kind } ? Backpack?.GetAmount(kind) ?? 0 : -1;

        return ammo < 0 ? name : $"{name} with {ammo} to fire";
    }

    /// <summary>The engine's own dupe of one thing, contents and all, or null if the engine cannot build it.</summary>
    private static Item Clone(Item source)
    {
        try
        {
            var ctor = source.GetType().GetConstructor(out var count);

            if (ctor == null)
            {
                return null;
            }

            object[] args = null;

            if (count > 0)
            {
                args = new object[count];
                Array.Fill(args, Type.Missing);
            }

            var copy = Dupe.DoDupe(source, ctor, args);

            if (copy == null)
            {
                return null;
            }

            // A bound thing is blessed for its owner; the double is somebody else, and what it carries goes with it.
            copy.BlessedFor = null;

            return copy;
        }
        catch (Exception e)
        {
            logger.Warning(e, "Could not copy {Item} for a double", source?.GetType().Name);

            return null;
        }
    }

    public override void Serialize(IGenericWriter writer)
    {
        base.Serialize(writer);

        writer.Write(0);
    }

    public override void Deserialize(IGenericReader reader)
    {
        base.Deserialize(reader);

        reader.ReadInt();

        // Saved in the middle of a fight. The fight did not survive the stop; neither does the double.
        Returned++;
        Timer.DelayCall(Delete);
    }
}
