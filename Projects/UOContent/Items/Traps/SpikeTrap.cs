using System;
using ModernUO.Serialization;
using Server.Spells;

namespace Server.Items;

public enum SpikeTrapType
{
    WestWall,
    NorthWall,
    WestFloor,
    NorthFloor
}

[SerializationGenerator(0, false)]
public partial class SpikeTrap : BaseTrap
{
    [Constructible]
    public SpikeTrap(SpikeTrapType type = SpikeTrapType.WestFloor) : base(GetBaseID(type))
    {
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public SpikeTrapType Type
    {
        get
        {
            return ItemID switch
            {
                4360 => SpikeTrapType.WestWall,
                4361 => SpikeTrapType.WestWall,
                4366 => SpikeTrapType.WestWall,
                4379 => SpikeTrapType.NorthWall,
                4380 => SpikeTrapType.NorthWall,
                4385 => SpikeTrapType.NorthWall,
                4506 => SpikeTrapType.WestFloor,
                4507 => SpikeTrapType.WestFloor,
                4511 => SpikeTrapType.WestFloor,
                4512 => SpikeTrapType.NorthFloor,
                4513 => SpikeTrapType.NorthFloor,
                4517 => SpikeTrapType.NorthFloor,
                _    => SpikeTrapType.WestWall
            };
        }
        set => ItemID = Extended ? GetExtendedID(value) : GetBaseID(value);
    }

    public bool Extended
    {
        get => ItemID == GetExtendedID(Type);
        set => ItemID = value ? GetExtendedID(Type) : GetBaseID(Type);
    }

    public override bool PassivelyTriggered => false;
    public override TimeSpan PassiveTriggerDelay => TimeSpan.Zero;
    public override int PassiveTriggerRange => 0;
    public override TimeSpan ResetDelay => TimeSpan.FromSeconds(6.0);

    public static int GetBaseID(SpikeTrapType type)
    {
        return type switch
        {
            SpikeTrapType.WestWall   => 4360,
            SpikeTrapType.NorthWall  => 4379,
            SpikeTrapType.WestFloor  => 4506,
            SpikeTrapType.NorthFloor => 4512,
            _                        => 0
        };
    }

    public static int GetExtendedID(SpikeTrapType type) => GetBaseID(type) + GetExtendedOffset(type);

    public static int GetExtendedOffset(SpikeTrapType type)
    {
        return type switch
        {
            SpikeTrapType.WestWall   => 6,
            SpikeTrapType.NorthWall  => 6,
            SpikeTrapType.WestFloor  => 5,
            SpikeTrapType.NorthFloor => 5,
            _                        => 0
        };
    }

    public override void OnTrigger(Mobile from)
    {
        if (!from.Alive || from.AccessLevel > AccessLevel.Player)
        {
            return;
        }

        Effects.SendLocationEffect(Location, Map, GetBaseID(Type) + 1, 18, 3, GetEffectHue());
        Effects.PlaySound(Location, Map, 0x22C);

        foreach (var mob in GetMobilesInRange(0))
        {
            if (mob.Alive && !mob.IsDeadBondedPet)
            {
                SpellHelper.Damage(TimeSpan.FromTicks(1), mob, mob, Utility.RandomMinMax(1, 6) * 6);
            }
        }

        Timer.StartTimer(TimeSpan.FromSeconds(1.0), OnSpikeExtended);

        from.LocalOverheadMessage(MessageType.Regular, 0x22, 500852); // You stepped onto a spike trap!
    }

    public virtual void OnSpikeExtended()
    {
        Extended = true;
        Timer.StartTimer(TimeSpan.FromSeconds(5.0), OnSpikeRetracted);
    }

    public virtual void OnSpikeRetracted()
    {
        Extended = false;
        Effects.SendLocationEffect(Location, Map, GetExtendedID(Type) - 1, 6, 3, GetEffectHue());
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        Extended = false;
    }
}
