using Server.Gumps;

namespace Server.Mobiles
{
    public class PricedHealer : BaseHealer
    {
        [Constructible]
        public PricedHealer(int price = 5000)
        {
            Price = price;

            if (!Core.AOS)
            {
                NameHue = 0x35;
            }
        }

        public PricedHealer(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Price { get; set; }

        public override bool IsInvulnerable => true;

        public override bool HealsYoungPlayers => false;

        public override void InitSBInfo()
        {
        }

        public override void OfferResurrection(Mobile m)
        {
            Direction = GetDirectionTo(m);

            m.PlaySound(0x214);
            m.FixedEffect(0x376A, 10, 16);

            m.CloseGump<ResurrectGump>();
            m.SendGump(new ResurrectGump(m, this, Price));
        }

        public override bool CheckResurrect(Mobile m) => true;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(Price);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        Price = reader.ReadInt();
                        break;
                    }
            }
        }
    }
}
