using Server.Spells.First;

namespace Server.Items
{
    [Serializable(0, false)]
    public partial class FeebleWand : BaseWand
    {
        [Constructible]
        public FeebleWand() : base(WandEffect.Feeblemindedness, 5, 30)
        {
        }

        public override void OnWandUse(Mobile from)
        {
            Cast(new FeeblemindSpell(from, this));
        }
    }
}
