using ModernUO.Serialization;
using Server.Spells.First;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class WeaknessWand : BaseWand
{
    [Constructible]
    public WeaknessWand() : base(WandEffect.Weakness, 5, 30)
    {
    }

    public override void OnWandUse(Mobile from)
    {
        Cast(new WeakenSpell(from, this));
    }
}