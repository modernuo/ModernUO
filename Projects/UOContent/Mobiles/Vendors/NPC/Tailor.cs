using ModernUO.Serialization;
using System;
using System.Collections.Generic;
using Server.Engines.BulkOrders;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Tailor : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public Tailor() : base("the tailor")
        {
            SetSkill(SkillName.Tailoring, 64.0, 100.0);
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override NpcGuild NpcGuild => NpcGuild.TailorsGuild;

        public override VendorShoeType ShoeType => Utility.RandomBool() ? VendorShoeType.Sandals : VendorShoeType.Shoes;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBTailor());
        }

        public override Item CreateBulkOrder(Mobile from, bool fromContextMenu)
        {
            if (from is PlayerMobile pm && pm.NextTailorBulkOrder == TimeSpan.Zero &&
                (fromContextMenu || Utility.RandomDouble() < 0.2))
            {
                var theirSkill = pm.Skills.Tailoring.Base;

                pm.NextTailorBulkOrder = theirSkill switch
                {
                    >= 70.1 => TimeSpan.FromHours(6.0),
                    >= 50.1 => TimeSpan.FromHours(2.0),
                    _       => TimeSpan.FromHours(1.0)
                };

                if (theirSkill >= 70.1 && (theirSkill - 40.0) / 300.0 > Utility.RandomDouble())
                {
                    return new LargeTailorBOD();
                }

                return SmallTailorBOD.CreateRandomFor(from);
            }

            return null;
        }

        public override bool IsValidBulkOrder(Item item) => item is SmallTailorBOD or LargeTailorBOD;

        public override bool SupportsBulkOrders(Mobile from) => from is PlayerMobile && from.Skills.Tailoring.Base > 0;

        public override TimeSpan GetNextBulkOrder(Mobile from)
        {
            if (from is PlayerMobile mobile)
            {
                return mobile.NextTailorBulkOrder;
            }

            return TimeSpan.Zero;
        }

        public override void OnSuccessfulBulkOrderReceive(Mobile from)
        {
            if (Core.SE && from is PlayerMobile mobile)
            {
                mobile.NextTailorBulkOrder = TimeSpan.Zero;
            }
        }
    }
}
