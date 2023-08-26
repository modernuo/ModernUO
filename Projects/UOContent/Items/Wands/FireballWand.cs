using ModernUO.Serialization;
using Server.Spells.Third;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class FireballWand : BaseWand
{
    [Constructible]
    public FireballWand() : base(WandEffect.Fireball, 5, Core.ML ? 109 : 15)
    {
    }

    public override void OnWandUse(Mobile from)
    {
        Cast(new FireballSpell(from, this));
    }
}