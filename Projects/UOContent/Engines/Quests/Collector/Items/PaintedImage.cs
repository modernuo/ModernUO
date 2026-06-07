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
        Hue = 0x8FD;
        _image = image;
    }

    public override double DefaultWeight => 1.0;

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

        PaintedImageGump.DisplayTo(from, _image);
    }
}

public class PaintedImageGump : DynamicGump
{
    private readonly ImageType _image;

    public override bool Singleton => true;

    private PaintedImageGump(ImageType image) : base(75, 25) => _image = image;

    public static void DisplayTo(Mobile from, ImageType image)
    {
        if (from?.NetState == null)
        {
            return;
        }

        from.SendGump(new PaintedImageGump(image));
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();

        var info = ImageTypeInfo.Get(_image);

        builder.AddBackground(45, 20, 100, 100, 0xA3C);
        builder.AddBackground(52, 29, 86, 82, 0xBB8);

        builder.AddItem(info.X, info.Y, info.Figurine);
    }
}
