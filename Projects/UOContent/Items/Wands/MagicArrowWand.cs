using Server.Spells.First;

namespace Server.Items
{
    public partial class MagicArrowWand : BaseWand
    {
        [Constructible]
        public MagicArrowWand() : base(WandEffect.MagicArrow, 5, Core.ML ? 109 : 30)
        {
        }

        public override void OnWandUse(Mobile from)
        {
            Cast(new MagicArrowSpell(from, this));
        }
    }
}
