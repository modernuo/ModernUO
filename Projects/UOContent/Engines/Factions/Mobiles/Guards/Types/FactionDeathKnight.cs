using ModernUO.Serialization;
using Server.Items;

namespace Server.Factions;

[SerializationGenerator(0, false)]
public partial class FactionDeathKnight : BaseFactionGuard
{
    [Constructible]
    public FactionDeathKnight() : base("the death knight")
    {
        GenerateBody(false, false);
        Hue = 1;

        SetStr(126, 150);
        SetDex(61, 85);
        SetInt(81, 95);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 30, 50);
        SetResistance(ResistanceType.Fire, 30, 50);
        SetResistance(ResistanceType.Cold, 30, 50);
        SetResistance(ResistanceType.Energy, 30, 50);
        SetResistance(ResistanceType.Poison, 30, 50);

        VirtualArmor = 24;

        SetSkill(SkillName.Swords, 100.0, 110.0);
        SetSkill(SkillName.Wrestling, 100.0, 110.0);
        SetSkill(SkillName.Tactics, 100.0, 110.0);
        SetSkill(SkillName.MagicResist, 100.0, 110.0);
        SetSkill(SkillName.Healing, 100.0, 110.0);
        SetSkill(SkillName.Anatomy, 100.0, 110.0);

        SetSkill(SkillName.Magery, 100.0, 110.0);
        SetSkill(SkillName.EvalInt, 100.0, 110.0);
        SetSkill(SkillName.Meditation, 100.0, 110.0);

        var shroud = new Item(0x204E);
        shroud.Layer = Layer.OuterTorso;

        AddItem(Immovable(Rehued(shroud, 1109)));
        AddItem(Newbied(Rehued(new ExecutionersAxe(), 2211)));

        PackItem(new Bandage(Utility.RandomMinMax(30, 40)));
        PackStrongPotions(6, 12);
    }

    public override GuardAI GuardAI => GuardAI.Melee | GuardAI.Curse | GuardAI.Bless;
}
