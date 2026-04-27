using ModernUO.Serialization;
using Server.Engines.MLQuests.Objectives;
using Server.Engines.MLQuests.Rewards;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.MLQuests.Definitions;

public class IntoTheVoidQuest : MLQuest
{
    public IntoTheVoidQuest()
    {
        Activated = true;
        OneTimeOnly = true;
        Title = 1112687;       // Into the Void
        Description = 1112690; // Quest description cliloc
        RefusalMessage = 1112691;
        InProgressMessage = 1112692;
        CompletionMessage = 1112693;

        // TODO: ServUO targets BaseVoidCreature (a broader set of SA void creatures).
        // Expand the target list when additional void creature types are implemented.
        Objectives.Add(new KillObjective(10, new[] { typeof(WandererOfTheVoid) }, "Void Daemons"));

        Rewards.Add(new ItemReward(1112694, typeof(AbyssReaver))); // Abyss Reaver
    }
}

[QuesterName("Agralem")]
[SerializationGenerator(0, false)]
public partial class Agralem : BaseCreature
{
    [Constructible]
    public Agralem() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "the Bladeweaver";

        Race = Race.Gargoyle;
        Body = 666;
        Female = false;
        Hue = 34536;

        HairItemID = 0x425D;
        HairHue = 0x31D;

        CantWalk = true;

        InitStats(100, 100, 25);

        SetSkill(SkillName.Anatomy, 65.0, 90.0);
        SetSkill(SkillName.MagicResist, 65.0, 90.0);
        SetSkill(SkillName.Tactics, 65.0, 90.0);
        SetSkill(SkillName.Throwing, 65.0, 90.0);

        AddItem(new Cyclone { LootType = LootType.Blessed });
        AddItem(new GargishLeatherKiltType1 { Hue = 2305, LootType = LootType.Blessed });
        AddItem(new GargishLeatherChestType1 { Hue = 2305, LootType = LootType.Blessed });
        AddItem(new GargishLeatherArmsType1 { Hue = 2305, LootType = LootType.Blessed });
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Agralem";

    public override bool CanShout => true;

    public override void Shout(PlayerMobile pm)
    {
        MLQuestSystem.Tell(this, pm, 1112688); // Daemons from the void! They must be vanquished!
    }
}
