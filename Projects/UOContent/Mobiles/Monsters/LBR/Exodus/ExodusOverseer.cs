using Server.Items;

namespace Server.Mobiles;

public class ExodusOverseer : BaseCreature
{
    [Constructible]
    public ExodusOverseer() : base(AIType.AI_Melee)
    {
        Body = 0x2F4;

        SetStr(561, 650);
        SetDex(76, 95);
        SetInt(61, 90);

        SetHits(331, 390);

        SetDamage(13, 19);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Energy, 50);

        SetResistance(ResistanceType.Physical, 45, 55);
        SetResistance(ResistanceType.Fire, 40, 60);
        SetResistance(ResistanceType.Cold, 25, 35);
        SetResistance(ResistanceType.Poison, 25, 35);
        SetResistance(ResistanceType.Energy, 25, 35);

        SetSkill(SkillName.MagicResist, 80.2, 98.0);
        SetSkill(SkillName.Tactics, 80.2, 98.0);
        SetSkill(SkillName.Wrestling, 80.2, 98.0);

        Fame = 10000;
        Karma = -10000;
        VirtualArmor = 50;

        if (Utility.Random(2) == 0)
        {
            PackItem(new PowerCrystal());
        }
        else
        {
            PackItem(new ArcaneGem());
        }
    }

    public ExodusOverseer(Serial serial) : base(serial)
    {
    }

    public override string CorpseName => "an overseer's corpse";

    public override bool IsScaredOfScaryThings => false;
    public override bool IsScaryToPets => true;

    public override string DefaultName => "an exodus overseer";

    public override bool AutoDispel => true;
    public override bool BardImmune => !Core.AOS;
    public override Poison PoisonImmune => Poison.Lethal;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich);
    }

    public override int GetIdleSound() => 0xFD;

    public override int GetAngerSound() => 0x26C;

    public override int GetDeathSound() => 0x211;

    public override int GetAttackSound() => 0x23B;

    public override int GetHurtSound() => 0x140;

    public override void OnDamagedBySpell(Mobile from, int damage)
    {
        base.OnDamagedBySpell(from, damage);

        if (from?.Alive == true && Utility.RandomDouble() < 0.4)
        {
            SendEBolt(from);
        }
    }

    public override void OnGotMeleeAttack(Mobile attacker, int damage)
    {
        base.OnGotMeleeAttack(attacker, damage);

        if (attacker is { Alive: true, Weapon: BaseRanged } && Utility.RandomDouble() < 0.4)
        {
            SendEBolt(attacker);
        }
    }

    public void SendEBolt(Mobile to)
    {
        MovingParticles(to, 0x379F, 7, 0, false, true, 0xBE3, 0xFCB, 0x211);
        to.PlaySound(0x229);
        DoHarmful(to);
        AOS.Damage(to, this, 50, 0, 0, 0, 0, 100);
    }

    private static MonsterAbility[] _abilities = { MonsterAbility.MagicalBarrierCounter };

    // OSI Removed this ability roughly during UOML
    public override MonsterAbility[] GetMonsterAbilities() => Core.ML ? null : _abilities;

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
}
