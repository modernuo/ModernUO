using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class LysanderGathenwale : BaseCreature
{
    [Constructible]
    public LysanderGathenwale() : base(AIType.AI_Mage)
    {
        Title = "the Cursed";

        Hue = 0x8838;
        Body = 0x190;

        AddItem(new Boots(0x599));
        AddItem(new Cloak(0x96F));

        var spellbook = new Spellbook();
        var gloves = new RingmailGloves();
        var chest = new StuddedChest();
        var arms = new PlateArms();

        spellbook.Hue = 0x599;
        gloves.Hue = 0x599;
        chest.Hue = 0x96F;
        arms.Hue = 0x599;

        AddItem(spellbook);
        AddItem(gloves);
        AddItem(chest);
        AddItem(arms);

        SetStr(111, 120);
        SetDex(71, 80);
        SetInt(121, 130);

        SetHits(180, 207);
        SetMana(227, 265);

        SetDamage(5, 13);

        SetResistance(ResistanceType.Physical, 35, 45);
        SetResistance(ResistanceType.Fire, 25, 30);
        SetResistance(ResistanceType.Cold, 50, 60);
        SetResistance(ResistanceType.Poison, 25, 35);
        SetResistance(ResistanceType.Energy, 25, 35);

        SetSkill(SkillName.Wrestling, 80.1, 90.0);
        SetSkill(SkillName.Tactics, 90.1, 100.0);
        SetSkill(SkillName.MagicResist, 80.1, 90.0);
        SetSkill(SkillName.Magery, 90.1, 100.0);
        SetSkill(SkillName.EvalInt, 95.1, 100.0);
        SetSkill(SkillName.Meditation, 90.1, 100.0);

        Fame = 5000;
        Karma = -10000;

        var reags = Loot.RandomReagent();
        reags.Amount = 30;
        PackItem(reags);
    }

    public override bool ClickTitle => false;
    public override bool ShowFameTitle => false;
    public override bool DeleteCorpseOnDeath => true;
    public override string DefaultName => "Lysander Gatherwale";

    public override bool AlwaysMurderer => true;

    public override int GetIdleSound() => 0x1CE;

    public override int GetAngerSound() => 0x1AC;

    public override int GetDeathSound() => 0x182;

    public override int GetHurtSound() => 0x28D;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.MedScrolls, 2);
    }

    public override bool OnBeforeDeath()
    {
        if (!base.OnBeforeDeath())
        {
            return false;
        }

        Backpack?.Destroy();

        if (Utility.Random(3) == 0)
        {
            var notebook = Loot.RandomLysanderNotebook();
            notebook.MoveToWorld(Location, Map);
        }

        Effects.SendLocationEffect(Location, Map, 0x376A, 10, 1);
        return true;
    }
}
