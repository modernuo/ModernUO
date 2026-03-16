using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class AbyssReaver : Cyclone
{
    [Constructible]
    public AbyssReaver()
    {
        SkillBonuses.SetValues(0, SkillName.Throwing, Utility.RandomMinMax(5, 10));
        Attributes.WeaponDamage = Utility.RandomMinMax(25, 35);
        Slayer = SlayerName.Exorcism;
    }

    public override int LabelNumber => 1112694; // Abyss Reaver
}
