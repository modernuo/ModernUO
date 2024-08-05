using ModernUO.Serialization;

namespace Server.Items.Misc.Corpses;

[SerializationGenerator(0, false)]
public partial class CorpseFacialHair
{
    [SerializableField(0)]
    int _itemID;

    [SerializableField(1)]
    int _hue;

    [DirtyTrackingEntity]
    public Corpse Owner { get; set; }

    public CorpseFacialHair(Corpse owner, int itemID=0, int hue=0)
    {
        Owner = owner;
        _itemID = itemID;
        _hue = hue;
        VirtualSerial = World.NewVirtual;
    }

    public Serial VirtualSerial { get; private set; } = Serial.Zero;
}
