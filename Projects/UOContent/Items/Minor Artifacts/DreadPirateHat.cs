using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DreadPirateHat : TricorneHat
{
    private static readonly SkillName[] _combatSkills =
    [
        SkillName.Tactics,
        SkillName.Archery,
        SkillName.Swords,
        SkillName.Macing,
        SkillName.Fencing
    ];

    [Constructible]
    public DreadPirateHat()
    {
        Hue = 0x497;

        SkillBonuses.SetValues(0, _combatSkills.RandomElement(), 10.0);

        Attributes.BonusDex = 8;
        Attributes.AttackChance = 10;
        Attributes.NightSight = 1;
    }

    public override int LabelNumber => 1063467;

    public override int BaseColdResistance => 14;
    public override int BasePoisonResistance => 10;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
