using ModernUO.Serialization;
using Server.Mobiles;
using Server.Multis;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ContractOfEmployment : Item
{
    [Constructible]
    public ContractOfEmployment() : base(0x14F0) => Weight = 1.0;

    public override int LabelNumber => 1041243; // a contract of employment

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
        else if (from.AccessLevel >= AccessLevel.GameMaster)
        {
            from.SendLocalizedMessage(503248); // Your godly powers allow you to place this vendor whereever you wish.

            Mobile v = new PlayerVendor(from, BaseHouse.FindHouseAt(from));

            v.Direction = from.Direction & Direction.Mask;
            v.MoveToWorld(from.Location, from.Map);

            v.SayTo(from, 503246); // Ah! it feels good to be working again.

            Delete();
        }
        else
        {
            var house = BaseHouse.FindHouseAt(from);

            if (house == null)
            {
                from.SendLocalizedMessage(503240); // Vendors can only be placed in houses.
            }
            else if (!BaseHouse.NewVendorSystem && !house.IsFriend(from))
            {
                // You must ask the owner of this building to name you a friend of the household in order to place a vendor here.
                from.SendLocalizedMessage(503242);
            }
            else if (BaseHouse.NewVendorSystem && !house.IsOwner(from))
            {
                // Only the house owner can directly place vendors.  Please ask the house owner to offer you a vendor contract so that you may place a vendor in this house.
                from.SendLocalizedMessage(1062423);
            }
            else if (!house.Public || !house.CanPlaceNewVendor())
            {
                // You cannot place this vendor or barkeep.  Make sure the house is public and has sufficient storage available.
                from.SendLocalizedMessage(503241);
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
                    // You cannot place a vendor or barkeep on top of a rental contract!
                    from.SendLocalizedMessage(1062678);
                }
                else
                {
                    Mobile v = new PlayerVendor(from, house);

                    v.Direction = from.Direction & Direction.Mask;
                    v.MoveToWorld(from.Location, from.Map);

                    v.SayTo(from, 503246); // Ah! it feels good to be working again.

                    Delete();
                }
            }
        }
    }
}
