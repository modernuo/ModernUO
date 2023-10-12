using ModernUO.Serialization;

namespace Server.Engines.Quests.Doom;

[SerializationGenerator(0, false)]
public partial class ChylothShroud : Item
{
    [Constructible]
    public ChylothShroud() : base(0x204E)
    {
        Hue = 0x846;
        Layer = Layer.OuterTorso;
    }
}
