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
    public Vines(int v) : base(0xCEB)
    {
        if (v is < 0 or > 7)
        {
            v = 0;
        }

        ItemID += v;
        Weight = 1.0;
    }

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;
}
