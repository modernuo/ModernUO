using Server.Spells.Fourth;

namespace Server.Items
{
    [Serializable(0, false)]
    public partial class LightningWand : BaseWand
    {
        [Constructible]
        public LightningWand() : base(WandEffect.Lightning, 5, Core.ML ? 109 : 20)
        {
        }

        public override void OnWandUse(Mobile from)
        {
            Cast(new LightningSpell(from, this));
        }
    }
}
