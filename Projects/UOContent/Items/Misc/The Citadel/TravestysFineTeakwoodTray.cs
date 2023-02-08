using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class TravestysFineTeakwoodTray : Item
{
    [Constructible]
    public TravestysFineTeakwoodTray() : base(Utility.Random(0x991, 2))
    {
    }

    public override int LabelNumber => 1075094; // Travesty's Fine Teakwood Tray
}
