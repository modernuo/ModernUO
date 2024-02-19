using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Collector;

[SerializationGenerator(0, false)]
public partial class TomasONeerlan : BaseQuester
{
    [Constructible]
    public TomasONeerlan() : base("the famed toymaker")
    {
    }

    public override string DefaultName => "Tomas O'Neerlan";

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Hue = 0x83F8;

        Female = false;
        Body = 0x190;
    }

    public override void InitOutfit()
    {
        AddItem(new FancyShirt());
        AddItem(new LongPants(0x546));
        AddItem(new Boots(0x452));
        AddItem(new FullApron(0x455));

        HairItemID = 0x203B; // ShortHair
        HairHue = 0x455;
    }

    public override bool CanTalkTo(PlayerMobile to) =>
        to.Quest is CollectorQuest qs && (qs.IsObjectiveInProgress(typeof(FindTomasObjective))
                                          || qs.IsObjectiveInProgress(typeof(CaptureImagesObjective))
                                          || qs.IsObjectiveInProgress(typeof(ReturnImagesObjective)));

    public override void OnTalk(PlayerMobile player, bool contextMenu)
    {
        var qs = player.Quest;

        if (qs is not CollectorQuest)
        {
            return;
        }

        Direction = GetDirectionTo(player);

        if (qs.FindObjective<FindTomasObjective>() is { Completed: false } obj1) {
            var paints = new EnchantedPaints();

            if (!player.PlaceInBackpack(paints))
            {
                paints.Delete();
                // You need to clear some space in your inventory to continue with the quest.  Come back here when you have more space in your inventory.
                player.SendLocalizedMessage(1046260);
            }
            else
            {
                obj1.Complete();
            }

            return;
        }

        if (qs.IsObjectiveInProgress(typeof(CaptureImagesObjective)))
        {
            qs.AddConversation(new TomasDuringCollectingConversation());
            return;
        }

        if (qs.FindObjective<ReturnImagesObjective>() is { Completed: false } obj2)
        {
            player.Backpack?.ConsumeUpTo(typeof(EnchantedPaints), 1);

            obj2.Complete();
        }
    }
}
