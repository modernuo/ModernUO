using Server.Items;

namespace Server.Mobiles;

public class VeriteElemental : BaseCreature
{
    [Constructible]
    public VeriteElemental(int oreAmount = 2) : base(AIType.AI_Melee)
    {
        Body = 113;
        BaseSoundID = 268;

        SetStr(226, 255);
        SetDex(126, 145);
        SetInt(71, 92);

        SetHits(136, 153);

        SetDamage(9, 16);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Energy, 50);

        SetResistance(ResistanceType.Physical, 30, 40);
        SetResistance(ResistanceType.Fire, 10, 20);
        SetResistance(ResistanceType.Cold, 50, 60);
        SetResistance(ResistanceType.Poison, 50, 60);
        SetResistance(ResistanceType.Energy, 50, 60);

        SetSkill(SkillName.MagicResist, 50.1, 95.0);
        SetSkill(SkillName.Tactics, 60.1, 100.0);
        SetSkill(SkillName.Wrestling, 60.1, 100.0);

        Fame = 3500;
        Karma = -3500;

        VirtualArmor = 35;

        Item ore = new VeriteOre(oreAmount);
        ore.ItemID = 0x19B9;
        PackItem(ore);
    }

    public VeriteElemental(Serial serial) : base(serial)
    {
    }

    public override string CorpseName => "an ore elemental corpse";
    public override string DefaultName => "a verite elemental";

    public override bool AutoDispel => true;
    public override bool BleedImmune => true;
    public override int TreasureMapLevel => 1;

    private static MonsterAbility[] _abilities = { MonsterAbility.DestroyEquipment };
    public override MonsterAbility[] GetMonsterAbilities() => _abilities;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich);
        AddLoot(LootPack.Gems, 2);
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
