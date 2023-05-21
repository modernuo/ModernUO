using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SwarmOfFlies : Item
{
    [Constructible]
    public SwarmOfFlies() : base(0x91B)
    {
        Hue = 1;
        Movable = false;
    }

    public override string DefaultName => "a swarm of flies";
}
