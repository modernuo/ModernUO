using Server.Items;

namespace Server.Mobiles;

public class CopperElemental : BaseCreature
{
    [Constructible]
    public CopperElemental(int oreAmount = 2) : base(AIType.AI_Melee)
    {
        Body = 109;
        BaseSoundID = 268;

        SetStr(226, 255);
        SetDex(126, 145);
        SetInt(71, 92);

        SetHits(136, 153);

        SetDamage(9, 16);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 30, 40);
        SetResistance(ResistanceType.Fire, 30, 40);
        SetResistance(ResistanceType.Cold, 30, 40);
        SetResistance(ResistanceType.Poison, 20, 30);
        SetResistance(ResistanceType.Energy, 10, 20);

        SetSkill(SkillName.MagicResist, 50.1, 95.0);
        SetSkill(SkillName.Tactics, 60.1, 100.0);
        SetSkill(SkillName.Wrestling, 60.1, 100.0);

        Fame = 4800;
        Karma = -4800;

        VirtualArmor = 26;

        Item ore = new CopperOre(oreAmount);
        ore.ItemID = 0x19B9;
        PackItem(ore);
    }

    public CopperElemental(Serial serial) : base(serial)
    {
    }

    public override string CorpseName => "an ore elemental corpse";
    public override string DefaultName => "a copper elemental";

    public override bool BleedImmune => true;
    public override bool AutoDispel => true;
    public override int TreasureMapLevel => 1;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Average);
        AddLoot(LootPack.Gems, 2);
    }

    public override void AlterMeleeDamageFrom(Mobile from, ref int damage)
    {
        base.AlterMeleeDamageFrom(from, ref damage);

        damage /= 2; // 50% melee damage
    }

    public override void CheckReflect(Mobile caster, ref bool reflect)
    {
        reflect = true; // Every spell is reflected back to the caster
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
}
