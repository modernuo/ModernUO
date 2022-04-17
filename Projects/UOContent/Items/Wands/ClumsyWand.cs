using ModernUO.Serialization;
using Server.Spells.First;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class ClumsyWand : BaseWand
    {
        [Constructible]
        public ClumsyWand() : base(WandEffect.Clumsiness, 5, 30)
        {
        }

        public override void OnWandUse(Mobile from)
        {
            Cast(new ClumsySpell(from, this));
        }
    }
}
