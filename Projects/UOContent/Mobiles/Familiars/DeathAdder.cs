using ModernUO.Serialization;

namespace Server.Mobiles;

[SerializationGenerator(0)]
public partial class DeathAdder : BaseFamiliar
{
    public DeathAdder()
    {
        Body = 0x15;
        Hue = 0x455;
        BaseSoundID = 219;

        SetStr(70);
        SetDex(150);
        SetInt(100);

        SetHits(50);
        SetStam(150);
        SetMana(0);

        SetDamage(1, 4);
        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 10);
        SetResistance(ResistanceType.Poison, 100);

        SetSkill(SkillName.Wrestling, 90.0);
        SetSkill(SkillName.Tactics, 50.0);
        SetSkill(SkillName.MagicResist, 100.0);
        SetSkill(SkillName.Poisoning, 150.0);

        ControlSlots = 1;
    }

    public DeathAdder(Serial serial) : base(serial)
    {
    }

    public override string CorpseName => "a death adder corpse";
    public override string DefaultName => "a death adder";

    public override Poison HitPoison => Utility.RandomDouble() < 0.8 ? Poison.Greater : Poison.Deadly;
}
