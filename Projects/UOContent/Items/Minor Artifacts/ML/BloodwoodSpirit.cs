using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class BloodwoodSpirit : BaseTalisman
{
    [Constructible]
    public BloodwoodSpirit() : base(0x2F5A)
    {
        Hue = 0x27;
        MaxChargeTime = 1200;

        Removal = TalismanRemoval.Damage;
        Blessed = GetRandomBlessed();
        Protection = GetRandomProtection(false);

        SkillBonuses.SetValues(0, SkillName.SpiritSpeak, 10.0);
        SkillBonuses.SetValues(1, SkillName.Necromancy, 5.0);
    }

    public override int LabelNumber => 1075034; // Bloodwood Spirit
    public override bool ForceShowName => true;
}
