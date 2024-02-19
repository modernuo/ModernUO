using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Haven;

[SerializationGenerator(0, false)]
public partial class MansionGuard : BaseQuester
{
    [Constructible]
    public MansionGuard() : base("the Mansion Guard")
    {
    }

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Hue = Race.Human.RandomSkinHue();

        Female = false;
        Body = 0x190;
        Name = NameList.RandomName("male");
    }

    public override void InitOutfit()
    {
        AddItem(new PlateChest());
        AddItem(new PlateArms());
        AddItem(new PlateGloves());
        AddItem(new PlateLegs());

        Utility.AssignRandomHair(this);
        Utility.AssignRandomFacialHair(this, HairHue);

        var weapon = new Bardiche();
        weapon.Movable = false;
        AddItem(weapon);
    }

    public override int GetAutoTalkRange(PlayerMobile pm) => 3;

    public override bool CanTalkTo(PlayerMobile to) =>
        to.Quest == null && QuestSystem.CanOfferQuest(to, typeof(UzeraanTurmoilQuest));

    public override void OnTalk(PlayerMobile player, bool contextMenu)
    {
        if (player.Quest == null && QuestSystem.CanOfferQuest(player, typeof(UzeraanTurmoilQuest)))
        {
            Direction = GetDirectionTo(player);

            new UzeraanTurmoilQuest(player).SendOffer();
        }
    }
}
