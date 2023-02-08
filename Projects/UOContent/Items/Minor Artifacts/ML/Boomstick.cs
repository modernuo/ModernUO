using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class Boomstick : WildStaff
{
    [Constructible]
    public Boomstick()
    {
        Hue = 0x25;

        Attributes.SpellChanneling = 1;
        Attributes.RegenMana = 3;
        Attributes.CastSpeed = 1;
        Attributes.LowerRegCost = 20;
    }

    public override int LabelNumber => 1075032; // Boomstick

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    public override void GetDamageTypes(
        Mobile wielder, out int phys, out int fire, out int cold, out int pois,
        out int nrgy, out int chaos, out int direct
    )
    {
        phys = fire = cold = pois = nrgy = direct = 0;
        chaos = 100;
    }
}
