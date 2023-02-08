using ModernUO.Serialization;
using System;
using System.Collections.Generic;
using Server.Engines.BulkOrders;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Blacksmith : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public Blacksmith() : base("the blacksmith")
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

        public override NpcGuild NpcGuild => NpcGuild.BlacksmithsGuild;

        public override VendorShoeType ShoeType => VendorShoeType.None;

        public override void InitSBInfo()
        {
            /*m_SBInfos.Add( new SBSmithTools() );
      
            m_SBInfos.Add( new SBMetalShields() );
            m_SBInfos.Add( new SBWoodenShields() );
      
            m_SBInfos.Add( new SBPlateArmor() );
      
            m_SBInfos.Add( new SBHelmetArmor() );
            m_SBInfos.Add( new SBChainmailArmor() );
            m_SBInfos.Add( new SBRingmailArmor() );
            m_SBInfos.Add( new SBAxeWeapon() );
            m_SBInfos.Add( new SBPoleArmWeapon() );
            m_SBInfos.Add( new SBRangedWeapon() );
      
            m_SBInfos.Add( new SBKnifeWeapon() );
            m_SBInfos.Add( new SBMaceWeapon() );
            m_SBInfos.Add( new SBSpearForkWeapon() );
            m_SBInfos.Add( new SBSwordWeapon() );*/

            m_SBInfos.Add(new SBBlacksmith());
            if (IsTokunoVendor)
            {
                m_SBInfos.Add(new SBSEArmor());
                m_SBInfos.Add(new SBSEWeapons());
            }
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            Item item = Utility.RandomBool() ? null : new RingmailChest();

            if (item != null && !EquipItem(item))
            {
                item.Delete();
                item = null;
            }

            if (item == null)
            {
                AddItem(new FullApron());
            }

            AddItem(new Bascinet());
            AddItem(new SmithHammer());
        }

        public override Item CreateBulkOrder(Mobile from, bool fromContextMenu)
        {
            if (from is PlayerMobile pm && pm.NextSmithBulkOrder == TimeSpan.Zero &&
                (fromContextMenu || Utility.RandomDouble() < 0.2))
            {
                var theirSkill = pm.Skills.Blacksmith.Base;

                if (theirSkill >= 70.1)
                {
                    pm.NextSmithBulkOrder = TimeSpan.FromHours(6.0);
                }
                else if (theirSkill >= 50.1)
                {
                    pm.NextSmithBulkOrder = TimeSpan.FromHours(2.0);
                }
                else
                {
                    pm.NextSmithBulkOrder = TimeSpan.FromHours(1.0);
                }

                if (theirSkill >= 70.1 && (theirSkill - 40.0) / 300.0 > Utility.RandomDouble())
                {
                    return new LargeSmithBOD();
                }

                return SmallSmithBOD.CreateRandomFor(from);
            }

            return null;
        }

        public override bool IsValidBulkOrder(Item item) => item is SmallSmithBOD or LargeSmithBOD;

        public override bool SupportsBulkOrders(Mobile from) => from is PlayerMobile && from.Skills.Blacksmith.Base > 0;

        public override TimeSpan GetNextBulkOrder(Mobile from)
        {
            if (from is PlayerMobile mobile)
            {
                return mobile.NextSmithBulkOrder;
            }

            return TimeSpan.Zero;
        }

        public override void OnSuccessfulBulkOrderReceive(Mobile from)
        {
            if (Core.SE && from is PlayerMobile mobile)
            {
                mobile.NextSmithBulkOrder = TimeSpan.Zero;
            }
        }
    }
}
