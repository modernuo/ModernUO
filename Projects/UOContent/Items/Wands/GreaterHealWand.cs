using Server.Spells.Fourth;

namespace Server.Items
{
    public partial class GreaterHealWand : BaseWand
    {
        [Constructible]
        public GreaterHealWand() : base(WandEffect.GreaterHealing, 1, Core.ML ? 109 : 5)
        {
        }

        public override void OnWandUse(Mobile from)
        {
            Cast(new GreaterHealSpell(from, this));
        }
    }
}
