using ModernUO.Serialization;
using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class IronWorker : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public IronWorker() : base("the iron worker")
        {
            SetSkill(SkillName.ArmsLore, 36.0, 68.0);
            SetSkill(SkillName.Blacksmith, 65.0, 88.0);
            SetSkill(SkillName.Fencing, 60.0, 83.0);
            SetSkill(SkillName.Macing, 61.0, 93.0);
            SetSkill(SkillName.Swords, 60.0, 83.0);
            SetSkill(SkillName.Tactics, 60.0, 83.0);
            SetSkill(SkillName.Parry, 61.0, 93.0);
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override VendorShoeType ShoeType => VendorShoeType.None;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBAxeWeapon());
            m_SBInfos.Add(new SBKnifeWeapon());
            m_SBInfos.Add(new SBMaceWeapon());
            m_SBInfos.Add(new SBSmithTools());
            m_SBInfos.Add(new SBPoleArmWeapon());
            m_SBInfos.Add(new SBSpearForkWeapon());
            m_SBInfos.Add(new SBSwordWeapon());

            m_SBInfos.Add(new SBMetalShields());

            m_SBInfos.Add(new SBHelmetArmor());
            m_SBInfos.Add(new SBPlateArmor());
            m_SBInfos.Add(new SBChainmailArmor());
            m_SBInfos.Add(new SBRingmailArmor());
            m_SBInfos.Add(new SBStuddedArmor());
            m_SBInfos.Add(new SBLeatherArmor());
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            Item item = Utility.RandomBool() ? null : new RingmailChest();

            if (item != null && !EquipItem(item))
            {
                item.Delete();

                AddItem(new FullApron(Utility.RandomBrightHue()));
            }

            AddItem(
                Utility.Random(3) switch
                {
                    0 => new Bandana(Utility.RandomBrightHue()),
                    _ => new JesterHat(Utility.RandomBrightHue()) // 1-2
                }
            );

            AddItem(new Bascinet());
            AddItem(new SmithHammer());

            item = FindItemOnLayer(Layer.Pants);

            if (item != null)
            {
                item.Hue = Utility.RandomBrightHue();
            }

            item = FindItemOnLayer(Layer.OuterLegs);

            if (item != null)
            {
                item.Hue = Utility.RandomBrightHue();
            }

            item = FindItemOnLayer(Layer.InnerLegs);

            if (item != null)
            {
                item.Hue = Utility.RandomBrightHue();
            }

            item = FindItemOnLayer(Layer.OuterTorso);

            if (item != null)
            {
                item.Hue = Utility.RandomBrightHue();
            }

            item = FindItemOnLayer(Layer.InnerTorso);

            if (item != null)
            {
                item.Hue = Utility.RandomBrightHue();
            }

            item = FindItemOnLayer(Layer.Shirt);

            if (item != null)
            {
                item.Hue = Utility.RandomBrightHue();
            }
        }
    }
}
