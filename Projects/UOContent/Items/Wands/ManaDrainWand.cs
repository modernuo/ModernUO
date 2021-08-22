using Server.Spells.Fourth;

namespace Server.Items
{
    public partial class ManaDrainWand : BaseWand
    {
        [Constructible]
        public ManaDrainWand() : base(WandEffect.ManaDraining, 5, 30)
        {
        }

        public override void OnWandUse(Mobile from)
        {
            Cast(new ManaDrainSpell(from, this));
        }
    }
}
