using ModernUO.Serialization;
using Server.Items;

namespace Server.Engines.Quests.Doom;

[SerializationGenerator(0, false)]
public partial class ChylothStaff : BlackStaff
{
    [Constructible]
    public ChylothStaff() => Hue = 0x482;

    public override int LabelNumber => 1041111; // a magic staff
}
