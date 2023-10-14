using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Engines.Quests.Collector;

[SerializationGenerator(0, false)]
public partial class PaintedImage : Item
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private ImageType _image;

    [Constructible]
    public PaintedImage(ImageType image) : base(0xFF3)
    {
        Weight = 1.0;
        Hue = 0x8FD;

        _image = image;
    }

    public override void AddNameProperty(IPropertyList list)
    {
        var info = ImageTypeInfo.Get(_image);
        list.Add(1060847, $"{1055126:#}\t{info.Name:#}"); // a painted image of:
    }

    public override void OnSingleClick(Mobile from)
    {
        var info = ImageTypeInfo.Get(_image);
        LabelTo(from, 1060847, $"#1055126\t#{info.Name}"); // a painted image of:
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 2))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            return;
        }

        from.SendGump(new InternalGump(_image));
    }

    private class InternalGump : Gump
    {
        public InternalGump(ImageType image) : base(75, 25)
        {
            var info = ImageTypeInfo.Get(image);

            AddBackground(45, 20, 100, 100, 0xA3C);
            AddBackground(52, 29, 86, 82, 0xBB8);

            AddItem(info.X, info.Y, info.Figurine);
        }
    }
}
