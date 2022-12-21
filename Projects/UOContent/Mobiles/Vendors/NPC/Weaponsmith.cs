using ModernUO.Serialization;
using System;
using System.Collections.Generic;
using Server.Engines.BulkOrders;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Weaponsmith : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public Weaponsmith() : base("the weaponsmith")
        {
            SetSkill(SkillName.ArmsLore, 64.0, 100.0);
            SetSkill(SkillName.Blacksmith, 65.0, 88.0);
            SetSkill(SkillName.Fencing, 45.0, 68.0);
            SetSkill(SkillName.Macing, 45.0, 68.0);
            SetSkill(SkillName.Swords, 45.0, 68.0);
            SetSkill(SkillName.Tactics, 36.0, 68.0);
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override VendorShoeType ShoeType => Utility.RandomBool() ? VendorShoeType.Boots : VendorShoeType.ThighBoots;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBWeaponSmith());

            if (IsTokunoVendor)
            {
                m_SBInfos.Add(new SBSEWeapons());
            }
        }

        public override int GetShoeHue() => 0;

        public override void InitOutfit()
        {
            base.InitOutfit();

            AddItem(new HalfApron());
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

        public override bool SupportsBulkOrders(Mobile from) =>
            from is PlayerMobile && Core.AOS && from.Skills.Blacksmith.Base > 0;

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
