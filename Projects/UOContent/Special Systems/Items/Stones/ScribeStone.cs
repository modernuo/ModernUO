using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ScribeStone : Item
{
    [Constructible]
    public ScribeStone() : base(0xED4)
    {
        Movable = false;
        Hue = 0x105;
    }

    public override string DefaultName => "a Scribe Supply Stone";

    public override void OnDoubleClick(Mobile from)
    {
        var scribeBag = new ScribeBag();

        if (!from.AddToBackpack(scribeBag))
        {
            scribeBag.Delete();
        }
    }
}
