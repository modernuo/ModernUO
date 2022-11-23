using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class EmberStaff : QuarterStaff
{
    [Constructible]
    public EmberStaff()
    {
        LootType = LootType.Blessed;

        WeaponAttributes.HitFireball = 15;
        WeaponAttributes.MageWeapon = 10;
        Attributes.SpellChanneling = 1;
        Attributes.CastSpeed = -1;
        WeaponAttributes.LowerStatReq = 50;
    }

    public override int LabelNumber => 1077582; // Ember Staff
}
