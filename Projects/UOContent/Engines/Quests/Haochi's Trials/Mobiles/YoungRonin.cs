using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Samurai;

[SerializationGenerator(0)]
public partial class YoungRonin : BaseCreature
{
    [Constructible]
    public YoungRonin() : base(AIType.AI_Melee, FightMode.Aggressor)
    {
        InitStats(45, 30, 5);
        SetHits(10, 20);

        Hue = Race.Human.RandomSkinHue();
        Body = 0x190;

        Utility.AssignRandomHair(this);
        Utility.AssignRandomFacialHair(this);

        AddItem(new LeatherDo());
        AddItem(new LeatherHiroSode());
        AddItem(new SamuraiTabi());

        AddItem(
            Utility.Random(3) switch
            {
                0 => new StuddedHaidate(),
                1 => new PlateSuneate(),
                _ => new LeatherSuneate()
            }
        );

        AddItem(new Bandana(Utility.RandomNondyedHue()));

        AddItem(
            Utility.Random(3) switch
            {
                0 => new NoDachi(),
                1 => new Lajatang(),
                _ => new Wakizashi()
            }
        );

        SetSkill(SkillName.Swords, 50.0);
        SetSkill(SkillName.Tactics, 50.0);
    }

    public override string CorpseName => "a young ronin's corpse";
    public override string DefaultName => "a young ronin";

    public override bool AlwaysMurderer => true;
}
