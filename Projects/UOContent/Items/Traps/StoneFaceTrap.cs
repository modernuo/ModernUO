using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.Spells;

namespace Server.Items;

public enum StoneFaceTrapType
{
    NorthWestWall,
    NorthWall,
    WestWall
}

[SerializationGenerator(0, false)]
public partial class StoneFaceTrap : BaseTrap
{
    [Constructible]
    public StoneFaceTrap() : base(0x10FC) => Light = LightType.Circle225;

    [CommandProperty(AccessLevel.GameMaster)]
    public StoneFaceTrapType Type
    {
        get
        {
            return ItemID switch
            {
                0x10F5 => StoneFaceTrapType.NorthWestWall,
                0x10F6 => StoneFaceTrapType.NorthWestWall,
                0x10F7 => StoneFaceTrapType.NorthWestWall,
                0x10FC => StoneFaceTrapType.NorthWall,
                0x10FD => StoneFaceTrapType.NorthWall,
                0x10FE => StoneFaceTrapType.NorthWall,
                0x110F => StoneFaceTrapType.WestWall,
                0x1110 => StoneFaceTrapType.WestWall,
                0x1111 => StoneFaceTrapType.WestWall,
                _      => StoneFaceTrapType.NorthWestWall
            };
        }
        set => ItemID = Breathing ? GetFireID(value) : GetBaseID(value);
    }

    public bool Breathing
    {
        get => ItemID == GetFireID(Type);
        set => ItemID = value ? GetFireID(Type) : GetBaseID(Type);
    }

    public override bool PassivelyTriggered => true;
    public override TimeSpan PassiveTriggerDelay => TimeSpan.Zero;
    public override int PassiveTriggerRange => 2;
    public override TimeSpan ResetDelay => TimeSpan.Zero;

    public static int GetBaseID(StoneFaceTrapType type)
    {
        return type switch
        {
            StoneFaceTrapType.NorthWestWall => 0x10F5,
            StoneFaceTrapType.NorthWall     => 0x10FC,
            StoneFaceTrapType.WestWall      => 0x110F,
            _                               => 0
        };
    }

    public static int GetFireID(StoneFaceTrapType type)
    {
        return type switch
        {
            StoneFaceTrapType.NorthWestWall => 0x10F7,
            StoneFaceTrapType.NorthWall     => 0x10FE,
            StoneFaceTrapType.WestWall      => 0x1111,
            _                               => 0
        };
    }

    public override void OnTrigger(Mobile from)
    {
        if (!from.Alive || from.AccessLevel > AccessLevel.Player)
        {
            return;
        }

        Effects.PlaySound(Location, Map, 0x359);

        Breathing = true;

        Timer.StartTimer(TimeSpan.FromSeconds(2.0), FinishBreath);
        Timer.StartTimer(TimeSpan.FromSeconds(1.0), TriggerDamage);
    }

    public virtual void FinishBreath()
    {
        Breathing = false;
    }

    public virtual void TriggerDamage()
    {
        var queue = PooledRefQueue<Mobile>.Create();
        foreach (var mob in GetMobilesInRange(1))
        {
            if (mob.Alive && !mob.IsDeadBondedPet && mob.AccessLevel == AccessLevel.Player)
            {
                queue.Enqueue(mob);
            }
        }

        while (queue.Count > 0)
        {
            var mob = queue.Dequeue();
            SpellHelper.Damage(TimeSpan.FromTicks(1), mob, mob, Utility.Dice(3, 15, 0));
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        Breathing = false;
    }
}

[SerializationGenerator(0, false)]
public partial class StoneFaceTrapNoDamage : StoneFaceTrap
{
    [Constructible]
    public StoneFaceTrapNoDamage()
    {
    }

    public override void TriggerDamage()
    {
        // nothing..
    }
}
