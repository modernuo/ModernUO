using ModernUO.Serialization;
using Server.Mobiles;
using Server.Multis;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BarkeepContract : Item
{
    [Constructible]
    public BarkeepContract() : base(0x14F0)
    {
        Weight = 1.0;
        LootType = LootType.Blessed;
    }

    public override string DefaultName => "a barkeep contract";

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
        else if (from.AccessLevel >= AccessLevel.GameMaster)
        {
            from.SendLocalizedMessage(503248); // Your godly powers allow you to place this vendor whereever you wish.

            var v = new PlayerBarkeeper(from, BaseHouse.FindHouseAt(from))
            {
                Direction = from.Direction & Direction.Mask
            };

            v.MoveToWorld(from.Location, from.Map);

            Delete();
        }
        else
        {
            var house = BaseHouse.FindHouseAt(from);

            if (house?.IsOwner(from) != true)
            {
                from.LocalOverheadMessage(
                    MessageType.Regular,
                    0x3B2,
                    false,
                    "You are not the full owner of this house."
                );
            }
            else if (!house.CanPlaceNewBarkeep())
            {
                from.SendLocalizedMessage(
                    1062490
                ); // That action would exceed the maximum number of barkeeps for this house.
            }
            else
            {
                BaseHouse.IsThereVendor(from.Location, from.Map, out var vendor, out var contract);

                if (vendor)
                {
                    from.SendLocalizedMessage(1062677); // You cannot place a vendor or barkeep at this location.
                }
                else if (contract)
                {
                    from.SendLocalizedMessage(
                        1062678
                    ); // You cannot place a vendor or barkeep on top of a rental contract!
                }
                else
                {
                    var v = new PlayerBarkeeper(from, house)
                    {
                        Direction = from.Direction & Direction.Mask
                    };

                    v.MoveToWorld(from.Location, from.Map);

                    Delete();
                }
            }
        }
    }
}
