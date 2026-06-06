namespace Server.Items;

// Reads the legacy on-disk VirtualHairInfo block ([int version][int itemId][int hue]).
// Used ONLY by Corpse save migration (V14/V15/V16). It has no runtime role.
public sealed class LegacyHairInfo
{
    public int ItemId { get; private set; }
    public int Hue { get; private set; }

    public void Deserialize(IGenericReader reader)
    {
        reader.ReadInt(); // legacy serialization version
        ItemId = reader.ReadInt();
        Hue = reader.ReadInt();
    }
}
