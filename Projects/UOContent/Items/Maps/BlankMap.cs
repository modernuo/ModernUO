using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BlankMap : MapItem
{
    [Constructible]
    public BlankMap()
    {
    }

    public override void OnDoubleClick(Mobile from)
    {
        SendLocalizedMessageTo(from, 500208); // It appears to be blank.
    }
}
