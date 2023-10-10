using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Samurai;

[SerializationGenerator(0)]
public partial class YoungNinja : BaseCreature
{
    [Constructible]
    public YoungNinja() : base(AIType.AI_Melee, FightMode.Aggressor)
    {
        InitStats(45, 30, 5);
        SetHits(20, 30);

        Hue = Race.Human.RandomSkinHue();
        Body = 0x190;

        Utility.AssignRandomHair(this);
        Utility.AssignRandomFacialHair(this);

        AddItem(new NinjaTabi());
        AddItem(new LeatherNinjaPants());
        AddItem(new LeatherNinjaJacket());
        AddItem(new LeatherNinjaBelt());

        AddItem(new Bandana(Utility.RandomNondyedHue()));

        AddItem(
            Utility.Random(3) switch
            {
                0 => new Tessen(),
                1 => new Kama(),
                _ => new Lajatang()
            }
        );

        SetSkill(SkillName.Swords, 50.0);
        SetSkill(SkillName.Tactics, 50.0);
    }

    public override string CorpseName => "a young ninja's corpse";
    public override string DefaultName => "a young ninja";

    public override bool AlwaysMurderer => true;
}
