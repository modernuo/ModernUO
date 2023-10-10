using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Engines.Quests.Samurai;

[SerializationGenerator(0)]
public partial class InjuredWolf : BaseCreature
{
    [Constructible]
    public InjuredWolf() : base(AIType.AI_Animal, FightMode.Aggressor)
    {
        Body = 0xE1;
        BaseSoundID = 0xE5;

        Hue = Utility.RandomAnimalHue();

        SetStr(10, 20);
        SetDex(45, 65);
        SetInt(10, 15);

        SetHits(1);

        SetDamage(1, 3);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 15);
        SetResistance(ResistanceType.Fire, 5, 10);

        SetSkill(SkillName.MagicResist, 10.0);
        SetSkill(SkillName.Tactics, 0.0, 5.0);
        SetSkill(SkillName.Wrestling, 20.0, 30.0);
    }

    public override string CorpseName => "an injured wolf corpse";
    public override string DefaultName => "an injured wolf";

    public override int GetIdleSound() => 0xE9;
}
