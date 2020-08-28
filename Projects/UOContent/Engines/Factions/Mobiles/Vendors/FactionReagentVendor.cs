using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Factions
{
    public class FactionReagentVendor : BaseFactionVendor
    {
        public FactionReagentVendor(Town town, Faction faction) : base(town, faction, "the Reagent Man")
        {
            SetSkill(SkillName.EvalInt, 65.0, 88.0);
            SetSkill(SkillName.Inscribe, 60.0, 83.0);
            SetSkill(SkillName.Magery, 64.0, 100.0);
            SetSkill(SkillName.Meditation, 60.0, 83.0);
            SetSkill(SkillName.MagicResist, 65.0, 88.0);
            SetSkill(SkillName.Wrestling, 36.0, 68.0);
        }

        public FactionReagentVendor(Serial serial) : base(serial)
        {
        }

        public override VendorShoeType ShoeType => Utility.RandomBool() ? VendorShoeType.Shoes : VendorShoeType.Sandals;

        public override void InitSBInfo()
        {
            SBInfos.Add(new SBFactionReagent());
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            AddItem(new Robe(Utility.RandomBlueHue()));
            AddItem(new GnarledStaff());
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

    public class SBFactionReagent : SBInfo
    {
        public override IShopSellInfo SellInfo { get; } = new InternalSellInfo();

        public override List<GenericBuyInfo> BuyInfo { get; } = new InternalBuyInfo();

        public class InternalBuyInfo : List<GenericBuyInfo>
        {
            public InternalBuyInfo()
            {
                for (var i = 0; i < 2; ++i)
                {
                    Add(new GenericBuyInfo(typeof(BlackPearl), 5, 20, 0xF7A, 0));
                    Add(new GenericBuyInfo(typeof(Bloodmoss), 5, 20, 0xF7B, 0));
                    Add(new GenericBuyInfo(typeof(MandrakeRoot), 3, 20, 0xF86, 0));
                    Add(new GenericBuyInfo(typeof(Garlic), 3, 20, 0xF84, 0));
                    Add(new GenericBuyInfo(typeof(Ginseng), 3, 20, 0xF85, 0));
                    Add(new GenericBuyInfo(typeof(Nightshade), 3, 20, 0xF88, 0));
                    Add(new GenericBuyInfo(typeof(SpidersSilk), 3, 20, 0xF8D, 0));
                    Add(new GenericBuyInfo(typeof(SulfurousAsh), 3, 20, 0xF8C, 0));
                }
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
        }
    }
}
