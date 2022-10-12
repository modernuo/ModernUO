using Server.Items;

namespace Server.Mobiles;

public class Betrayer : BaseCreature
{
    [Constructible]
    public Betrayer() : base(AIType.AI_Mage)
    {
        Body = 767;

        SetStr(401, 500);
        SetDex(81, 100);
        SetInt(151, 200);

        SetHits(241, 300);

        SetDamage(16, 22);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 60, 70);
        SetResistance(ResistanceType.Fire, 60, 70);
        SetResistance(ResistanceType.Cold, 60, 70);
        SetResistance(ResistanceType.Poison, 30, 40);
        SetResistance(ResistanceType.Energy, 20, 30);

        SetSkill(SkillName.Anatomy, 90.1, 100.0);
        SetSkill(SkillName.EvalInt, 90.1, 100.0);
        SetSkill(SkillName.Magery, 50.1, 100.0);
        SetSkill(SkillName.Meditation, 90.1, 100.0);
        SetSkill(SkillName.MagicResist, 120.1, 130.0);
        SetSkill(SkillName.Tactics, 90.1, 100.0);
        SetSkill(SkillName.Wrestling, 90.1, 100.0);

        Fame = 15000;
        Karma = -15000;

        VirtualArmor = 65;
        SpeechHue = Utility.RandomDyedHue();

        PackItem(new PowerCrystal());

        if (Utility.RandomDouble() < 0.02)
        {
            PackItem(new BlackthornWelcomeBook());
        }
    }

    public Betrayer(Serial serial) : base(serial)
    {
    }

    public override string CorpseName => "a betrayer corpse";

    public override string DefaultName => "a betrayer";

    public override bool AlwaysMurderer => true;
    public override bool BardImmune => !Core.AOS;
    public override Poison PoisonImmune => Poison.Lethal;
    public override int Meat => 1;
    public override int TreasureMapLevel => 5;

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (Utility.RandomDouble() < 0.05)
        {
            if (!IsParagon)
            {
                if (Utility.RandomDouble() < 0.75)
                {
                    c.DropItem(DawnsMusicGear.RandomCommon);
                }
                else
                {
                    c.DropItem(DawnsMusicGear.RandomUncommon);
                }
            }
            else
            {
                c.DropItem(DawnsMusicGear.RandomRare);
            }
        }
    }

    public override int GetDeathSound() => 0x423;

    public override int GetAttackSound() => 0x23B;

    public override int GetHurtSound() => 0x140;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich);
        AddLoot(LootPack.Rich);
        AddLoot(LootPack.Gems, 1);
    }

    private static MonsterAbility[] _abilities = { MonsterAbility.ColossalBlow, MonsterAbility.PoisonGasAreaAttack };
    public override MonsterAbility[] GetMonsterAbilities() => _abilities;

    public override void Serialize(IGenericWriter writer)
    {
        base.Serialize(writer);

        writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
        base.Deserialize(reader);

        var version = reader.ReadInt();
    }
}
