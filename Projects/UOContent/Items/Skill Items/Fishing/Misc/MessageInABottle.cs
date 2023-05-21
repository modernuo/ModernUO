using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MessageInABottle : Item
{
    [SerializableField(1)]
    private Map _targetMap;

    [Constructible]
    public MessageInABottle(Map map = null) : this(map, GetRandomLevel())
    {
    }

    [Constructible]
    public MessageInABottle(Map map, int level) : base(0x099F)
    {
        Weight = 1.0;
        TargetMap = map ?? Map.Trammel;
        _level = level;
    }

    public override int LabelNumber => 1041080; // a message in a bottle

    [CommandProperty(AccessLevel.GameMaster)]
    [SerializableProperty(0)]
    public int Level
    {
        get => _level;
        set
        {
            _level = Math.Max(1, Math.Min(value, 4));
            this.MarkDirty();
        }
    }

    public static int GetRandomLevel()
    {
        if (Core.AOS && Utility.Random(25) < 1)
        {
            return 4; // ancient
        }

        return Utility.RandomMinMax(1, 3);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            ReplaceWith(new SOS(TargetMap, _level));
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 501891); // You extract the message from the bottle.
        }
        else
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
    }
}
