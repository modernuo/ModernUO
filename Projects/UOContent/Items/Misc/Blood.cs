using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Blood : Item
{
    [Constructible]
    public Blood() : this(Utility.RandomList(0x1645, 0x122A, 0x122B, 0x122C, 0x122D, 0x122E, 0x122F))
    {
    }

    [Constructible]
    public Blood(int itemID) : base(itemID)
    {
        Movable = false;
        Timer.StartTimer(TimeSpan.FromSeconds(5), Delete);
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        Timer.StartTimer(TimeSpan.FromSeconds(5), Delete);
    }
}
