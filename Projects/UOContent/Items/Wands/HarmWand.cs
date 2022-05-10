using ModernUO.Serialization;
using Server.Spells.Second;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class HarmWand : BaseWand
    {
        [Constructible]
        public HarmWand() : base(WandEffect.Harming, 5, Core.ML ? 109 : 30)
        {
        }

        public override void OnWandUse(Mobile from)
        {
            Cast(new HarmSpell(from, this));
        }
    }
}
