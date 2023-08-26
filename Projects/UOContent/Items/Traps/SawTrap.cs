using System;
using Server.Spells;

namespace Server.Items;

public enum SawTrapType
{
    WestWall,
    NorthWall,
    WestFloor,
    NorthFloor
}

public class SawTrap : BaseTrap
{
    [Constructible]
    public SawTrap(SawTrapType type = SawTrapType.NorthFloor) : base(GetBaseID(type))
    {
    }

    public SawTrap(Serial serial) : base(serial)
    {
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public SawTrapType Type
    {
        get
        {
            return ItemID switch
            {
                0x1103 => SawTrapType.NorthWall,
                0x1116 => SawTrapType.WestWall,
                0x11AC => SawTrapType.NorthFloor,
                0x11B1 => SawTrapType.WestFloor,
                _      => SawTrapType.NorthWall
            };
        }
        set => ItemID = GetBaseID(value);
    }

    public override bool PassivelyTriggered => false;
    public override TimeSpan PassiveTriggerDelay => TimeSpan.Zero;
    public override int PassiveTriggerRange => 0;
    public override TimeSpan ResetDelay => TimeSpan.FromSeconds(0.0);

    public static int GetBaseID(SawTrapType type)
    {
        return type switch
        {
            SawTrapType.NorthWall  => 0x1103,
            SawTrapType.WestWall   => 0x1116,
            SawTrapType.NorthFloor => 0x11AC,
            SawTrapType.WestFloor  => 0x11B1,
            _                      => 0
        };
    }

    public override void OnTrigger(Mobile from)
    {
        if (!from.Alive || from.AccessLevel > AccessLevel.Player)
        {
            return;
        }

        Effects.SendLocationEffect(Location, Map, GetBaseID(Type) + 1, 6, 3, GetEffectHue());
        Effects.PlaySound(Location, Map, 0x21C);

        SpellHelper.Damage(TimeSpan.FromTicks(1), from, from, Utility.RandomMinMax(5, 15));

        from.LocalOverheadMessage(MessageType.Regular, 0x22, 500853); // You stepped onto a blade trap!
    }

    public override void Serialize(IGenericWriter writer)
    {
        base.Serialize(writer);

        writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
        base.Deserialize(reader);

        var version = reader.ReadInt();
    }
}