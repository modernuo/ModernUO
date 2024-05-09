using ModernUO.Serialization;
using Server.Misc;
using Server.Mobiles;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class HalloweenPumpkin : Item
{
    [InternString]
    [SerializableField(0)]
    private string _staffer;

    [Constructible]
    public HalloweenPumpkin()
    {
        Weight = Utility.RandomMinMax(3, 20);
        ItemID = Utility.RandomDouble() < 0.02
            ? Utility.RandomList(0x4694, 0x4698)
            : Utility.RandomList(0xc6a, 0xc6b, 0xc6c);
    }

    public override string DefaultName => _staffer != null ? $"{_staffer}'s Jack-O-Lantern" : "A Jack-O-Lantern";

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 2))
        {
            return;
        }

        var douse = ItemID is 0x4695 or 0x4691;

        ItemID = ItemID switch
        {
            0x4694 => 0x4691,
            0x4691 => 0x4694,
            0x4698 => 0x4695,
            0x4695 => 0x4698,
            _      => ItemID
        };

        from.SendLocalizedMessage(douse ? 1113988 : 1113987); // You extinguish/light the Jack-O-Lantern
        Effects.PlaySound(GetWorldLocation(), Map, douse ? 0x3be : 0x47);
    }

    public override bool OnDragLift(Mobile from)
    {
        if (Name == null && ItemID is 0x4694 or 0x4691 or 0x4698 or 0x4695)
        {
            if (Utility.RandomBool())
            {
                new PumpkinHead().MoveToWorld(GetWorldLocation(), Map);

                Delete();
                return false;
            }

            _staffer = StaffInfo.GetRandomStaff();
        }

        return true;
    }
}
