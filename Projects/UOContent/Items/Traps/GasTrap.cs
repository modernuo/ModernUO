using System;
using ModernUO.Serialization;

namespace Server.Items;

public enum GasTrapType
{
    NorthWall,
    WestWall,
    Floor
}

[SerializationGenerator(0, false)]
public partial class GasTrap : BaseTrap
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Poison _poison;

    [Constructible]
    public GasTrap() : this(Poison.Lesser)
    {
    }

    [Constructible]
    public GasTrap(Poison poison) : this(GasTrapType.Floor, poison)
    {
    }

    [Constructible]
    public GasTrap(GasTrapType type, Poison poison = null) : base(GetBaseID(type)) => Poison = poison;

    [CommandProperty(AccessLevel.GameMaster)]
    public GasTrapType Type
    {
        get => ItemID switch
        {
            0x113C => GasTrapType.NorthWall,
            0x1147 => GasTrapType.WestWall,
            0x11A8 => GasTrapType.Floor,
            _      => GasTrapType.WestWall
        };
        set => ItemID = GetBaseID(value);
    }

    public override bool PassivelyTriggered => false;
    public override TimeSpan PassiveTriggerDelay => TimeSpan.Zero;
    public override int PassiveTriggerRange => 0;
    public override TimeSpan ResetDelay => TimeSpan.FromSeconds(0.0);

    public static int GetBaseID(GasTrapType type)
    {
        return type switch
        {
            GasTrapType.NorthWall => 0x113C,
            GasTrapType.WestWall  => 0x1147,
            GasTrapType.Floor     => 0x11A8,
            _                     => 0
        };
    }

    public override void OnTrigger(Mobile from)
    {
        if (Poison == null || !from.Player || !from.Alive || from.AccessLevel > AccessLevel.Player)
        {
            return;
        }

        Effects.SendLocationEffect(Location, Map, GetBaseID(Type) - 2, 16, 3, GetEffectHue());
        Effects.PlaySound(Location, Map, 0x231);

        from.ApplyPoison(from, Poison);

        from.LocalOverheadMessage(MessageType.Regular, 0x22, 500855); // You are enveloped by a noxious gas cloud!
    }
}
