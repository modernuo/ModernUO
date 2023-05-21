using System;
using ModernUO.Serialization;
using Server.Spells;

namespace Server.Items;

[Flippable(0x125E, 0x1230)]
[SerializationGenerator(0)]
public partial class GuillotineComponent : AddonComponent
{
    public GuillotineComponent() : base(0x125E)
    {
    }

    public override int LabelNumber => 1024656; // Guillotine
}

[SerializationGenerator(0)]
public partial class GuillotineAddon : BaseAddon
{
    [Constructible]
    public GuillotineAddon()
    {
        AddComponent(new GuillotineComponent(), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new GuillotineDeed();

    public override void OnComponentUsed(AddonComponent c, Mobile from)
    {
        if (from.InRange(Location, 2))
        {
            if (Utility.RandomBool())
            {
                from.Location = Location;

                Timer.StartTimer(TimeSpan.FromSeconds(0.5), () => Activate(c, from));
            }
            else
            {
                // Hmm... you suspect that if you used this again, it might hurt.
                from.LocalOverheadMessage(MessageType.Regular, 0, 501777);
            }
        }
        else
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
    }

    public virtual void Activate(AddonComponent c, Mobile from)
    {
        if (c.ItemID is 0x125E or 0x1269 or 0x1260)
        {
            c.ItemID = 0x1269;
        }
        else
        {
            c.ItemID = 0x1247;
        }

        // blood
        var amount = Utility.RandomMinMax(3, 7);

        for (var i = 0; i < amount; i++)
        {
            var x = c.X + Utility.RandomMinMax(-1, 1);
            var y = c.Y + Utility.RandomMinMax(-1, 1);
            var z = c.Z;

            if (!c.Map.CanFit(x, y, z, 1, false, false))
            {
                z = c.Map.GetAverageZ(x, y);

                if (!c.Map.CanFit(x, y, z, 1, false, false))
                {
                    continue;
                }
            }

            var blood = new Blood(Utility.RandomMinMax(0x122C, 0x122F));
            blood.MoveToWorld(new Point3D(x, y, z), c.Map);
        }

        if (from.Female)
        {
            from.PlaySound(Utility.RandomMinMax(0x150, 0x153));
        }
        else
        {
            from.PlaySound(Utility.RandomMinMax(0x15A, 0x15D));
        }

        // Hmm... you suspect that if you used this again, it might hurt.
        from.LocalOverheadMessage(MessageType.Regular, 0, 501777);
        SpellHelper.Damage(TimeSpan.Zero, from, Utility.Dice(2, 10, 5));

        Timer.StartTimer(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.5), 2, () => Deactivate(c));
    }

    private static void Deactivate(AddonComponent c)
    {
        c.ItemID = c.ItemID switch
        {
            0x1269 => 0x1260,
            0x1260 => 0x125E,
            0x1247 => 0x1246,
            0x1246 => 0x1230,
            _      => c.ItemID
        };
    }
}

[SerializationGenerator(0)]
public partial class GuillotineDeed : BaseAddonDeed
{
    [Constructible]
    public GuillotineDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new GuillotineAddon();
    public override int LabelNumber => 1024656; // Guillotine
}
