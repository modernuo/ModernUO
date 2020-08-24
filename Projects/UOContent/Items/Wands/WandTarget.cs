using Server.Items;

namespace Server.Targeting
{
    public class WandTarget : Target
    {
        private readonly BaseWand m_Item;

        public WandTarget(BaseWand item) : base(6, false, TargetFlags.None) => m_Item = item;

        private static int GetOffset(Mobile caster) => 5 + (int)(caster.Skills.Magery.Value * 0.02);

        protected override void OnTarget(Mobile from, object targeted)
        {
            m_Item.DoWandTarget(from, targeted);
        }
    }
}
