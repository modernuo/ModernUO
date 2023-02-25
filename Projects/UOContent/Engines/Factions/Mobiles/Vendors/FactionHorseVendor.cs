using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Factions
{
    public class FactionHorseVendor : BaseFactionVendor
    {
        public FactionHorseVendor(Town town, Faction faction) : base(town, faction, "the Horse Breeder")
        {
            SetSkill(SkillName.AnimalLore, 64.0, 100.0);
            SetSkill(SkillName.AnimalTaming, 90.0, 100.0);
            SetSkill(SkillName.Veterinary, 65.0, 88.0);
        }

        public FactionHorseVendor(Serial serial) : base(serial)
        {
        }

        public override VendorShoeType ShoeType => Female ? VendorShoeType.ThighBoots : VendorShoeType.Boots;

        public override void InitSBInfo()
        {
        }

        public override int GetShoeHue() => 0;

        public override void InitOutfit()
        {
            base.InitOutfit();

            AddItem(Utility.RandomBool() ? new QuarterStaff() : new ShepherdsCrook());
        }

        public override void VendorBuy(Mobile from)
        {
            if (Faction == null || Faction.Find(from, true) != Faction)
            {
                PrivateOverheadMessage(
                    MessageType.Regular,
                    0x3B2,
                    1042201, // You are not in my faction, I cannot sell you a horse!
                    from.NetState
                );
            }
            else if (FactionGump.Exists(from))
            {
                from.SendLocalizedMessage(1042160); // You already have a faction menu open.
            }
            else if (from is PlayerMobile mobile)
            {
                mobile.SendGump(new HorseBreederGump(mobile, Faction));
            }
        }

        public override void VendorSell(Mobile from)
        {
        }

        public override bool OnBuyItems(Mobile buyer, List<BuyItemResponse> list) => false;

        public override bool OnSellItems(Mobile seller, List<SellItemResponse> list) => false;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
