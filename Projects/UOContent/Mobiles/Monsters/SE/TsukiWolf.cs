using Server.Engines.Plants;
using Server.Items;

namespace Server.Mobiles;

public class TsukiWolf : BaseCreature
{
    [Constructible]
    public TsukiWolf() : base(AIType.AI_Melee)
    {
        Body = 250;
        Hue = Utility.Random(3) == 0 ? Utility.RandomNeutralHue() : 0;

        SetStr(401, 450);
        SetDex(151, 200);
        SetInt(66, 76);

        SetHits(376, 450);
        SetMana(40);

        SetDamage(14, 18);

        SetDamageType(ResistanceType.Physical, 90);
        SetDamageType(ResistanceType.Cold, 5);
        SetDamageType(ResistanceType.Energy, 5);

        SetResistance(ResistanceType.Physical, 40, 60);
        SetResistance(ResistanceType.Fire, 50, 70);
        SetResistance(ResistanceType.Cold, 50, 70);
        SetResistance(ResistanceType.Poison, 50, 70);
        SetResistance(ResistanceType.Energy, 50, 70);

        SetSkill(SkillName.Anatomy, 65.1, 72.0);
        SetSkill(SkillName.MagicResist, 65.1, 70.0);
        SetSkill(SkillName.Tactics, 95.1, 110.0);
        SetSkill(SkillName.Wrestling, 97.6, 107.5);

        Fame = 8500;
        Karma = -8500;

        if (Core.ML && Utility.RandomDouble() < .33)
        {
            PackItem(Seed.RandomPeculiarSeed(1));
        }

        PackItem(
            Utility.Random(10) switch
            {
                0 => new LeftArm(),
                1 => new RightArm(),
                2 => new Torso(),
                3 => new Bone(),
                4 => new RibCage(),
                5 => new RibCage(),
                _ => new BonePile() // 6-9
            }
        );
    }

    public TsukiWolf(Serial serial)
        : base(serial)
    {
    }

    public override string CorpseName => "a tsuki wolf corpse";
    public override string DefaultName => "a tsuki wolf";
    public override int Meat => 4;
    public override int Hides => 25;
    public override FoodType FavoriteFood => FoodType.Meat;

    private static MonsterAbility[] _abilities = { MonsterAbilities.BloodBathAttack };
    public override MonsterAbility[] GetMonsterAbilities() => _abilities;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Average);
        AddLoot(LootPack.Rich);
    }

    public override void Serialize(IGenericWriter writer)
    {
        base.Serialize(writer);

        writer.Write(0);
    }

    public override void Deserialize(IGenericReader reader)
    {
        base.Deserialize(reader);

        var version = reader.ReadInt();
    }

    public override int GetAngerSound() => 0x52D;

    public override int GetIdleSound() => 0x52C;

    public override int GetAttackSound() => 0x52B;

    public override int GetHurtSound() => 0x52E;

    public override int GetDeathSound() => 0x52A;
}
