using ModernUO.Serialization;
using Server.Spells.Fourth;

namespace Server.Items;

[SerializationGenerator(0, false)]
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