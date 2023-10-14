using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Engines.Quests.Samurai;

[SerializationGenerator(0)]
public partial class DiseasedCat : BaseCreature
{
    [Constructible]
    public DiseasedCat() : base(AIType.AI_Animal, FightMode.Aggressor)
    {
        Body = 0xC9;
        Hue = Utility.RandomAnimalHue();
        BaseSoundID = 0x69;

        SetStr(9);
        SetDex(35);
        SetInt(5);

        SetHits(6);
        SetMana(0);

        SetDamage(1);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 5, 10);

        SetSkill(SkillName.MagicResist, 5.0);
        SetSkill(SkillName.Tactics, 4.0);
        SetSkill(SkillName.Wrestling, 5.0);

        VirtualArmor = 8;
    }

    public override string DefaultName => "a diseased cat";

    public override bool AlwaysMurderer => true;
}
