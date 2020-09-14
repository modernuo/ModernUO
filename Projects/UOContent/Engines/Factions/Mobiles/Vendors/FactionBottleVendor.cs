using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Factions
{
    public class FactionBottleVendor : BaseFactionVendor
    {
        public FactionBottleVendor(Town town, Faction faction) : base(town, faction, "the Bottle Seller")
        {
            SetSkill(SkillName.Alchemy, 85.0, 100.0);
            SetSkill(SkillName.TasteID, 65.0, 88.0);
        }

        public FactionBottleVendor(Serial serial) : base(serial)
        {
        }

        public override VendorShoeType ShoeType => Utility.RandomBool() ? VendorShoeType.Shoes : VendorShoeType.Sandals;

        public override void InitSBInfo()
        {
            SBInfos.Add(new SBFactionBottle());
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            AddItem(new Robe(Utility.RandomPinkHue()));
        }

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

    public class SBFactionBottle : SBInfo
    {
        public override IShopSellInfo SellInfo { get; } = new InternalSellInfo();

        public override List<GenericBuyInfo> BuyInfo { get; } = new InternalBuyInfo();

        public class InternalBuyInfo : List<GenericBuyInfo>
        {
            public InternalBuyInfo()
            {
                for (var i = 0; i < 5; ++i)
                {
                    Add(new GenericBuyInfo(typeof(Bottle), 5, 20, 0xF0E, 0));
                }
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
        }
    }
}
