using ModernUO.Serialization;

namespace Server.Items;

[Furniture]
[Flippable(0xEBB, 0xEBC)]
[SerializationGenerator(0, false)]
public partial class TallMusicStand : Item
{
    [Constructible]
    public TallMusicStand() : base(0xEBB) => Weight = 10.0;
}

[Furniture]
[Flippable(0xEB6, 0xEB8)]
[SerializationGenerator(0, false)]
public partial class ShortMusicStand : Item
{
    [Constructible]
    public ShortMusicStand() : base(0xEB6) => Weight = 10.0;
}
