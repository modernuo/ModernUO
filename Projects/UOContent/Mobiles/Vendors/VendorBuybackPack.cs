using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
/// Holds items players sold to an NPC vendor until the next restock. The contents are never saved
/// and their removal is never broadcast, because no client outside the vendor's buy gump was ever
/// told about them.
/// </summary>
[SerializationGenerator(0)]
public partial class VendorBuybackPack : Backpack
{
    [Constructible]
    public VendorBuybackPack()
    {
        Layer = Layer.ShopBuy;
        Movable = false;
        Visible = false;
    }

    public override bool SkipsChildSerialization => true;

    public override bool RestrictsChildRemoval => true;

    // DropItem appends, so Items[0] is always the oldest entry.
    public void AddBuyback(Item item, int capacity)
    {
        if (capacity <= 0)
        {
            item.Delete();
            return;
        }

        while (Items.Count >= capacity)
        {
            Items[0].Delete();
        }

        DropItem(item);
    }

    public void Purge()
    {
        for (var i = Items.Count - 1; i >= 0; --i)
        {
            if (i < Items.Count)
            {
                Items[i].Delete();
            }
        }
    }
}
