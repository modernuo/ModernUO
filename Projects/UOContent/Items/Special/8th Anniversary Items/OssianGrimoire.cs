using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class OssianGrimoire : NecromancerSpellbook
{
    [Constructible]
    public OssianGrimoire()
    {
        LootType = LootType.Blessed;

        SkillBonuses.SetValues(0, SkillName.Necromancy, 10.0);
        Attributes.RegenMana = 1;
        Attributes.CastSpeed = 1;
        Attributes.IncreasedKarmaLoss = 5;
    }

    public override int LabelNumber => 1078148; // Ossian Grimoire
}
