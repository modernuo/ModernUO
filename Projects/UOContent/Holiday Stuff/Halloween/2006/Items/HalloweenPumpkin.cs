using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items
{
    [SerializationGenerator(1, false)]
    public partial class HalloweenPumpkin : Item
    {
        private static readonly string[] m_Staff =
        {
            "Owyn",
            "Luthius",
            "Kamron",
            "Jaedan",
            "Vorspire"
        };

        [Constructible]
        public HalloweenPumpkin()
        {
            Weight = Utility.RandomMinMax(3, 20);
            ItemID = Utility.RandomDouble() <= .02
                ? Utility.RandomList(0x4694, 0x4698)
                : Utility.RandomList(0xc6a, 0xc6b, 0xc6c);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 2))
            {
                return;
            }

            var douse = false;

            switch (ItemID)
            {
                case 0x4694:
                    ItemID = 0x4691;
                    break;
                case 0x4691:
                    ItemID = 0x4694;
                    douse = true;
                    break;
                case 0x4698:
                    ItemID = 0x4695;
                    break;
                case 0x4695:
                    ItemID = 0x4698;
                    douse = true;
                    break;
                default: return;
            }

            from.SendLocalizedMessage(douse ? 1113988 : 1113987); // You extinguish/light the Jack-O-Lantern
            Effects.PlaySound(GetWorldLocation(), Map, douse ? 0x3be : 0x47);
        }

        private void AssignRandomName()
        {
            Name = $"{m_Staff.RandomElement()}'s Jack-O-Lantern";
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

                AssignRandomName();
            }

            return true;
        }

        private void Deserialize(IGenericReader reader, int version)
        {
            if (version == 0 && Name == null && ItemID == 0x4698)
            {
                AssignRandomName();
            }
        }
    }
}
