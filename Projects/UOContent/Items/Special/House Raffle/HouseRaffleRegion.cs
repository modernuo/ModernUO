using System.Linq;
using Server.Items;
using Server.Spells.Sixth;
using Server.Targeting;

namespace Server.Regions
{
    public class HouseRaffleRegion : BaseRegion
    {
        private readonly HouseRaffleStone m_Stone;

        public HouseRaffleRegion(HouseRaffleStone stone)
            : base(null, stone.PlotFacet, DefaultPriority, stone.PlotBounds) =>
            m_Stone = stone;

        public override bool AllowHousing(Mobile from, Point3D p)
        {
            if (m_Stone == null)
            {
                return false;
            }

            if (m_Stone.IsExpired)
            {
                return true;
            }

            if (m_Stone.Deed == null)
            {
                return false;
            }

            var pack = from.Backpack;

            if (pack != null && ContainsDeed(pack))
            {
                return true;
            }

            var bank = from.FindBankNoCreate();

            return bank != null && ContainsDeed(bank);
        }

        private bool ContainsDeed(Container cont)
        {
            return cont.FindItemsByType<HouseRaffleDeed>().Any(deed => deed == m_Stone.Deed);
        }

        public override bool OnTarget(Mobile m, Target t, object o)
        {
            if (m.Spell is MarkSpell && m.AccessLevel == AccessLevel.Player)
            {
                m.SendLocalizedMessage(501800); // You cannot mark an object at that location.
                return false;
            }

            return base.OnTarget(m, t, o);
        }
    }
}
