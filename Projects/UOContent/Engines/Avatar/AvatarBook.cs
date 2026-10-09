using ModernUO.Serialization;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Engines.Avatar;

[SerializationGenerator(0)]
public partial class AvatarBook : Item
{
    [Constructible]
    public AvatarBook() : base(0x2147)
    {
        Name = "The Avatar's Ascent";
        LootType = LootType.Blessed;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from is not PlayerMobile player)
        {
            return;
        }

        if (!from.InRange(GetWorldLocation(), 2))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            return;
        }

        if (!player.Avatar.Active)
        {
            from.SendMessage("You must be an Avatar to use this book.");
            return;
        }

        player.SendGump(new AvatarShopGump(player));
    }
}
