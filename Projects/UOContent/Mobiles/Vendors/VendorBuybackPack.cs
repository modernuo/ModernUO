using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
/// Holds items players sold to an NPC vendor until the next restock. The contents are never saved
/// and their removal reaches only the vendor and openers, like any private container.
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
            var oldest = Items[0];
            oldest.Delete();

            // Delete() on an already-deleted item is a no-op that leaves it in place; drop it
            // directly so a stale entry can never spin this loop forever.
            if (Items.Count > 0 && Items[0] == oldest)
            {
                Items.RemoveAt(0);
            }
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
