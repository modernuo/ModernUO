using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Vines : Item
{
    [Constructible]
    public Vines() : this(Utility.Random(8))
    {
    }

    [Constructible]
    public Vines(int v) : base(0xCEB + Math.Clamp(v, 0, 7))
    {
    }

    public override double DefaultWeight => 1.0;

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;
}
