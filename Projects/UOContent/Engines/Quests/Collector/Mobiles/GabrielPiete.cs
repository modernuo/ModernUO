using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Collector;

[SerializationGenerator(0, false)]
public partial class GabrielPiete : BaseQuester
{
    [Constructible]
    public GabrielPiete() : base("the renowned minstrel")
    {
    }

    public override string DefaultName => "Gabriel Piete";

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Hue = 0x83EF;

        Female = false;
        Body = 0x190;
    }

    public override void InitOutfit()
    {
        AddItem(new FancyShirt());
        AddItem(new LongPants(0x5F7));
        AddItem(new Shoes(0x5F7));

        HairItemID = 0x2049; // Pig Tails
        HairHue = 0x460;

        FacialHairItemID = 0x2041; // Mustache
        FacialHairHue = 0x460;
    }

    public override bool CanTalkTo(PlayerMobile to)
    {
        QuestSystem qs = to.Quest as CollectorQuest;

        if (qs == null)
        {
            return false;
        }

        return qs.IsObjectiveInProgress(typeof(FindGabrielObjective))
               || qs.IsObjectiveInProgress(typeof(FindSheetMusicObjective))
               || qs.IsObjectiveInProgress(typeof(ReturnSheetMusicObjective))
               || qs.IsObjectiveInProgress(typeof(ReturnAutographObjective));
    }

    public override void OnTalk(PlayerMobile player, bool contextMenu)
    {
        var qs = player.Quest;

        if (qs is CollectorQuest)
        {
            Direction = GetDirectionTo(player);

            QuestObjective obj = qs.FindObjective<FindGabrielObjective>();

            if (obj?.Completed == false)
            {
                obj.Complete();
            }
            else if (qs.IsObjectiveInProgress(typeof(FindSheetMusicObjective)))
            {
                qs.AddConversation(new GabrielNoSheetMusicConversation());
            }
            else
            {
                obj = qs.FindObjective<ReturnSheetMusicObjective>();

                if (obj?.Completed == false)
                {
                    obj.Complete();
                }
                else if (qs.IsObjectiveInProgress(typeof(ReturnAutographObjective)))
                {
                    qs.AddConversation(new GabrielIgnoreConversation());
                }
            }
        }
    }
}
