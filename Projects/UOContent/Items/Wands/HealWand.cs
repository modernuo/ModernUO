using ModernUO.Serialization;
using Server.Spells.First;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class HealWand : BaseWand
    {
        [Constructible]
        public HealWand() : base(WandEffect.Healing, 10, Core.ML ? 109 : 25)
        {
        }

        public override void OnWandUse(Mobile from)
        {
            Cast(new HealSpell(from, this));
        }
    }
}
