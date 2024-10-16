using ModernUO.Serialization;
using Server.Ethics;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class HolySteed : BaseMount
{
    public override string DefaultName => "a silver steed";

    [Constructible]
    public HolySteed() : base(0x75, 0x3EA8, AIType.AI_Melee, FightMode.Aggressor)
    {
        SetStr(496, 525);
        SetDex(86, 105);
        SetInt(86, 125);

        SetHits(298, 315);

        SetDamage(16, 22);

        SetDamageType(ResistanceType.Physical, 40);
        SetDamageType(ResistanceType.Fire, 40);
        SetDamageType(ResistanceType.Energy, 20);

        SetResistance(ResistanceType.Physical, 55, 65);
        SetResistance(ResistanceType.Fire, 30, 40);
        SetResistance(ResistanceType.Cold, 30, 40);
        SetResistance(ResistanceType.Poison, 30, 40);
        SetResistance(ResistanceType.Energy, 20, 30);

        SetSkill(SkillName.MagicResist, 25.1, 30.0);
        SetSkill(SkillName.Tactics, 97.6, 100.0);
        SetSkill(SkillName.Wrestling, 80.5, 92.5);

        Fame = 14000;
        Karma = 14000;

        VirtualArmor = 60;

        Tamable = false;
        ControlSlots = 1;
    }

    public override int StepsMax => 6400;
    public override string CorpseName => "a holy corpse";
    public override bool IsDispellable => false;
    public override bool IsBondable => false;
    public override FoodType FavoriteFood => FoodType.FruitsAndVeggies | FoodType.GrainsAndHay;

    private static MonsterAbility[] _abilities = [MonsterAbilities.FireBreath];
    public override MonsterAbility[] GetMonsterAbilities() => _abilities;

    public override string ApplyNameSuffix(string suffix)
    {
        var ethic = Ethic.Hero;
        if (ethic == null)
        {
            return base.ApplyNameSuffix("");
        }

        var adjunct = ethic.Definition.Adjunct;

        if (suffix.Length == 0)
        {
            return base.ApplyNameSuffix(adjunct);
        }

        return base.ApplyNameSuffix($"{suffix} {adjunct}");
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (Ethic.Hero == null || Ethic.Find(from) != Ethic.Hero)
        {
            from.SendMessage("You may not ride this steed.");
        }
        else
        {
            base.OnDoubleClick(from);
        }
    }
}
