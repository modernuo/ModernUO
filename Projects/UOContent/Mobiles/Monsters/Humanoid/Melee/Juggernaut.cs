using Server.Items;

namespace Server.Mobiles;

public class Juggernaut : BaseCreature
{
    [Constructible]
    public Juggernaut() : base(AIType.AI_Melee)
    {
        Body = 768;

        SetStr(301, 400);
        SetDex(51, 70);
        SetInt(51, 100);

        SetHits(181, 240);

        SetDamage(12, 19);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Fire, 25);
        SetDamageType(ResistanceType.Energy, 25);

        SetResistance(ResistanceType.Physical, 65, 75);
        SetResistance(ResistanceType.Fire, 35, 45);
        SetResistance(ResistanceType.Cold, 35, 45);
        SetResistance(ResistanceType.Poison, 15, 25);
        SetResistance(ResistanceType.Energy, 10, 20);

        SetSkill(SkillName.Anatomy, 90.1, 100.0);
        SetSkill(SkillName.MagicResist, 140.1, 150.0);
        SetSkill(SkillName.Tactics, 90.1, 100.0);
        SetSkill(SkillName.Wrestling, 90.1, 100.0);

        Fame = 12000;
        Karma = -12000;

        VirtualArmor = 70;

        if (Utility.RandomDouble() < 0.1)
        {
            PackItem(new PowerCrystal());
        }

        if (Utility.RandomDouble() < 0.4)
        {
            PackItem(new ClockworkAssembly());
        }
    }

    public Juggernaut(Serial serial) : base(serial)
    {
    }

    public override string CorpseName => "a juggernaut corpse";

    public override string DefaultName => "a blackthorn juggernaut";

    public override bool AlwaysMurderer => true;
    public override bool BardImmune => !Core.AOS;
    public override bool BleedImmune => true;
    public override Poison PoisonImmune => Poison.Lethal;
    public override int Meat => 1;
    public override int TreasureMapLevel => 5;

    private static MonsterAbility[] _abilities = { MonsterAbility.ColossalBlow };
    public override MonsterAbility[] GetMonsterAbilities() => _abilities;

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

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich);
        AddLoot(LootPack.Gems, 1);
    }

    public override int GetDeathSound() => 0x423;

    public override int GetAttackSound() => 0x23B;

    public override int GetHurtSound() => 0x140;

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
